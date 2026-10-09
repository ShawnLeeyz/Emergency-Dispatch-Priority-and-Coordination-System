using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages;

/// <summary>Displays a safe error page without exposing technical exception details.</summary>
public sealed class ErrorModel : PageModel
{
    public void OnGet()
    {
    }
}
