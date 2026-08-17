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
        var service = new DispatchService(cases, new InMemoryDepartmentRepository(), new KeywordSeverityPriority(), notifier);

        var result = service.CreateAndDispatch(new("Alex", "0211234567", "Medical emergency", "Person unconscious", "1 Queen St", Severity.Low, [ResponseUnitType.Medical]));

        Assert.AreEqual(Priority.High, result.Priority);
        Assert.AreEqual(CaseStatus.InProgress, result.Status);
        Assert.AreEqual("MED-01", result.AssignedUnits.Single().Identifier);
        Assert.AreEqual(UnitAvailability.Unavailable, result.AssignedUnits.Single().Availability);
        Assert.HasCount(1, notifier.GetAll());
    }

    [TestMethod]
    public void CloseCase_ReleasesAllAssignedUnits_AndClosesCase()
    {
        var cases = new InMemoryCaseRepository();
        var service = new DispatchService(cases, new InMemoryDepartmentRepository(), new KeywordSeverityPriority(), new InMemoryDispatchNotifier());
        var result = service.CreateAndDispatch(new("Alex", "0211234567", "Collision", "Vehicle accident", "1 Queen St", Severity.Medium, [ResponseUnitType.Medical, ResponseUnitType.Police]));

        service.CloseCase(result.Id);

        Assert.AreEqual(CaseStatus.Closed, result.Status);
        Assert.IsFalse(result.AssignedUnits.Any());
    }

    [TestMethod]
    public void Search_ReturnsCasesMatchingEitherCallerOrCaseNumber()
    {
        var repository = new InMemoryCaseRepository();
        var first = new Case("Jordan", "0211234567", "Report", "Details", "1 Queen St", Severity.Low, [ResponseUnitType.Police]);
        repository.Add(first);

        CollectionAssert.Contains(repository.Search("Jordan", null).ToList(), first);
        CollectionAssert.Contains(repository.Search(first.CaseNumber, null).ToList(), first);
    }
}
