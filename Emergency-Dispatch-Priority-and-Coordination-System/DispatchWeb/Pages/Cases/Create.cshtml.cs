using System.ComponentModel.DataAnnotations;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Cases;

public sealed class CreateModel(DispatchService dispatchService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var request = new CreateCaseRequest(
                Input.CallerName,
                Input.CallerPhone,
                Input.IncidentType,
                Input.Description,
                Input.Location,
                Input.Severity,
                Input.RequiredUnitTypes);

            var dispatchCase = dispatchService.CreateAndDispatch(request);
            return RedirectToPage("/Index", new { created = dispatchCase.CaseNumber });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Caller name")]
        public string CallerName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Caller phone")]
        public string CallerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Incident type")]
        public string IncidentType { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Incident description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Incident location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Reported severity")]
        public Severity Severity { get; set; } = Severity.Medium;

        [MinLength(1, ErrorMessage = "Select at least one response department.")]
        [Display(Name = "Required response departments")]
        public List<ResponseUnitType> RequiredUnitTypes { get; set; } = [];
    }
}
