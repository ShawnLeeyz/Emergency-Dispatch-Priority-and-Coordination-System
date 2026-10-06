using System.Globalization;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.Data.Sqlite;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

/// <summary>Owns the local SQLite file and keeps one set of domain objects for the running application.</summary>
public sealed class SqliteDatabase
{
    private readonly string _connectionString;
    private readonly Lock _databaseLock = new();
    private readonly Dictionary<Guid, Case> _cases = [];
    private readonly Dictionary<ResponseUnitType, Department> _departments = [];
    private readonly Dictionary<Guid, Unit> _units = [];

    public SqliteDatabase(string connectionString)
    {
        _connectionString = connectionString;
        CreateDatabaseDirectory();
        CreateTables();
        SeedDepartments();
        LoadData();
    }

    internal IReadOnlyCollection<Case> GetCases()
    {
        lock (_databaseLock)
            return _cases.Values.OrderByDescending(dispatchCase => dispatchCase.RecordedAt).ToArray();
    }

    internal Case? GetCase(Guid id)
    {
        lock (_databaseLock)
            return _cases.GetValueOrDefault(id);
    }

    internal IReadOnlyCollection<Department> GetDepartments()
    {
        lock (_databaseLock)
            return _departments.Values.OrderBy(department => department.Type).ToArray();
    }

    internal Department? GetDepartment(ResponseUnitType type)
    {
        lock (_databaseLock)
            return _departments.GetValueOrDefault(type);
    }

    internal void AddCase(Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(dispatchCase);
        lock (_databaseLock)
        {
            if (!_cases.TryAdd(dispatchCase.Id, dispatchCase))
                throw new InvalidOperationException("A case with that ID already exists.");
            SaveCaseInternal(dispatchCase);
        }
    }

    internal void SaveCase(Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(dispatchCase);
        lock (_databaseLock)
        {
            _cases[dispatchCase.Id] = dispatchCase;
            SaveCaseInternal(dispatchCase);
        }
    }

    internal void SaveUnit(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        lock (_databaseLock)
        {
            _units[unit.Id] = unit;
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE Units
                SET Location = $location,
                    PersonnelCount = $personnelCount,
                    Availability = $availability,
                    AssignedCaseId = $assignedCaseId
                WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$location", unit.Location);
            command.Parameters.AddWithValue("$personnelCount", unit.PersonnelCount);
            command.Parameters.AddWithValue("$availability", (int)unit.Availability);
            command.Parameters.AddWithValue("$assignedCaseId", unit.AssignedCaseId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$id", unit.Id.ToString());
            command.ExecuteNonQuery();
        }
    }

    internal void AddNotification(DispatchNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        lock (_databaseLock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO DispatchNotifications
                    (CreatedAt, UnitIdentifier, DepartmentType, CaseNumber, IncidentType, Location, Message)
                VALUES
                    ($createdAt, $unitIdentifier, $departmentType, $caseNumber, $incidentType, $location, $message);
                """;
            command.Parameters.AddWithValue("$createdAt", notification.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$unitIdentifier", notification.UnitIdentifier);
            command.Parameters.AddWithValue("$departmentType", (int)notification.DepartmentType);
            command.Parameters.AddWithValue("$caseNumber", notification.CaseNumber);
            command.Parameters.AddWithValue("$incidentType", notification.IncidentType);
            command.Parameters.AddWithValue("$location", notification.Location);
            command.Parameters.AddWithValue("$message", notification.Message);
            command.ExecuteNonQuery();
        }
    }

    internal IReadOnlyCollection<DispatchNotification> GetNotifications()
    {
        lock (_databaseLock)
        {
            var notifications = new List<DispatchNotification>();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT CreatedAt, UnitIdentifier, DepartmentType, CaseNumber, IncidentType, Location, Message
                FROM DispatchNotifications ORDER BY Id DESC;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(new DispatchNotification(
                    ParseDate(reader.GetString(0)), reader.GetString(1),
                    (ResponseUnitType)reader.GetInt32(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6)));
            }
            return notifications;
        }
    }

    private void SaveCaseInternal(Case dispatchCase)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Cases
                    (Id, CallerName, CallerPhone, IncidentType, Description, Location, RecordedAt, Severity, Priority, Status)
                VALUES
                    ($id, $callerName, $callerPhone, $incidentType, $description, $location, $recordedAt, $severity, $priority, $status)
                ON CONFLICT(Id) DO UPDATE SET
                    CallerName = excluded.CallerName,
                    CallerPhone = excluded.CallerPhone,
                    IncidentType = excluded.IncidentType,
                    Description = excluded.Description,
                    Location = excluded.Location,
                    RecordedAt = excluded.RecordedAt,
                    Severity = excluded.Severity,
                    Priority = excluded.Priority,
                    Status = excluded.Status;
                """;
            command.Parameters.AddWithValue("$id", dispatchCase.Id.ToString());
            command.Parameters.AddWithValue("$callerName", dispatchCase.CallerName);
            command.Parameters.AddWithValue("$callerPhone", dispatchCase.CallerPhone);
            command.Parameters.AddWithValue("$incidentType", dispatchCase.IncidentType);
            command.Parameters.AddWithValue("$description", dispatchCase.Description);
            command.Parameters.AddWithValue("$location", dispatchCase.Location);
            command.Parameters.AddWithValue("$recordedAt", dispatchCase.RecordedAt.ToString("O"));
            command.Parameters.AddWithValue("$severity", (int)dispatchCase.Severity);
            command.Parameters.AddWithValue("$priority", (int)dispatchCase.Priority);
            command.Parameters.AddWithValue("$status", (int)dispatchCase.Status);
            command.ExecuteNonQuery();
        }

        DeleteCaseChildren(connection, transaction, dispatchCase.Id);

        foreach (var type in dispatchCase.RequiredUnitTypes)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO CaseRequiredUnits (CaseId, UnitType) VALUES ($caseId, $unitType);";
            command.Parameters.AddWithValue("$caseId", dispatchCase.Id.ToString());
            command.Parameters.AddWithValue("$unitType", (int)type);
            command.ExecuteNonQuery();
        }

        foreach (var assignment in dispatchCase.Assignments)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO CaseAssignments (CaseId, UnitId, AssignedAt, SignedOffAt)
                VALUES ($caseId, $unitId, $assignedAt, $signedOffAt);
                """;
            command.Parameters.AddWithValue("$caseId", dispatchCase.Id.ToString());
            command.Parameters.AddWithValue("$unitId", assignment.Unit.Id.ToString());
            command.Parameters.AddWithValue("$assignedAt", assignment.AssignedAt.ToString("O"));
            command.Parameters.AddWithValue("$signedOffAt", assignment.SignedOffAt?.ToString("O") ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void DeleteCaseChildren(SqliteConnection connection, SqliteTransaction transaction, Guid caseId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM CaseRequiredUnits WHERE CaseId = $caseId;
            DELETE FROM CaseAssignments WHERE CaseId = $caseId;
            """;
        command.Parameters.AddWithValue("$caseId", caseId.ToString());
        command.ExecuteNonQuery();
    }

