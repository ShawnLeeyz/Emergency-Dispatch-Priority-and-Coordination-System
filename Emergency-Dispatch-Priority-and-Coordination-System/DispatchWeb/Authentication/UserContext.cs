using System.Security.Claims;

namespace DispatchWeb.Authentication;

public static class UserContext
{
    public static string Role(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public static string Scope(this ClaimsPrincipal user) => user.FindFirstValue("scope") ?? string.Empty;
    public static string DisplayName(this ClaimsPrincipal user) => user.FindFirstValue("display_name") ?? user.Identity?.Name ?? "User";
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(DemoRoles.Admin);
}
