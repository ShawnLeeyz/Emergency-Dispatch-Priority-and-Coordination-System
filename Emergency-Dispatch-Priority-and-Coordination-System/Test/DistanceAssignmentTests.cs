using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

/// <summary>Checks closest-unit selection, unavailable units, ties, and stored distances.</summary>
[TestClass]
public sealed class DistanceAssignmentTests
{
    [TestMethod]
    public void Assignment_SelectsClosestAvailableCompatibleUnit()
    {
        // Arrange
        var farUnit = new Unit("MED-01", ResponseUnitType.Medical, "Far station", 2, -36.9000, 174.8000);
        var closeUnit = new Unit("MED-02", ResponseUnitType.Medical, "Close station", 2, -36.8490, 174.7640);
        var departments = Repository(ResponseUnitType.Medical, farUnit, closeUnit);
        var service = CreateService(new InMemoryCaseRepository(), departments);

        // Act
        var dispatchCase = service.CreateAndDispatch(Request(-36.8485, 174.7633));

        // Assert
        Assert.AreSame(closeUnit, dispatchCase.AssignedUnits.Single());
        Assert.AreEqual(UnitAvailability.Available, farUnit.Availability);
        Assert.IsLessThan(0.1, dispatchCase.Assignments.Single().DistanceKilometres);
    }

    [TestMethod]
    public void Assignment_SkipsClosestUnitWhenItIsUnavailable()
    {
        // Arrange
        var unavailable = new Unit("MED-01", ResponseUnitType.Medical, "Closest", 2, -36.8485, 174.7633);
        var available = new Unit("MED-02", ResponseUnitType.Medical, "Next closest", 2, -36.8600, 174.7700);
        var blocker = new Case("Existing", "021 000 0000", "Existing", "Existing case", "Location",
            Severity.Low, [ResponseUnitType.Medical]);
        Assert.IsTrue(blocker.Assign(unavailable));
        var service = CreateService(new InMemoryCaseRepository(),
            Repository(ResponseUnitType.Medical, unavailable, available));

        // Act
        var dispatchCase = service.CreateAndDispatch(Request(-36.8485, 174.7633));

        // Assert
        Assert.AreSame(available, dispatchCase.AssignedUnits.Single());
        Assert.AreEqual(blocker.Id, unavailable.AssignedCaseId);
    }

    [TestMethod]
    public void EqualDistance_UsesUnitIdentifierAsRepeatableTieBreaker()
    {
        // Arrange
        var secondAlphabetically = new Unit("MED-02", ResponseUnitType.Medical, "Same place", 2, -36.8500, 174.7650);
        var firstAlphabetically = new Unit("MED-01", ResponseUnitType.Medical, "Same place", 2, -36.8500, 174.7650);
        var strategy = new UnitAssignmentService();
        var dispatchCase = CaseAt(-36.8485, 174.7633);

        // Act
        var selected = strategy.SelectClosestAvailable([secondAlphabetically, firstAlphabetically], dispatchCase);

        // Assert
        Assert.AreSame(firstAlphabetically, selected);
    }

    [TestMethod]
    public void DistanceCalculation_ProducesKnownApproximateResult()
    {
        // Arrange
        var strategy = new UnitAssignmentService();
        var dispatchCase = CaseAt(0, 0);
        var unit = new Unit("MED-01", ResponseUnitType.Medical, "One degree north", 2, 1, 0);

        // Act
        var distance = strategy.CalculateDistanceKilometres(dispatchCase, unit);

        // Assert
        Assert.AreEqual(111.19, distance, 0.1);
    }

    [TestMethod]
    public void InvalidCoordinates_AreRejectedByDomainObjects()
    {
        // Arrange and Act
        var caseException = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CaseAt(91, 0));
        var unitException = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new Unit("MED-01", ResponseUnitType.Medical, "Station", 2, 0, 181));

        // Assert
        Assert.AreEqual("latitude", caseException.ParamName);
        Assert.AreEqual("longitude", unitException.ParamName);
    }

    [TestMethod]
    public void QueuedCase_StoresDistanceWhenReleasedUnitIsAssigned()
    {
        // Arrange
        var unit = new Unit("FIR-01", ResponseUnitType.Fire, "Station", 4, -36.8500, 174.7600);
        var departments = Repository(ResponseUnitType.Fire, unit);
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases, departments);
        var active = service.CreateAndDispatch(FireRequest(-36.8500, 174.7600));
        var waiting = service.CreateAndDispatch(FireRequest(-36.8700, 174.7800));

        // Act
        service.SignOffUnit(active.Id, unit.Id, ResponseUnitType.Fire);

        // Assert
        Assert.AreSame(unit, waiting.AssignedUnits.Single());
        Assert.IsGreaterThan(0, waiting.Assignments.Single().DistanceKilometres);
    }

    [TestMethod]
    public void CoordinatesAndAssignmentDistance_SurviveDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var cases = new SqliteCaseRepository(firstDatabase);
            var departments = new SqliteDepartmentRepository(firstDatabase);
            var service = CreateService(cases, departments, new SqliteAuditRepository(firstDatabase),
                new SqliteDispatchNotifier(firstDatabase));

            // Act
            var created = service.CreateAndDispatch(Request(-36.8520, 174.7650));
            var expectedDistance = created.Assignments.Single().DistanceKilometres;
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var restored = new SqliteCaseRepository(reopenedDatabase).Get(created.Id)!;

            // Assert
            Assert.AreEqual(-36.8520, restored.Latitude, 0.000001);
            Assert.AreEqual(174.7650, restored.Longitude, 0.000001);
            Assert.AreEqual(expectedDistance, restored.Assignments.Single().DistanceKilometres, 0.000001);
            Assert.AreEqual(created.AssignedUnits.Single().Latitude, restored.AssignedUnits.Single().Latitude, 0.000001);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static DispatchService CreateService(ICaseRepository cases, IDepartmentRepository departments,
        IAuditRepository? audit = null, IDispatchNotifier? notifier = null) =>
        new(cases, departments, new KeywordSeverityPriority(), notifier ?? new InMemoryDispatchNotifier(),
            audit ?? new InMemoryAuditRepository(), new UnitAssignmentService());

    private static TestDepartmentRepository Repository(ResponseUnitType type, params Unit[] units) =>
        new(new Department(type, type.ToString(), units));

    private static CreateCaseRequest Request(double latitude, double longitude) =>
        new("Distance Caller", "021 555 0177", "Medical request", "No listed keyword", "Incident",
            Severity.Medium, [ResponseUnitType.Medical], latitude, longitude);

    private static CreateCaseRequest FireRequest(double latitude, double longitude) =>
        new("Distance Caller", "021 555 0177", "Fire request", "No listed keyword", "Incident",
            Severity.Medium, [ResponseUnitType.Fire], latitude, longitude);

    private static Case CaseAt(double latitude, double longitude) =>
        new("Distance Caller", "021 555 0177", "Request", "Details", "Incident",
            Severity.Medium, [ResponseUnitType.Medical], latitude: latitude, longitude: longitude);

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-distance-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }

    private sealed class TestDepartmentRepository(params Department[] departments) : IDepartmentRepository
    {
        public IReadOnlyCollection<Department> GetAll() => departments;
        public Department? Get(ResponseUnitType type) => departments.SingleOrDefault(item => item.Type == type);
        public void Save(Unit unit) { }
    }
}
