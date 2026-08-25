using System.Diagnostics;
using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;
using Microsoft.AspNetCore.Http;

namespace Test;

/// <summary>
/// Automated coverage for the twelve initial test cases defined in report Section 7.
/// UI-only portions and design differences are recorded in docs/section-7-test-mismatches.md.
/// </summary>
[TestClass]
public sealed class PrototypeTesting
{
    [TestMethod]
    public void TC01_CompleteEmergencyDetails_CreateOneCaseWithRecordedData()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);
        var beforeSubmission = DateTimeOffset.UtcNow;

        var result = service.CreateAndDispatch(Request(
            caller: "John Smith",
            phone: "021 123 4567",
            incidentType: "Vehicle collision",
            description: "Two vehicles blocking the road",
            location: "25 Queen Street",
            severity: Severity.High,
            ResponseUnitType.Police,
            ResponseUnitType.Medical));

        Assert.AreEqual(result, cases.Get(result.Id));
        Assert.HasCount(1, cases.GetAll());
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.CaseNumber));
        Assert.IsTrue(result.RecordedAt >= beforeSubmission && result.RecordedAt <= DateTimeOffset.UtcNow);
        Assert.AreEqual("John Smith", result.CallerName);
        Assert.AreEqual("021 123 4567", result.CallerPhone);
        Assert.AreEqual("Vehicle collision", result.IncidentType);
        Assert.AreEqual("Two vehicles blocking the road", result.Description);
        Assert.AreEqual("25 Queen Street", result.Location);
        Assert.AreEqual(Severity.High, result.Severity);
        CollectionAssert.AreEquivalent(
            new[] { ResponseUnitType.Police, ResponseUnitType.Medical },
            result.RequiredUnitTypes.ToArray());
    }

    [TestMethod]
    public void TC02_IncompleteEmergencyDetails_AreRejectedWithoutCreatingCase()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);

        var missingLocation = Assert.ThrowsExactly<ArgumentException>(() =>
            service.CreateAndDispatch(Request(location: "")));
        var missingDepartment = Assert.ThrowsExactly<ArgumentException>(() =>
            service.CreateAndDispatch(new CreateCaseRequest(
                "John Smith", "021 123 4567", "Emergency", "Routine request",
                "25 Queen Street", Severity.Medium, [])));

        Assert.AreEqual("This field is required. (Parameter 'location')", missingLocation.Message);
        StringAssert.Contains(missingDepartment.Message, "At least one response type is required.");
        Assert.IsEmpty(cases.GetAll());
    }

    [TestMethod]
    public void TC03_Submission_CreatesPrioritisesStoresAndAssignsMultiDepartmentCase()
    {
        var cases = new InMemoryCaseRepository();
        var departments = StandardDepartments();
        var service = CreateService(cases, departments);

        var result = service.CreateAndDispatch(Request(
            description: "Two vehicles blocking the road after a collision",
            severity: Severity.High,
            requiredTypes: [ResponseUnitType.Police, ResponseUnitType.Medical]));

        Assert.AreSame(result, cases.Get(result.Id));
        Assert.AreEqual(Priority.Medium, result.Priority);
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
        CollectionAssert.AreEqual(
            new[] { "POL-01", "MED-01" },
            result.AssignedUnits.Select(unit => unit.Identifier).ToArray());
        Assert.IsTrue(result.AssignedUnits.All(unit => unit.Availability == UnitAvailability.Unavailable));
    }

    [TestMethod]
    [DataRow("Person is unconscious", Severity.Low, Priority.High)]
    [DataRow("Vehicle collision", Severity.Low, Priority.Medium)]
    [DataRow("Routine welfare request", Severity.High, Priority.High)]
    [DataRow("Routine welfare request", Severity.Medium, Priority.Medium)]
    [DataRow("Routine welfare request", Severity.Low, Priority.Low)]
    public void TC04_PrototypePriorityPolicy_ReturnsItsDefinedOutcome(
        string description,
        Severity severity,
        Priority expected)
    {
        var dispatchCase = new Case("John Smith", "021 123 4567", "Emergency", description,
            "25 Queen Street", severity, [ResponseUnitType.Medical]);

        var actual = new KeywordSeverityPriority().Calculate(dispatchCase);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TC05_MultiDepartmentSubmission_CompletesWithinTwoSecondsWithoutDataLoss()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);
        var request = Request(
            caller: "John Smith",
            phone: "021 123 4567",
            incidentType: "Building fire",
            description: "Fire with possible injuries",
            location: "25 Queen Street",
            severity: Severity.High,
            ResponseUnitType.Fire,
            ResponseUnitType.Medical,
            ResponseUnitType.Police);
        var timer = Stopwatch.StartNew();

        var result = service.CreateAndDispatch(request);
        timer.Stop();

        Assert.IsTrue(timer.Elapsed < TimeSpan.FromSeconds(2), $"Dispatch took {timer.Elapsed.TotalMilliseconds:N0} ms.");
        Assert.AreEqual(request.CallerName, result.CallerName);
        Assert.AreEqual(request.CallerPhone, result.CallerPhone);
        Assert.AreEqual(request.IncidentType, result.IncidentType);
        Assert.AreEqual(request.Description, result.Description);
        Assert.AreEqual(request.Location, result.Location);
        Assert.AreEqual(request.Severity, result.Severity);
        CollectionAssert.AreEquivalent(request.RequiredUnitTypes.ToArray(), result.RequiredUnitTypes.ToArray());
        CollectionAssert.AreEquivalent(
            new[] { ResponseUnitType.Fire, ResponseUnitType.Medical, ResponseUnitType.Police },
            result.AssignedUnits.Select(unit => unit.Type).ToArray());
    }

    [TestMethod]
    public void TC06_Assignment_SkipsUnavailableUnitAndSelectsFirstAvailableUnit()
    {
        var p01 = new Unit("P-01", ResponseUnitType.Police, "Central", 2);
        var p02 = new Unit("P-02", ResponseUnitType.Police, "North", 2);
        var p03 = new Unit("P-03", ResponseUnitType.Police, "South", 2);
        Occupy(p01);
        var departments = new TestDepartmentRepository(
            new Department(ResponseUnitType.Police, "Police", [p01, p02, p03]));
        var service = CreateService(new InMemoryCaseRepository(), departments);

        var result = service.CreateAndDispatch(Request(requiredTypes: [ResponseUnitType.Police]));

        Assert.AreEqual("P-02", result.AssignedUnits.Single().Identifier);
        Assert.AreEqual(UnitAvailability.Unavailable, p01.Availability);
        Assert.AreEqual(UnitAvailability.Unavailable, p02.Availability);
        Assert.AreEqual(UnitAvailability.Available, p03.Availability);
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
    }

    [TestMethod]
    public void TC07_NoAvailableUnit_QueuesCaseUntilAUnitSignsOff()
    {
        var cases = new InMemoryCaseRepository();
        var fire01 = new Unit("F-01", ResponseUnitType.Fire, "Central", 4);
        var fire02 = new Unit("F-02", ResponseUnitType.Fire, "North", 4);
        var departments = new TestDepartmentRepository(
            new Department(ResponseUnitType.Fire, "Fire", [fire01, fire02]));
        var service = CreateService(cases, departments);
        var first = service.CreateAndDispatch(Request(caller: "First", requiredTypes: [ResponseUnitType.Fire]));
        service.CreateAndDispatch(Request(caller: "Second", requiredTypes: [ResponseUnitType.Fire]));

        var waiting = service.CreateAndDispatch(Request(caller: "Waiting", requiredTypes: [ResponseUnitType.Fire]));

        Assert.AreEqual(CaseStatus.Open, waiting.Status);
        Assert.IsTrue(waiting.IsWaitingFor(ResponseUnitType.Fire));
        Assert.IsEmpty(waiting.AssignedUnits);

        service.SignOffUnit(first.Id, fire01.Id, ResponseUnitType.Fire);

        Assert.AreEqual(CaseStatus.InProgress, waiting.Status);
        Assert.AreSame(fire01, waiting.AssignedUnits.Single());
        Assert.AreEqual(UnitAvailability.Unavailable, fire01.Availability);
    }

    [TestMethod]
    public async Task TC08_ConcurrentSubmissions_CannotAssignOneUnitToTwoCases()
    {
        var cases = new InMemoryCaseRepository();
        var onlyUnit = new Unit("A-01", ResponseUnitType.Medical, "Central", 2);
        var departments = new TestDepartmentRepository(
            new Department(ResponseUnitType.Medical, "Medical", [onlyUnit]));
        var service = CreateService(cases, departments);
        using var start = new ManualResetEventSlim(false);

        Task<Case> Submit(string caller) => Task.Run(() =>
        {
            start.Wait();
            return service.CreateAndDispatch(Request(caller: caller, requiredTypes: [ResponseUnitType.Medical]));
        });

        var firstTask = Submit("Caller one");
        var secondTask = Submit("Caller two");
        start.Set();
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.AreEqual(1, results.Count(dispatchCase => dispatchCase.AssignedUnits.Contains(onlyUnit)));
        Assert.AreEqual(1, results.Count(dispatchCase => dispatchCase.Status == CaseStatus.Open));
        Assert.AreEqual(UnitAvailability.Unavailable, onlyUnit.Availability);
        Assert.HasCount(2, cases.GetAll());
        Assert.AreEqual(results.Single(dispatchCase => dispatchCase.AssignedUnits.Contains(onlyUnit)).Id, onlyUnit.AssignedCaseId);
    }

    [TestMethod]
    public void TC09_DepartmentProjection_ContainsOnlyItsCasesAndReflectsChanges()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);
        var policeCase = service.CreateAndDispatch(Request(caller: "Police one", requiredTypes: [ResponseUnitType.Police]));
        service.CreateAndDispatch(Request(caller: "Fire one", requiredTypes: [ResponseUnitType.Fire]));

        var initialPoliceDashboard = ActiveCasesFor(cases, ResponseUnitType.Police);
        var secondPoliceCase = service.CreateAndDispatch(Request(caller: "Police two", requiredTypes: [ResponseUnitType.Police]));
        var refreshedPoliceDashboard = ActiveCasesFor(cases, ResponseUnitType.Police);

        CollectionAssert.AreEqual(new[] { policeCase }, initialPoliceDashboard);
        CollectionAssert.AreEquivalent(new[] { policeCase, secondPoliceCase }, refreshedPoliceDashboard);
        Assert.IsTrue(refreshedPoliceDashboard.All(dispatchCase =>
            dispatchCase.RequiredUnitTypes.Contains(ResponseUnitType.Police)));
    }

    [TestMethod]
    public void TC10_CaseRemainsInProgressUntilFinalUnitSignsOff()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);
        var dispatchCase = service.CreateAndDispatch(Request(
            requiredTypes: [ResponseUnitType.Police, ResponseUnitType.Medical]));
        var police = dispatchCase.AssignedUnits.Single(unit => unit.Type == ResponseUnitType.Police);
        var medical = dispatchCase.AssignedUnits.Single(unit => unit.Type == ResponseUnitType.Medical);

        service.SignOffUnit(dispatchCase.Id, police.Id, ResponseUnitType.Police);

        Assert.AreEqual(UnitAvailability.Available, police.Availability);
        Assert.AreEqual(UnitAvailability.Unavailable, medical.Availability);
        Assert.AreEqual(CaseStatus.InProgress, dispatchCase.Status);

        service.SignOffUnit(dispatchCase.Id, medical.Id, ResponseUnitType.Medical);

        Assert.AreEqual(UnitAvailability.Available, medical.Availability);
        Assert.AreEqual(CaseStatus.Closed, dispatchCase.Status);
    }

    [TestMethod]
    public void TC11_RemovingAllActiveUnits_ReturnsIncompleteCaseToOpen()
    {
        var dispatchCase = new Case("John Smith", "021 123 4567", "Emergency", "Details",
            "25 Queen Street", Severity.Low, [ResponseUnitType.Police]);
        var unit = new Unit("P-03", ResponseUnitType.Police, "Central", 2);
        Assert.IsTrue(dispatchCase.Assign(unit));

        var removed = dispatchCase.Unassign(unit.Id);

        Assert.IsTrue(removed);
        Assert.AreEqual(CaseStatus.Open, dispatchCase.Status);
        Assert.AreEqual(UnitAvailability.Available, unit.Availability);
        Assert.IsEmpty(dispatchCase.AssignedUnits);
    }

    [TestMethod]
    [DataRow(DemoRoles.Dispatcher, null, "/Cases/Create", null, true)]
    [DataRow(DemoRoles.Dispatcher, null, "/Departments/Police", "Police", false)]
    [DataRow(DemoRoles.Department, "Police", "/Departments/Police", "Police", true)]
    [DataRow(DemoRoles.Department, "Police", "/Departments/Fire", "Fire", false)]
    [DataRow(DemoRoles.ResponseUnit, "POL-01", "/ResponseUnits/POL-01", null, true)]
    [DataRow(DemoRoles.ResponseUnit, "POL-01", "/ResponseUnits/POL-02", null, false)]
    public async Task TC12_RoleAccessMiddleware_RestrictsPagesToPermittedRoleAndScope(
        string role,
        string? scope,
        string path,
        string? routeScope,
        bool expectedAllowed)
    {
        var nextCalled = false;
        var middleware = new RoleAccessMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext
        {
            User = new DemoAccount("tester", "password", "Test User", role, scope).CreatePrincipal()
        };
        context.Request.Path = path;
        if (path.StartsWith("/Departments/", StringComparison.OrdinalIgnoreCase))
            context.Request.RouteValues["department"] = routeScope;
        if (path.StartsWith("/ResponseUnits/", StringComparison.OrdinalIgnoreCase))
            context.Request.RouteValues["unit"] = path.Split('/').Last();

        await middleware.InvokeAsync(context);

        Assert.AreEqual(expectedAllowed, nextCalled);
        if (!expectedAllowed)
            Assert.AreEqual("/Account/AccessDenied", context.Response.Headers.Location.ToString());
    }

    private static DispatchService CreateService(
        InMemoryCaseRepository cases,
        IDepartmentRepository? departments = null,
        IDispatchNotifier? notifier = null) =>
        new(cases, departments ?? StandardDepartments(), new KeywordSeverityPriority(),
            notifier ?? new InMemoryDispatchNotifier());

    private static TestDepartmentRepository StandardDepartments() => new(
        new Department(ResponseUnitType.Medical, "Medical",
            [new Unit("MED-01", ResponseUnitType.Medical, "Central", 2), new Unit("MED-02", ResponseUnitType.Medical, "North", 2)]),
        new Department(ResponseUnitType.Police, "Police",
            [new Unit("POL-01", ResponseUnitType.Police, "Central", 2), new Unit("POL-02", ResponseUnitType.Police, "North", 2)]),
        new Department(ResponseUnitType.Fire, "Fire",
            [new Unit("FIR-01", ResponseUnitType.Fire, "Central", 4), new Unit("FIR-02", ResponseUnitType.Fire, "North", 4)]));

    private static CreateCaseRequest Request(
        string caller = "John Smith",
        string phone = "021 123 4567",
        string incidentType = "Emergency",
        string description = "Routine request",
        string location = "25 Queen Street",
        Severity severity = Severity.Medium,
        params ResponseUnitType[] requiredTypes)
    {
        var selectedTypes = requiredTypes.Length == 0 ? new[] { ResponseUnitType.Medical } : requiredTypes;
        return new CreateCaseRequest(caller, phone, incidentType, description, location, severity, selectedTypes);
    }

    private static CreateCaseRequest Request(
        string caller = "John Smith",
        string phone = "021 123 4567",
        string incidentType = "Emergency",
        string description = "Routine request",
        string location = "25 Queen Street",
        Severity severity = Severity.Medium,
        IReadOnlyCollection<ResponseUnitType>? requiredTypes = null) =>
        new(caller, phone, incidentType, description, location, severity,
            requiredTypes ?? [ResponseUnitType.Medical]);

    private static void Occupy(Unit unit)
    {
        var blocker = new Case("Existing caller", "021 000 0000", "Existing incident", "Details",
            "Existing location", Severity.Low, [unit.Type]);
        Assert.IsTrue(blocker.Assign(unit));
    }

    private static Case[] ActiveCasesFor(ICaseRepository cases, ResponseUnitType departmentType) =>
        cases.GetAll()
            .Where(dispatchCase => dispatchCase.Status != CaseStatus.Closed &&
                                   dispatchCase.RequiredUnitTypes.Contains(departmentType))
            .ToArray();

    private sealed class TestDepartmentRepository(params Department[] departments) : IDepartmentRepository
    {
        public IReadOnlyCollection<Department> GetAll() => departments;
        public Department? Get(ResponseUnitType type) => departments.SingleOrDefault(department => department.Type == type);
    }
}
