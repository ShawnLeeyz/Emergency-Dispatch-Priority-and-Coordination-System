using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Admin;

public sealed class IndexModel(DemoAccountStore accounts, ICaseRepository cases, IDepartmentRepository departments) : PageModel
{
    public IReadOnlyCollection<DemoAccount> Accounts { get; private set; } = [];
    public int CaseCount { get; private set; }
    public int UnitCount { get; private set; }
    public void OnGet()
    {
        Accounts = accounts.GetAll();
        CaseCount = cases.GetAll().Count;
        UnitCount = departments.GetAll().Sum(department => department.Units.Count);
    }
}
