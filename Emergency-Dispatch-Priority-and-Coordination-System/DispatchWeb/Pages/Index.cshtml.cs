using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages;

public sealed class IndexModel(ICaseRepository cases, IDepartmentRepository departments, IDispatchNotifier notifier) : PageModel
{
    public IReadOnlyCollection<Case> Cases { get; private set; } = [];
    public IReadOnlyCollection<Department> Departments { get; private set; } = [];
    public IReadOnlyCollection<DispatchNotification> Notifications { get; private set; } = [];
    public void OnGet()
    {
        Cases = cases.GetAll();
        Departments = departments.GetAll();
        Notifications = notifier.GetAll().Take(6).ToArray();
    }
}
