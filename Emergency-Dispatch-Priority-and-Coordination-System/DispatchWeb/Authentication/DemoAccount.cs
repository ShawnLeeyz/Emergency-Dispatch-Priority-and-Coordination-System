using System.Security.Claims;

namespace DispatchWeb.Authentication;

public static class DemoRoles
{
    public const string Dispatcher = "Dispatcher";
    public const string Department = "Department";
    public const string ResponseUnit = "ResponseUnit";
    public const string Admin = "Admin";
}

public sealed record DemoAccount(string Username, string Password, string DisplayName, string Role, string? Scope)
{
    public ClaimsPrincipal CreatePrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Username),
            new("display_name", DisplayName),
            new(ClaimTypes.Role, Role)
        };
        if (!string.IsNullOrWhiteSpace(Scope)) claims.Add(new Claim("scope", Scope));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "DemoCookie"));
    }

    public string LandingPage => Role switch
    {
        DemoRoles.Dispatcher => "/",
        DemoRoles.Department => $"/Departments/{Scope}",
        DemoRoles.ResponseUnit => $"/ResponseUnits/{Scope}",
        DemoRoles.Admin => "/Admin",
        _ => "/"
    };
}
