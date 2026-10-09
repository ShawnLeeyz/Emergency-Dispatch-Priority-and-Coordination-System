using System.Security.Claims;

namespace DispatchWeb.Authentication;

/// <summary>Names the roles used by the prototype's access checks.</summary>
public static class DemoRoles
{
    public const string Dispatcher = "Dispatcher";
    public const string Department = "Department";
    public const string ResponseUnit = "ResponseUnit";
    public const string Admin = "Admin";
}

/// <summary>Represents the safe account details placed in the sign-in cookie.</summary>
public sealed record DemoAccount(string Username, string DisplayName, string Role, string? Scope)
{
    public ClaimsPrincipal CreatePrincipal()
    {
        // Claims let pages read the user's role and department or unit scope.
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