    private void CreateDatabaseDirectory()
    {
        var dataSource = new SqliteConnectionStringBuilder(_connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:") return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (directory is not null) Directory.CreateDirectory(directory);
    }

    private void CreateTables()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Departments (
                Type INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Units (
                Id TEXT PRIMARY KEY,
                Identifier TEXT NOT NULL UNIQUE,
                Type INTEGER NOT NULL,
                Location TEXT NOT NULL,
                PersonnelCount INTEGER NOT NULL,
                Availability INTEGER NOT NULL,
                AssignedCaseId TEXT NULL,
                FOREIGN KEY (Type) REFERENCES Departments(Type)
            );

            CREATE TABLE IF NOT EXISTS Cases (
                Id TEXT PRIMARY KEY,
                CallerName TEXT NOT NULL,
                CallerPhone TEXT NOT NULL,
                IncidentType TEXT NOT NULL,
                Description TEXT NOT NULL,
                Location TEXT NOT NULL,
                RecordedAt TEXT NOT NULL,
                Severity INTEGER NOT NULL,
                Priority INTEGER NOT NULL,
                Status INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CaseRequiredUnits (
                CaseId TEXT NOT NULL,
                UnitType INTEGER NOT NULL,
                PRIMARY KEY (CaseId, UnitType),
                FOREIGN KEY (CaseId) REFERENCES Cases(Id)
            );

            CREATE TABLE IF NOT EXISTS CaseAssignments (
                CaseId TEXT NOT NULL,
                UnitId TEXT NOT NULL,
                AssignedAt TEXT NOT NULL,
                SignedOffAt TEXT NULL,
                PRIMARY KEY (CaseId, UnitId, AssignedAt),
                FOREIGN KEY (CaseId) REFERENCES Cases(Id),
                FOREIGN KEY (UnitId) REFERENCES Units(Id)
            );

            CREATE TABLE IF NOT EXISTS DispatchNotifications (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL,
                UnitIdentifier TEXT NOT NULL,
                DepartmentType INTEGER NOT NULL,
                CaseNumber TEXT NOT NULL,
                IncidentType TEXT NOT NULL,
                Location TEXT NOT NULL,
                Message TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private void SeedDepartments()
    {
        using var connection = OpenConnection();
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Departments;";
        if (Convert.ToInt32(countCommand.ExecuteScalar()) > 0) return;

        using var transaction = connection.BeginTransaction();
        AddDepartment(connection, transaction, ResponseUnitType.Medical, "Medical",
            [("MED-01", "Central Hospital", 2), ("MED-02", "North Clinic", 2)]);
        AddDepartment(connection, transaction, ResponseUnitType.Police, "Police",
            [("POL-01", "Central Station", 2), ("POL-02", "West Station", 2)]);
        AddDepartment(connection, transaction, ResponseUnitType.Fire, "Fire",
            [("FIR-01", "Fire Station 1", 4), ("FIR-02", "Fire Station 2", 4)]);
        transaction.Commit();
    }

    private static void AddDepartment(SqliteConnection connection, SqliteTransaction transaction,
        ResponseUnitType type, string name, IEnumerable<(string Identifier, string Location, int Personnel)> units)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Departments (Type, Name) VALUES ($type, $name);";
            command.Parameters.AddWithValue("$type", (int)type);
            command.Parameters.AddWithValue("$name", name);
            command.ExecuteNonQuery();
        }

        foreach (var unit in units)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Units (Id, Identifier, Type, Location, PersonnelCount, Availability, AssignedCaseId)
                VALUES ($id, $identifier, $type, $location, $personnelCount, $availability, NULL);
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$identifier", unit.Identifier);
            command.Parameters.AddWithValue("$type", (int)type);
            command.Parameters.AddWithValue("$location", unit.Location);
            command.Parameters.AddWithValue("$personnelCount", unit.Personnel);
            command.Parameters.AddWithValue("$availability", (int)UnitAvailability.Available);
            command.ExecuteNonQuery();
        }
    }

    private void LoadData()
    {
        using var connection = OpenConnection();
        LoadDepartmentsAndUnits(connection);
        LoadCases(connection);
    }

    private void LoadDepartmentsAndUnits(SqliteConnection connection)
    {
        var departmentRows = new List<(ResponseUnitType Type, string Name)>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Type, Name FROM Departments ORDER BY Type;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                departmentRows.Add(((ResponseUnitType)reader.GetInt32(0), reader.GetString(1)));
        }

        foreach (var row in departmentRows)
        {
            var units = new List<Unit>();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, Identifier, Location, PersonnelCount, Availability, AssignedCaseId
                FROM Units WHERE Type = $type ORDER BY Identifier;
                """;
            command.Parameters.AddWithValue("$type", (int)row.Type);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var unit = new Unit(
                    Guid.Parse(reader.GetString(0)), reader.GetString(1), row.Type, reader.GetString(2),
                    reader.GetInt32(3), (UnitAvailability)reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)));
                units.Add(unit);
                _units.Add(unit.Id, unit);
            }
            _departments.Add(row.Type, new Department(row.Type, row.Name, units));
        }
    }

    private void LoadCases(SqliteConnection connection)
    {
        var caseRows = new List<StoredCase>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, CallerName, CallerPhone, IncidentType, Description, Location,
                       RecordedAt, Severity, Priority, Status
                FROM Cases ORDER BY RecordedAt;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                caseRows.Add(new StoredCase(
                    Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5), ParseDate(reader.GetString(6)),
                    (Severity)reader.GetInt32(7), (Priority)reader.GetInt32(8), (CaseStatus)reader.GetInt32(9)));
            }
        }

        foreach (var row in caseRows)
        {
            var requiredTypes = LoadRequiredTypes(connection, row.Id);
            var dispatchCase = new Case(row.Id, row.CallerName, row.CallerPhone, row.IncidentType,
                row.Description, row.Location, row.Severity, requiredTypes, row.RecordedAt, row.Priority, row.Status);
            LoadAssignments(connection, dispatchCase);
            _cases.Add(dispatchCase.Id, dispatchCase);
        }
    }

    private static ResponseUnitType[] LoadRequiredTypes(SqliteConnection connection, Guid caseId)
    {
        var types = new List<ResponseUnitType>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UnitType FROM CaseRequiredUnits WHERE CaseId = $caseId ORDER BY UnitType;";
        command.Parameters.AddWithValue("$caseId", caseId.ToString());
        using var reader = command.ExecuteReader();
        while (reader.Read()) types.Add((ResponseUnitType)reader.GetInt32(0));
        return types.ToArray();
    }

    private void LoadAssignments(SqliteConnection connection, Case dispatchCase)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT UnitId, AssignedAt, SignedOffAt
            FROM CaseAssignments WHERE CaseId = $caseId ORDER BY AssignedAt;
            """;
        command.Parameters.AddWithValue("$caseId", dispatchCase.Id.ToString());
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var unitId = Guid.Parse(reader.GetString(0));
            if (!_units.TryGetValue(unitId, out var unit)) continue;
            dispatchCase.RestoreAssignment(unit, ParseDate(reader.GetString(1)),
                reader.IsDBNull(2) ? null : ParseDate(reader.GetString(2)));
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static DateTimeOffset ParseDate(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private sealed record StoredCase(Guid Id, string CallerName, string CallerPhone, string IncidentType,
        string Description, string Location, DateTimeOffset RecordedAt, Severity Severity,
        Priority Priority, CaseStatus Status);
}
