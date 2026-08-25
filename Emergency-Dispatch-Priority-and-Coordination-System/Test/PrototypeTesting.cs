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
        var notifier = new InMemoryDispatchNotifier();
        var service = CreateService(cases, departments, notifier);

        var result = service.CreateAndDispatch(Request(
            description: "Two vehicles blocking the road after a collision",
            severity: Severity.High,
            requiredTypes: [ResponseUnitType.Police, ResponseUnitType.Medical]));

        Assert.AreSame(result, cases.Get(result.Id));
        Assert.AreEqual(Priority.High, result.Priority);
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
        CollectionAssert.AreEqual(
            new[] { "POL-01", "MED-01" },
            result.AssignedUnits.Select(unit => unit.Identifier).ToArray());
        Assert.IsTrue(result.AssignedUnits.All(unit => unit.Availability == UnitAvailability.Unavailable));
        CollectionAssert.AreEquivalent(
            new[] { "POL-01", "MED-01" },
            notifier.GetAll().Select(notification => notification.UnitIdentifier).ToArray());
        Assert.IsTrue(notifier.GetAll().All(notification =>
            notification.CaseNumber == result.CaseNumber &&
            notification.IncidentType == result.IncidentType &&
            notification.Location == result.Location));
    }

    [TestMethod]
    [DynamicData(nameof(AppendixPriorityCases))]
    public void TC04_AppendixDepartmentKeywordsTakePrecedence_ThenSeverityIsFallback(
        ResponseUnitType department,
        string incidentType,
        string description,
        Severity severity,
        Priority expected)
    {
        var dispatchCase = new Case("John Smith", "021 123 4567", incidentType, description,
            "25 Queen Street", severity, [department]);

        var actual = new KeywordSeverityPriority().Calculate(dispatchCase);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TC04_MultiDepartmentCase_UsesHighestMatchingAppendixPriority()
    {
        var dispatchCase = new Case("John Smith", "021 123 4567", "Emergency",
            "Police report trespassing while Medical reports chest pain", "25 Queen Street",
            Severity.Low, [ResponseUnitType.Police, ResponseUnitType.Medical]);

        var actual = new KeywordSeverityPriority().Calculate(dispatchCase);

        Assert.AreEqual(Priority.High, actual);
    }

    [TestMethod]
    public void Appendix02_NotificationFailure_DoesNotBreakCriticalDispatchWorkflow()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases, notifier: new ThrowingNotifier());

        var result = service.CreateAndDispatch(Request(requiredTypes: [ResponseUnitType.Medical]));

        Assert.AreSame(result, cases.Get(result.Id));
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
        Assert.AreEqual(UnitAvailability.Unavailable, result.AssignedUnits.Single().Availability);
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
    public void TC09_DepartmentProjection_ContainsOnlyItsCasesAndIncludesNewCasesAfterRefresh()
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
    public void TC11_HistorySearch_ReturnsMatchesForCallerCaseIdOrDate()
    {
        var cases = new InMemoryCaseRepository();
        var callerMatch = CaseAt("Alex Morgan", new DateTimeOffset(2026, 8, 20, 1, 0, 0, TimeSpan.Zero));
        var idMatch = CaseAt("Jordan Lee", new DateTimeOffset(2026, 8, 21, 1, 0, 0, TimeSpan.Zero));
        var dateMatch = CaseAt("Taylor Brown", new DateTimeOffset(2026, 8, 22, 1, 0, 0, TimeSpan.Zero));
        var noMatch = CaseAt("Sam Wilson", new DateTimeOffset(2026, 8, 23, 1, 0, 0, TimeSpan.Zero));
        cases.Add(callerMatch);
        cases.Add(idMatch);
        cases.Add(dateMatch);
        cases.Add(noMatch);

        var results = cases.Search("Alex", idMatch.CaseNumber, new DateOnly(2026, 8, 22));

        CollectionAssert.AreEquivalent(new[] { callerMatch, idMatch, dateMatch }, results.ToArray());
        CollectionAssert.DoesNotContain(results.ToArray(), noMatch);
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

    public static IEnumerable<object[]> AppendixPriorityCases()
    {
        foreach (var term in new[]
                 {
                     "gun", "firearm", "shooting", "weapon", "armed", "knife", "stabbing", "kill", "murder",
                     "hostage", "kidnapping", "assault", "attacking", "suicide", "unconscious", "unresponsive", "bleeding"
                 })
            yield return [ResponseUnitType.Police, "Emergency", $"Report of {term}", Severity.Low, Priority.High];

        foreach (var term in new[]
                 {
                     "fleeing", "running away", "suspect", "intruder", "fight", "brawl", "punching", "burglary",
                     "breaking in", "robbery", "mugging", "domestic", "arguing", "screaming", "yelling",
                     "trespassing", "vandalism", "smashing"
                 })
            yield return [ResponseUnitType.Police, "Emergency", $"Report of {term}", Severity.Low, Priority.Medium];

        foreach (var term in new[]
                 {
                     "unconscious", "unresponsive", "passed out", "not breathing", "choking", "heart attack",
                     "chest pain", "cardiac", "stroke", "face drooping", "severe bleeding", "arterial"
                 })
            yield return [ResponseUnitType.Medical, "Emergency", $"Patient is {term}", Severity.Low, Priority.High];

        foreach (var term in new[]
                 {
                     "broken bone", "fracture", "conscious fall", "fall while conscious", "seizure has stopped",
                     "stopped seizure", "moderate burn", "burn moderate", "severe pain", "dizzy", "fainting"
                 })
            yield return [ResponseUnitType.Medical, "Emergency", $"Patient reports {term}", Severity.Low, Priority.Medium];

        foreach (var term in new[]
                 {
                     "trapped", "inside", "structure fire", "house fire", "building on fire", "explosion", "building gas leak",
                     "chemical", "hazmat", "spill", "wildfire"
                 })
            yield return [ResponseUnitType.Fire, "Emergency", $"Report of {term}", Severity.Low, Priority.High];

        foreach (var term in new[]
                 {
                     "vehicle fire", "small wildfire", "grass fire", "scrub fire", "brush fire", "tree fire",
                     "smell of smoke", "electrical sparks", "fire alarm", "smoke detector", "dumpster fire"
                 })
            yield return [ResponseUnitType.Fire, "Emergency", $"Report of {term}", Severity.Low, Priority.Medium];

        yield return [ResponseUnitType.Medical, "Routine welfare request", "No listed keyword", Severity.High, Priority.High];
        yield return [ResponseUnitType.Medical, "Routine welfare request", "No listed keyword", Severity.Medium, Priority.Medium];
        yield return [ResponseUnitType.Medical, "Routine welfare request", "No listed keyword", Severity.Low, Priority.Low];
        yield return [ResponseUnitType.Police, "Heart attack", "No Police keyword", Severity.Low, Priority.Low];
        yield return [ResponseUnitType.Medical, "Chest pain", "Reported in incident type", Severity.Low, Priority.High];
    }

    private static Case CaseAt(string caller, DateTimeOffset recordedAt) =>
        new(caller, "021 123 4567", "Report", "Details", "25 Queen Street",
            Severity.Low, [ResponseUnitType.Police], recordedAt);

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

    private sealed class ThrowingNotifier : IDispatchNotifier
    {
        public void Notify(Unit unit, Case dispatchCase) => throw new InvalidOperationException("Notification unavailable.");
        public IReadOnlyCollection<DispatchNotification> GetAll() => [];
    }
}
