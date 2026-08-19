using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace DispatchWeb.Pages;
public sealed class HistoryModel(ICaseRepository cases) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }
    public IReadOnlyCollection<Case> Cases { get; private set; } = [];
    public void OnGet() => Cases = cases.Search(Search, Date);
}
