using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

/// <summary>Checks that important actions create complete and persistent audit records.</summary>
[TestClass]
public sealed class AuditLoggingTests
{
    [TestMethod]
    public void CaseCreationAndAssignment_RecordUserCaseAndUnitDetails()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var departments = new InMemoryDepartmentRepository();
        var service = CreateService(cases, departments, audit);

        // Act
        var dispatchCase = service.CreateAndDispatch(Request(ResponseUnitType.Police), "dispatch01");
        var events = audit.GetAll();

        // Assert
        Assert.HasCount(2, events);
        var created = events.Single(item => item.EventType == AuditEventTypes.CaseCreated);
        Assert.AreEqual("dispatch01", created.PerformedBy);
        Assert.AreEqual(dispatchCase.Id, created.CaseId);
        Assert.AreEqual(dispatchCase.CaseNumber, created.CaseNumber);
        StringAssert.Contains(created.NewValue, "Police");

        var assigned = events.Single(item => item.EventType == AuditEventTypes.UnitAssigned);
        Assert.AreEqual(dispatchCase.AssignedUnits.Single().Identifier, assigned.UnitIdentifier);
        Assert.AreEqual("Available", assigned.OldValue);
        Assert.AreEqual("Assigned", assigned.NewValue);
    }

    [TestMethod]
    public void UnitUpdate_RecordsOldAndNewValues()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var departments = new InMemoryDepartmentRepository();
        var service = CreateService(cases, departments, audit);
        var unit = departments.Get(ResponseUnitType.Fire)!.Units.First();

        // Act
        service.UpdateUnit(ResponseUnitType.Fire, unit.Id, "Eastern Station", 6, "fire01");
        var auditEvent = audit.GetAll().Single();

        // Assert
        Assert.AreEqual(AuditEventTypes.UnitUpdated, auditEvent.EventType);
        Assert.AreEqual("fire01", auditEvent.PerformedBy);
        Assert.AreEqual(unit.Identifier, auditEvent.UnitIdentifier);
        StringAssert.Contains(auditEvent.OldValue, "Personnel: 4");
        StringAssert.Contains(auditEvent.NewValue, "Eastern Station");
        StringAssert.Contains(auditEvent.NewValue, "Personnel: 6");
    }

    [TestMethod]
    public void FinalSignOff_RecordsSignOffAndCaseClosure()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var departments = new InMemoryDepartmentRepository();
        var service = CreateService(cases, departments, audit);
        var dispatchCase = service.CreateAndDispatch(Request(ResponseUnitType.Medical), "dispatch01");
        var unit = dispatchCase.AssignedUnits.Single();

        // Act
        service.SignOffUnit(dispatchCase.Id, unit.Id, unit.Type, "med01");
        var events = audit.GetAll();

        // Assert
        Assert.IsTrue(events.Any(item => item.EventType == AuditEventTypes.UnitSignedOff &&
                                         item.PerformedBy == "med01" &&
                                         item.UnitIdentifier == unit.Identifier));
        Assert.IsTrue(events.Any(item => item.EventType == AuditEventTypes.CaseClosed &&
                                         item.CaseId == dispatchCase.Id &&
                                         item.NewValue == CaseStatus.Closed.ToString()));
    }

    [TestMethod]
    public void AuditEvents_SurviveDatabaseRestartAndRemainNewestFirst()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var firstAudit = new SqliteAuditRepository(firstDatabase);
            var service = CreateService(
                new SqliteCaseRepository(firstDatabase),
                new SqliteDepartmentRepository(firstDatabase),
                firstAudit,
                new SqliteDispatchNotifier(firstDatabase));

            // Act
            var dispatchCase = service.CreateAndDispatch(Request(ResponseUnitType.Fire), "dispatch02");
            var unit = dispatchCase.AssignedUnits.Single();
            service.SignOffUnit(dispatchCase.Id, unit.Id, unit.Type, "fir01");
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var restoredEvents = new SqliteAuditRepository(reopenedDatabase).GetAll().ToArray();

            // Assert
            Assert.HasCount(4, restoredEvents);
            Assert.AreEqual(AuditEventTypes.CaseClosed, restoredEvents[0].EventType);
            Assert.AreEqual(AuditEventTypes.CaseCreated, restoredEvents[^1].EventType);
            Assert.IsTrue(restoredEvents.All(item => item.CaseId == dispatchCase.Id));
            Assert.IsTrue(restoredEvents.All(item => !string.IsNullOrWhiteSpace(item.PerformedBy)));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static DispatchService CreateService(ICaseRepository cases, IDepartmentRepository departments,
        IAuditRepository audit, IDispatchNotifier? notifier = null) =>
        new(cases, departments, new KeywordSeverityPriority(),
            notifier ?? new InMemoryDispatchNotifier(), audit, new UnitAssignmentService());

    private static CreateCaseRequest Request(ResponseUnitType type) =>
        new("Audit Caller", "021 555 0199", "Emergency", "Audit test case",
            "1 Test Street", Severity.Medium, [type]);

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-audit-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}
