using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Departments;

public sealed class DashboardModel(
    ICaseRepository cases,
    IDepartmentRepository departments,
    IDispatchNotifier notifier,
    DispatchService dispatchService) : PageModel
{
    public ResponseUnitType DepartmentType { get; private set; }
    public Department Department { get; private set; } = null!;
    public IReadOnlyCollection<Case> Cases { get; private set; } = [];
    public IReadOnlyCollection<DispatchNotification> Notifications { get; private set; } = [];

    public IActionResult OnGet(string department)
    {
        if (!TryLoad(department)) return NotFound();
        return Page();
    }

    public IActionResult OnPostSignOff(string department, Guid caseId, Guid unitId)
    {
        if (!Enum.TryParse<ResponseUnitType>(department, true, out var departmentType)) return NotFound();

        try
        {
            dispatchService.SignOffUnit(caseId, unitId, departmentType);
            TempData["Success"] = "Unit signed off. Availability and case status were updated automatically.";
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { department = departmentType });
    }

    private bool TryLoad(string department)
    {
        if (!Enum.TryParse<ResponseUnitType>(department, true, out var type)) return false;
        var selectedDepartment = departments.Get(type);
        if (selectedDepartment is null) return false;

        DepartmentType = type;
        Department = selectedDepartment;
        Cases = cases.GetAll()
            .Where(dispatchCase => dispatchCase.Status != CaseStatus.Closed && dispatchCase.RequiredUnitTypes.Contains(type))
            .ToArray();
        Notifications = notifier.GetAll().Where(notification => notification.DepartmentType == type).Take(6).ToArray();
        return true;
    }
}
