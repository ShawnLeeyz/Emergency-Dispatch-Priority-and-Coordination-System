using DispatchWeb.Authentication;
using DispatchWeb.Pages.Cases;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Test;

[TestClass]
public sealed class PriorityOverrideTests
{
    [TestMethod]
    public void ValidOverride_PreservesCalculatedPriorityAndRecordsFullAuditEvent()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases, new InMemoryDepartmentRepository(), audit);
        var dispatchCase = service.CreateAndDispatch(LowPriorityRequest(), "dispatch01");

        // Act
        service.OverridePriority(dispatchCase.Id, Priority.High,
            "Caller reported an immediate threat.", "dispatch01");
        var auditEvent = audit.GetAll().Single(item => item.EventType == AuditEventTypes.PriorityOverridden);

        // Assert
        Assert.AreEqual(Priority.Low, dispatchCase.CalculatedPriority);
        Assert.AreEqual(Priority.High, dispatchCase.Priority);
        Assert.AreEqual("dispatch01", auditEvent.PerformedBy);
        Assert.AreEqual(dispatchCase.Id, auditEvent.CaseId);
        Assert.AreEqual("Low", auditEvent.OldValue);
        Assert.AreEqual("High", auditEvent.NewValue);
        Assert.AreEqual("Caller reported an immediate threat.", auditEvent.Reason);
    }

    [TestMethod]
    public void MissingReason_IsRejectedWithoutChangingPriorityOrWritingOverrideAudit()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases, new InMemoryDepartmentRepository(), audit);
        var dispatchCase = service.CreateAndDispatch(LowPriorityRequest(), "dispatch01");

        // Act
        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            service.OverridePriority(dispatchCase.Id, Priority.High, "   ", "dispatch01"));

        // Assert
        StringAssert.Contains(exception.Message, "reason is required");
        Assert.AreEqual(Priority.Low, dispatchCase.Priority);
        Assert.IsFalse(audit.GetAll().Any(item => item.EventType == AuditEventTypes.PriorityOverridden));
    }

    [TestMethod]
    public void SamePriorityAndClosedCase_AreRejected()
    {
        // Arrange
        var audit = new InMemoryAuditRepository();
        var cases = new InMemoryCaseRepository();
        var service = CreateService(cases, new InMemoryDepartmentRepository(), audit);
        var dispatchCase = service.CreateAndDispatch(LowPriorityRequest(), "dispatch01");

        // Act and Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            service.OverridePriority(dispatchCase.Id, Priority.Low, "No actual change", "dispatch01"));

        var unit = dispatchCase.AssignedUnits.Single();
        service.SignOffUnit(dispatchCase.Id, unit.Id, unit.Type, "med01");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.OverridePriority(dispatchCase.Id, Priority.High, "Case already closed", "dispatch01"));
    }

    [TestMethod]
    public void PriorityOverrideAndAuditEvent_SurviveDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var firstCases = new SqliteCaseRepository(firstDatabase);
            var firstAudit = new SqliteAuditRepository(firstDatabase);
            var service = CreateService(firstCases, new SqliteDepartmentRepository(firstDatabase), firstAudit,
                new SqliteDispatchNotifier(firstDatabase));
            var dispatchCase = service.CreateAndDispatch(LowPriorityRequest(), "dispatch02");

            // Act
            service.OverridePriority(dispatchCase.Id, Priority.Medium,
                "Dispatcher confirmed additional risk.", "dispatch02");
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var restoredCase = new SqliteCaseRepository(reopenedDatabase).Get(dispatchCase.Id)!;
            var restoredAudit = new SqliteAuditRepository(reopenedDatabase).GetAll()
                .Single(item => item.EventType == AuditEventTypes.PriorityOverridden);

            // Assert
            Assert.AreEqual(Priority.Low, restoredCase.CalculatedPriority);
            Assert.AreEqual(Priority.Medium, restoredCase.Priority);
            Assert.AreEqual("Low", restoredAudit.OldValue);
            Assert.AreEqual("Medium", restoredAudit.NewValue);
            Assert.AreEqual("Dispatcher confirmed additional risk.", restoredAudit.Reason);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void OverridePage_AllowsDispatcherAndRejectsDepartmentUser()
    {
        // Arrange
        var cases = new InMemoryCaseRepository();
        var departments = new InMemoryDepartmentRepository();
        var service = CreateService(cases, departments, new InMemoryAuditRepository());
        var dispatchCase = service.CreateAndDispatch(LowPriorityRequest());
        var dispatcherPage = CreatePage(cases, service, dispatchCase.Id,
            new DemoAccount("dispatch01", "Dispatcher", DemoRoles.Dispatcher, null));
        var departmentPage = CreatePage(cases, service, dispatchCase.Id,
            new DemoAccount("medical01", "Medical", DemoRoles.Department, "Medical"));

        // Act
        var dispatcherResult = dispatcherPage.OnGet();
        var departmentResult = departmentPage.OnGet();

        // Assert
        Assert.IsInstanceOfType<PageResult>(dispatcherResult);
        var denied = Assert.IsInstanceOfType<RedirectToPageResult>(departmentResult);
        Assert.AreEqual("/Account/AccessDenied", denied.PageName);
    }

    private static OverridePriorityModel CreatePage(ICaseRepository cases, DispatchService service,
        Guid caseId, DemoAccount account)
    {
        var context = new DefaultHttpContext { User = account.CreatePrincipal() };
        return new OverridePriorityModel(cases, service)
        {
            CaseId = caseId,
            PageContext = new PageContext { HttpContext = context }
        };
    }

    private static DispatchService CreateService(ICaseRepository cases, IDepartmentRepository departments,
        IAuditRepository audit, IDispatchNotifier? notifier = null) =>
        new(cases, departments, new KeywordSeverityPriority(),
            notifier ?? new InMemoryDispatchNotifier(), audit);

    private static CreateCaseRequest LowPriorityRequest() =>
        new("Priority Caller", "021 555 0188", "Routine request", "No listed keyword",
            "2 Test Street", Severity.Low, [ResponseUnitType.Medical]);

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-priority-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}
