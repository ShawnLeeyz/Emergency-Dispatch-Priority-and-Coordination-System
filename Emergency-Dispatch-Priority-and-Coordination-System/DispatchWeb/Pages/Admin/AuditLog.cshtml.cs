using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Admin;

public sealed class AuditLogModel(IAuditRepository audit) : PageModel
{
    public IReadOnlyCollection<AuditEvent> Events { get; private set; } = [];

    public void OnGet()
    {
        Events = audit.GetAll();
    }
}
