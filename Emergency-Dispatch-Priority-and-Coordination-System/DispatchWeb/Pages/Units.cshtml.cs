using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace DispatchWeb.Pages;
public sealed class UnitsModel(IDepartmentRepository departments) : PageModel
{
    public IReadOnlyCollection<Department> Departments { get; private set; } = [];
    public void OnGet() => Departments = departments.GetAll();
}
