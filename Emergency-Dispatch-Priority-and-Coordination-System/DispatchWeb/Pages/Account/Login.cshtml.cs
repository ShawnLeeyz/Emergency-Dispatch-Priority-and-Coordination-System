using System.ComponentModel.DataAnnotations;
using DispatchWeb.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispatchWeb.Pages.Account;

public sealed class LoginModel(DemoAccountStore accounts) : PageModel
{
    [BindProperty] public LoginInput Input { get; set; } = new();
    public IReadOnlyCollection<DemoAccount> DemoAccounts => accounts.GetAll();

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect(GetLandingPage());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var account = accounts.Validate(Input.Username, Input.Password);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "The username or password is incorrect. Check the demo account details and try again.");
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, account.CreatePrincipal(),
            new AuthenticationProperties { IsPersistent = false });
        return LocalRedirect(account.LandingPage);
    }

    private string GetLandingPage() => User.Role() switch
    {
        DemoRoles.Department => $"/Departments/{User.Scope()}",
        DemoRoles.ResponseUnit => $"/ResponseUnits/{User.Scope()}",
        DemoRoles.Admin => "/Admin",
        _ => "/"
    };

    public sealed class LoginInput
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    }
}
