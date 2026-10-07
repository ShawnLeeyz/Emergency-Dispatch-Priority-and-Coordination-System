using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;
using DispatchWeb.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "EmergencyDispatch.DemoSession";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "Data", "dispatch.db");
var connectionString = builder.Configuration.GetConnectionString("DispatchDatabase") ?? $"Data Source={databasePath}";
builder.Services.AddSingleton(new SqliteDatabase(connectionString));
builder.Services.AddSingleton<ICaseRepository, SqliteCaseRepository>();
builder.Services.AddSingleton<IDepartmentRepository, SqliteDepartmentRepository>();
builder.Services.AddSingleton<IDispatchNotifier, SqliteDispatchNotifier>();
builder.Services.AddSingleton<IUserAccountRepository, SqliteUserAccountRepository>();
builder.Services.AddSingleton<IAuditRepository, SqliteAuditRepository>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<DemoAccountStore>();
builder.Services.AddSingleton<IPriorityStrategy, KeywordSeverityPriority>();
builder.Services.AddSingleton<IUnitAssignmentService, UnitAssignmentService>();
builder.Services.AddSingleton<DispatchService>();

var app = builder.Build();
app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RoleAccessMiddleware>();
app.MapRazorPages();
app.Run();

// Allows the test project to start the real web application in memory.
public partial class Program;
