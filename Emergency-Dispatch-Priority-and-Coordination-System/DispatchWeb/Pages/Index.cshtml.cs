using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages;
public sealed class IndexModel(ICaseRepository cases, IDepartmentRepository departments, IDispatchNotifier notifier, DispatchService dispatchService) : PageModel
{
    public IReadOnlyCollection<Case> Cases { get; private set; } = [];
    public IReadOnlyCollection<Department> Departments { get; private set; } = [];
    public IReadOnlyCollection<DispatchNotification> Notifications { get; private set; } = [];
    public void OnGet() { Cases = cases.GetAll(); Departments = departments.GetAll(); Notifications = notifier.GetAll().Take(5).ToArray(); }
    public IActionResult OnPostClose(Guid caseId)
    {
        try { dispatchService.CloseCase(caseId); }
        catch (KeyNotFoundException) { TempData["Error"] = "That case could not be found."; }
        return RedirectToPage();
    }
}
