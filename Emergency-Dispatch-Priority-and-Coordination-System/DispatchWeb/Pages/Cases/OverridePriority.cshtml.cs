using System.ComponentModel.DataAnnotations;
using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Cases;

public sealed class OverridePriorityModel(ICaseRepository cases, DispatchService dispatchService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid CaseId { get; set; }

    [BindProperty]
    public OverrideInput Input { get; set; } = new();

    public Case DispatchCase { get; private set; } = null!;

    public IActionResult OnGet()
    {
        if (!CanOverride()) return RedirectToPage("/Account/AccessDenied");
        if (!LoadCase()) return NotFound();
        Input.NewPriority = DispatchCase.Priority;
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!CanOverride()) return RedirectToPage("/Account/AccessDenied");
        if (!LoadCase()) return NotFound();
        if (!ModelState.IsValid) return Page();

        try
        {
            dispatchService.OverridePriority(CaseId, Input.NewPriority, Input.Reason,
                User.Identity?.Name ?? "Unknown user");
            return RedirectToPage("/Index", new { overridden = DispatchCase.CaseNumber });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private bool LoadCase()
    {
        DispatchCase = cases.Get(CaseId)!;
        return DispatchCase is not null;
    }

    private bool CanOverride() => User.IsAdmin() || User.IsInRole(DemoRoles.Dispatcher);

    public sealed class OverrideInput
    {
        [Display(Name = "New priority")]
        public Priority NewPriority { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
