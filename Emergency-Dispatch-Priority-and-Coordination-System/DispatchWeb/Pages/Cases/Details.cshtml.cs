using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Cases;

/// <summary>
/// Builds one case report from the case record and its audit events.
/// Access is checked against the signed-in user's role before any report is displayed.
/// </summary>
public sealed class DetailsModel(ICaseRepository cases, IAuditRepository audit) : PageModel
{
    public Case DispatchCase { get; private set; } = null!;
    public IReadOnlyCollection<AuditEvent> AuditEvents { get; private set; } = [];
    public bool CanViewPrivateDetails { get; private set; }

    public IActionResult OnGet(Guid caseId)
    {
        var dispatchCase = cases.Get(caseId);
        if (dispatchCase is null) return NotFound();

        if (!CanView(dispatchCase))
            return RedirectToPage("/Account/AccessDenied");

        DispatchCase = dispatchCase;
        CanViewPrivateDetails = User.IsAdmin() || User.IsInRole(DemoRoles.Dispatcher);

        // The complete audit timeline is restricted to operational and admin users.
        if (CanViewPrivateDetails)
        {
            AuditEvents = audit.GetAll()
                .Where(item => item.CaseId == caseId)
                .OrderBy(item => item.CreatedAt)
                .ToArray();
        }

        return Page();
    }

    private bool CanView(Case dispatchCase)
    {
        if (User.IsAdmin() || User.IsInRole(DemoRoles.Dispatcher)) return true;

        if (User.IsInRole(DemoRoles.Department) &&
            Enum.TryParse<ResponseUnitType>(User.Scope(), true, out var departmentType))
        {
            return dispatchCase.RequiredUnitTypes.Contains(departmentType);
        }

        if (User.IsInRole(DemoRoles.ResponseUnit))
        {
            return dispatchCase.Assignments.Any(assignment =>
                assignment.Unit.Identifier.Equals(User.Scope(), StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
