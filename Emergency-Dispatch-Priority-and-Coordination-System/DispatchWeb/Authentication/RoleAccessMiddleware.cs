namespace DispatchWeb.Authentication;

/// <summary>Stops signed-in users from opening pages outside their role and scope.</summary>
public sealed class RoleAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Public and unauthenticated requests continue to ASP.NET's normal authentication handling.
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsPublic(path))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (string.Equals(context.User.Role(), DemoRoles.Admin, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        // Each operational role receives only the routes needed for its own workflow.
        var role = context.User.Role();
        var allowed = role switch
        {
            DemoRoles.Dispatcher => path == "/" || path.StartsWith("/Cases/Create", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/Cases/OverridePriority", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/Cases/Details", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/History", StringComparison.OrdinalIgnoreCase),
            DemoRoles.Department => IsDepartmentPath(context, path) || path.StartsWith("/Units", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/Cases/Details", StringComparison.OrdinalIgnoreCase),
            DemoRoles.ResponseUnit => IsOwnUnitPath(context, path) ||
                                      path.StartsWith("/Cases/Details", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        if (allowed)
        {
            await next(context);
            return;
        }

        context.Response.Redirect("/Account/AccessDenied");
    }

    private static bool IsPublic(string path) =>
        path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase);

    private static bool IsDepartmentPath(HttpContext context, string path) =>
        path.StartsWith("/Departments/", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(context.Request.RouteValues["department"]?.ToString(), context.User.Scope(), StringComparison.OrdinalIgnoreCase);

    private static bool IsOwnUnitPath(HttpContext context, string path) =>
        path.StartsWith("/ResponseUnits/", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(context.Request.RouteValues["unit"]?.ToString(), context.User.Scope(), StringComparison.OrdinalIgnoreCase);
}
