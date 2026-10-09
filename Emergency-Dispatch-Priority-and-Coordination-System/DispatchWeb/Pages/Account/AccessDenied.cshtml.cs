using DispatchWeb.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Account;

/// <summary>Sends a denied user back to the correct home page for their role.</summary>
public sealed class AccessDeniedModel : PageModel
{
    public string HomePage => User.Role() switch
    {
        DemoRoles.Department => $"/Departments/{User.Scope()}",
        DemoRoles.ResponseUnit => $"/ResponseUnits/{User.Scope()}",
        DemoRoles.Admin => "/Admin",
        _ => "/"
    };
}
