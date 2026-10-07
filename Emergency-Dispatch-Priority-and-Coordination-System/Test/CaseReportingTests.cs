using DispatchWeb.Authentication;
using DispatchWeb.Pages.Cases;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Test;

[TestClass]
public sealed class CaseReportingTests
{
    [TestMethod]
    public void DispatcherReport_LoadsCaseAndOrdersItsAuditTimeline()
    {
        // Arrange
        var cases = new InMemoryCaseRepository();
        var audit = new InMemoryAuditRepository();
        var dispatchCase = AssignedMedicalCase();
        cases.Add(dispatchCase);
        audit.Add(Event(dispatchCase, AuditEventTypes.UnitAssigned, 2));
        audit.Add(Event(dispatchCase, AuditEventTypes.CaseCreated, 1));
        var page = CreatePage(cases, audit, DispatchAccount());

        // Act
        var result = page.OnGet(dispatchCase.Id);

        // Assert
        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreSame(dispatchCase, page.DispatchCase);
        Assert.IsTrue(page.CanViewPrivateDetails);
        CollectionAssert.AreEqual(
            new[] { AuditEventTypes.CaseCreated, AuditEventTypes.UnitAssigned },
            page.AuditEvents.Select(item => item.EventType).ToArray());
    }

    [TestMethod]
    [DataRow(DemoRoles.Department, "Medical", true)]
    [DataRow(DemoRoles.Department, "Police", false)]
    [DataRow(DemoRoles.ResponseUnit, "MED-01", true)]
    [DataRow(DemoRoles.ResponseUnit, "MED-02", false)]
    public void OperationalReport_IsLimitedToSelectedDepartmentsAndAssignedUnits(
        string role, string scope, bool expectedAllowed)
    {
        // Arrange
        var cases = new InMemoryCaseRepository();
        var dispatchCase = AssignedMedicalCase();
        cases.Add(dispatchCase);
        var account = new DemoAccount("test-user", "Test User", role, scope);
        var page = CreatePage(cases, new InMemoryAuditRepository(), account);

        // Act
        var result = page.OnGet(dispatchCase.Id);

        // Assert
        if (expectedAllowed)
        {
            Assert.IsInstanceOfType<PageResult>(result);
            Assert.IsFalse(page.CanViewPrivateDetails);
            Assert.IsEmpty(page.AuditEvents);
        }
        else
        {
            var denied = Assert.IsInstanceOfType<RedirectToPageResult>(result);
            Assert.AreEqual("/Account/AccessDenied", denied.PageName);
        }
    }

    [TestMethod]
    public void MissingCaseReport_ReturnsNotFound()
    {
        // Arrange
        var page = CreatePage(new InMemoryCaseRepository(), new InMemoryAuditRepository(), DispatchAccount());

        // Act
        var result = page.OnGet(Guid.NewGuid());

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    private static DetailsModel CreatePage(InMemoryCaseRepository cases, InMemoryAuditRepository audit,
        DemoAccount account)
    {
        var context = new DefaultHttpContext { User = account.CreatePrincipal() };
        return new DetailsModel(cases, audit)
        {
            PageContext = new PageContext { HttpContext = context }
        };
    }

    private static Case AssignedMedicalCase()
    {
        var dispatchCase = new Case("Alex Morgan", "021 555 0100", "Medical emergency",
            "Caller requires medical assistance.", "10 Queen Street", Severity.High,
            [ResponseUnitType.Medical], new DateTimeOffset(2026, 10, 7, 1, 0, 0, TimeSpan.Zero),
            -36.8500, 174.7650);
        var unit = new Unit("MED-01", ResponseUnitType.Medical, "Central Station", 2,
            latitude: -36.8485, longitude: 174.7633);
        Assert.IsTrue(dispatchCase.Assign(unit, 0.23));
        return dispatchCase;
    }

    private static AuditEvent Event(Case dispatchCase, string eventType, int minute) =>
        new(Guid.NewGuid(), new DateTimeOffset(2026, 10, 7, 1, minute, 0, TimeSpan.Zero), eventType,
            "dispatch01", dispatchCase.Id, dispatchCase.CaseNumber, null, null, null, null);

    private static DemoAccount DispatchAccount() =>
        new("dispatch01", "Dispatcher", DemoRoles.Dispatcher, null);
}
