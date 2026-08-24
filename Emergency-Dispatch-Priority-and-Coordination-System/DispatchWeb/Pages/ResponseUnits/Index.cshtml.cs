using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.ResponseUnits;

public sealed class IndexModel(IDepartmentRepository departments, ICaseRepository cases, IDispatchNotifier notifier, DispatchService dispatchService) : PageModel
{
    public Unit Unit { get; private set; } = null!;
    public Case? ActiveCase { get; private set; }
    public DispatchNotification? Notification { get; private set; }

    public IActionResult OnGet(string unit) => Load(unit) ? Page() : NotFound();

    public IActionResult OnPostSignOff(string unit, Guid caseId)
    {
        if (!Load(unit) || Unit.AssignedCaseId != caseId)
        {
            TempData["Error"] = "This unit does not have that active assignment.";
            return RedirectToPage(new { unit });
        }

        try
        {
            dispatchService.SignOffUnit(caseId, Unit.Id, Unit.Type);
            TempData["Success"] = $"{Unit.Identifier} signed off. Unit availability and case status were updated.";
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }
        return RedirectToPage(new { unit });
    }

    private bool Load(string unit)
    {
        if (!User.IsAdmin() && !string.Equals(User.Scope(), unit, StringComparison.OrdinalIgnoreCase)) return false;
        Unit = departments.GetAll().SelectMany(department => department.Units).SingleOrDefault(candidate => candidate.Identifier.Equals(unit, StringComparison.OrdinalIgnoreCase))!;
        if (Unit is null) return false;
        ActiveCase = Unit.AssignedCaseId.HasValue ? cases.Get(Unit.AssignedCaseId.Value) : null;
        Notification = notifier.GetAll().FirstOrDefault(item => item.UnitIdentifier.Equals(Unit.Identifier, StringComparison.OrdinalIgnoreCase));
        return true;
    }
}
