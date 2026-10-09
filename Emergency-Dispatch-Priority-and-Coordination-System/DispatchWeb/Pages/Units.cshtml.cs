using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DispatchWeb.Authentication;
namespace DispatchWeb.Pages;

/// <summary>Lists response units and handles permitted detail updates.</summary>
public sealed class UnitsModel(IDepartmentRepository departments, DispatchService dispatchService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ResponseUnitType? Department { get; set; }

    [BindProperty]
    public UnitInput Input { get; set; } = new();

    public IReadOnlyCollection<Department> Departments { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (!ApplyScope()) return RedirectToPage("/Account/AccessDenied");
        LoadDepartments();
        return Page();
    }

    public IActionResult OnPostUpdate()
    {
        // A department can update only its own units; the admin may update any unit.
        if (!User.IsAdmin() && (!User.IsInRole(DemoRoles.Department) || !string.Equals(User.Scope(), Input.Department.ToString(), StringComparison.OrdinalIgnoreCase)))
            return RedirectToPage("/Account/AccessDenied");
        Department = Input.Department;
        if (!ModelState.IsValid)
        {
            LoadDepartments();
            return Page();
        }

        try
        {
            dispatchService.UpdateUnit(Input.Department, Input.UnitId, Input.Location, Input.PersonnelCount,
                User.Identity?.Name ?? "Unknown user", Input.Latitude, Input.Longitude);
            TempData["Success"] = $"{Input.Identifier} details updated.";
            return RedirectToPage(new { department = Input.Department });
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            LoadDepartments();
            return Page();
        }
    }

    private void LoadDepartments()
    {
        // The optional filter keeps department users focused on their own unit list.
        var all = departments.GetAll();
        Departments = Department.HasValue ? all.Where(d => d.Type == Department).ToArray() : all;
    }

    private bool ApplyScope()
    {
        // Convert the scope claim into the department filter used by the page.
        if (User.IsAdmin()) return true;
        if (!User.IsInRole(DemoRoles.Department) || !Enum.TryParse<ResponseUnitType>(User.Scope(), true, out var type)) return false;
        if (Department.HasValue && Department != type) return false;
        Department = type;
        return true;
    }

    public sealed class UnitInput
    {
        public Guid UnitId { get; set; }
        public ResponseUnitType Department { get; set; }
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Location { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 30)]
        public int PersonnelCount { get; set; }
    }
}
