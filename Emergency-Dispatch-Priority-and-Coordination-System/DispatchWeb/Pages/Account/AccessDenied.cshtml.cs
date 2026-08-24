using DispatchWeb.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Account;

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
