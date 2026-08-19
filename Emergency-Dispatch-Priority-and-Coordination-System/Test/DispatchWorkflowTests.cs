using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

[TestClass]
public sealed class DispatchWorkflowTests
{
    [TestMethod]
    public void CreateAndDispatch_AssignsFirstAvailableUnit_AndSendsNotification()
    {
        var cases = new InMemoryCaseRepository();
        var notifier = new InMemoryDispatchNotifier();
        var service = CreateService(cases, notifier);

        var result = service.CreateAndDispatch(Request("Alex", "Person unconscious", ResponseUnitType.Medical));

        Assert.AreEqual(Priority.High, result.Priority);
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
        Assert.AreEqual("MED-01", result.AssignedUnits.Single().Identifier);
        Assert.AreEqual(UnitAvailability.Unavailable, result.AssignedUnits.Single().Availability);
        Assert.HasCount(1, notifier.GetAll());
        Assert.AreEqual(ResponseUnitType.Medical, notifier.GetAll().Single().DepartmentType);
    }

    [TestMethod]
    public void SignOff_ReleasesOnlyThatUnit_AndFinalSignOffClosesCase()
    {
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases);
        var dispatchCase = service.CreateAndDispatch(Request("Alex", "Vehicle collision",
            ResponseUnitType.Medical, ResponseUnitType.Police));
        var medical = dispatchCase.AssignedUnits.Single(unit => unit.Type == ResponseUnitType.Medical);
        var police = dispatchCase.AssignedUnits.Single(unit => unit.Type == ResponseUnitType.Police);

        service.SignOffUnit(dispatchCase.Id, medical.Id, ResponseUnitType.Medical);

        Assert.AreEqual(UnitAvailability.Available, medical.Availability);
        Assert.AreEqual(UnitAvailability.Unavailable, police.Availability);
        Assert.AreEqual(CaseStatus.InProgress, dispatchCase.Status);
        CollectionAssert.AreEqual(new[] { police }, dispatchCase.AssignedUnits.ToArray());

        service.SignOffUnit(dispatchCase.Id, police.Id, ResponseUnitType.Police);

        Assert.AreEqual(UnitAvailability.Available, police.Availability);
        Assert.AreEqual(CaseStatus.Closed, dispatchCase.Status);
        Assert.IsFalse(dispatchCase.AssignedUnits.Any());
        Assert.HasCount(2, dispatchCase.Assignments.Where(assignment => !assignment.IsActive).ToArray());
    }

    [TestMethod]
    public void SignOff_AssignsReleasedUnitToOldestWaitingCase()
    {
        var cases = new InMemoryCaseRepository();
        var notifier = new InMemoryDispatchNotifier();
        var service = CreateService(cases, notifier);
        var first = service.CreateAndDispatch(Request("First", "Injury", ResponseUnitType.Medical));
        service.CreateAndDispatch(Request("Second", "Injury", ResponseUnitType.Medical));
        var waiting = service.CreateAndDispatch(Request("Waiting", "Injury", ResponseUnitType.Medical));
        var releasedUnit = first.AssignedUnits.Single();

        Assert.AreEqual(CaseStatus.Open, waiting.Status);
        Assert.IsTrue(waiting.IsWaitingFor(ResponseUnitType.Medical));

        service.SignOffUnit(first.Id, releasedUnit.Id, ResponseUnitType.Medical);

        Assert.AreEqual(CaseStatus.Closed, first.Status);
        Assert.AreEqual(CaseStatus.InProgress, waiting.Status);
        Assert.AreSame(releasedUnit, waiting.AssignedUnits.Single());
        Assert.AreEqual(waiting.Id, releasedUnit.AssignedCaseId);
        Assert.HasCount(3, notifier.GetAll());
    }

    [TestMethod]
    public void UnassigningAllActiveUnits_ReturnsIncompleteCaseToOpen()
    {
        var dispatchCase = new Case("Alex", "0211234567", "Report", "Details", "1 Queen St",
            Severity.Low, [ResponseUnitType.Police]);
        var unit = new Unit("POL-TEST", ResponseUnitType.Police, "Central", 2);
        dispatchCase.Assign(unit);

        dispatchCase.Unassign(unit.Id);

        Assert.AreEqual(CaseStatus.Open, dispatchCase.Status);
        Assert.AreEqual(UnitAvailability.Available, unit.Availability);
    }

    [TestMethod]
    public void Search_UsesOrSemanticsAcrossCallerCaseIdAndDate()
    {
        var repository = new InMemoryCaseRepository();
        var first = CaseAt("Alex", new DateTimeOffset(2026, 8, 16, 1, 0, 0, TimeSpan.Zero));
        var second = CaseAt("Jordan", new DateTimeOffset(2026, 8, 17, 1, 0, 0, TimeSpan.Zero));
        var third = CaseAt("Morgan", new DateTimeOffset(2026, 8, 18, 1, 0, 0, TimeSpan.Zero));
        repository.Add(first);
        repository.Add(second);
        repository.Add(third);

        var result = repository.Search("Alex", second.CaseNumber, new DateOnly(2026, 8, 18));

        CollectionAssert.AreEquivalent(new[] { first, second, third }, result.ToArray());
    }

    private static DispatchService CreateService(InMemoryCaseRepository cases, InMemoryDispatchNotifier? notifier = null) =>
        new(cases, new InMemoryDepartmentRepository(), new KeywordSeverityPriority(), notifier ?? new InMemoryDispatchNotifier());

    private static CreateCaseRequest Request(string caller, string description, params ResponseUnitType[] types) =>
        new(caller, "0211234567", "Emergency", description, "1 Queen St", Severity.Medium, types);

    private static Case CaseAt(string caller, DateTimeOffset recordedAt) =>
        new(caller, "0211234567", "Report", "Details", "1 Queen St", Severity.Low,
            [ResponseUnitType.Police], recordedAt);
}
