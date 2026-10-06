using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

[TestClass]
public sealed class SqlitePersistenceTests
{
    [TestMethod]
    public void CreatedCaseAndAssignment_SurviveDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstStore = CreateStore(databasePath);
            var service = CreateService(firstStore.Cases, firstStore.Departments);

            // Act
            var created = service.CreateAndDispatch(new CreateCaseRequest(
                "Alex Morgan", "021 555 0101", "Vehicle collision", "Two vehicles blocking traffic",
                "25 Queen Street", Severity.High, [ResponseUnitType.Police]));
            var reopenedStore = CreateStore(databasePath);
            var restored = reopenedStore.Cases.Get(created.Id);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(created.CaseNumber, restored.CaseNumber);
            Assert.AreEqual("Alex Morgan", restored.CallerName);
            Assert.AreEqual(Priority.High, restored.Priority);
            Assert.AreEqual(Priority.High, restored.CalculatedPriority);
            Assert.AreEqual(CaseStatus.InProgress, restored.Status);
            Assert.AreEqual(created.AssignedUnits.Single().Identifier, restored.AssignedUnits.Single().Identifier);
            Assert.AreEqual(UnitAvailability.Unavailable, restored.AssignedUnits.Single().Availability);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void UnitUpdate_SurvivesDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstStore = CreateStore(databasePath);
            var service = CreateService(firstStore.Cases, firstStore.Departments);
            var unit = firstStore.Departments.Get(ResponseUnitType.Police)!.Units.First();

            // Act
            service.UpdateUnit(ResponseUnitType.Police, unit.Id, "Airport station", 5);
            var reopenedStore = CreateStore(databasePath);
            var restoredUnit = reopenedStore.Departments.Get(ResponseUnitType.Police)!.Units
                .Single(candidate => candidate.Id == unit.Id);

            // Assert
            Assert.AreEqual("Airport station", restoredUnit.Location);
            Assert.AreEqual(5, restoredUnit.PersonnelCount);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void CaseClosureAndReleasedUnit_SurviveDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstStore = CreateStore(databasePath);
            var service = CreateService(firstStore.Cases, firstStore.Departments);
            var dispatchCase = service.CreateAndDispatch(new CreateCaseRequest(
                "Jamie Lee", "021 555 0102", "Medical call", "Patient requires assistance",
                "10 Main Road", Severity.Medium, [ResponseUnitType.Medical]));
            var unit = dispatchCase.AssignedUnits.Single();

            // Act
            service.SignOffUnit(dispatchCase.Id, unit.Id, ResponseUnitType.Medical);
            var reopenedStore = CreateStore(databasePath);
            var restoredCase = reopenedStore.Cases.Get(dispatchCase.Id)!;
            var restoredUnit = reopenedStore.Departments.Get(ResponseUnitType.Medical)!.Units
                .Single(candidate => candidate.Id == unit.Id);

            // Assert
            Assert.AreEqual(CaseStatus.Closed, restoredCase.Status);
            Assert.IsEmpty(restoredCase.AssignedUnits);
            Assert.HasCount(1, restoredCase.Assignments);
            Assert.IsNotNull(restoredCase.Assignments.Single().SignedOffAt);
            Assert.AreEqual(UnitAvailability.Available, restoredUnit.Availability);
            Assert.IsNull(restoredUnit.AssignedCaseId);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void DispatchNotification_SurvivesDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var cases = new SqliteCaseRepository(firstDatabase);
            var departments = new SqliteDepartmentRepository(firstDatabase);
            var notifier = new SqliteDispatchNotifier(firstDatabase);
            var service = new DispatchService(cases, departments, new KeywordSeverityPriority(), notifier,
                new SqliteAuditRepository(firstDatabase));

            // Act
            var dispatchCase = service.CreateAndDispatch(new CreateCaseRequest(
                "Morgan Reid", "021 555 0103", "Police assistance", "Officer requested",
                "15 Harbour Street", Severity.Medium, [ResponseUnitType.Police]));
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var restoredNotification = new SqliteDispatchNotifier(reopenedDatabase).GetAll().Single();

            // Assert
            Assert.AreEqual(dispatchCase.CaseNumber, restoredNotification.CaseNumber);
            Assert.AreEqual(ResponseUnitType.Police, restoredNotification.DepartmentType);
            Assert.AreEqual(dispatchCase.AssignedUnits.Single().Identifier, restoredNotification.UnitIdentifier);
            Assert.AreEqual("15 Harbour Street", restoredNotification.Location);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static (SqliteCaseRepository Cases, SqliteDepartmentRepository Departments) CreateStore(string path)
    {
        var database = new SqliteDatabase($"Data Source={path}");
        return (new SqliteCaseRepository(database), new SqliteDepartmentRepository(database));
    }

    private static DispatchService CreateService(ICaseRepository cases, IDepartmentRepository departments) =>
        new(cases, departments, new KeywordSeverityPriority(), new InMemoryDispatchNotifier(),
            new InMemoryAuditRepository());

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-persistence-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}
