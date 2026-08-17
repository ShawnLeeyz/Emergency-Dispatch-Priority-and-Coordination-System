using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ICaseRepository, InMemoryCaseRepository>();
builder.Services.AddSingleton<IDepartmentRepository, InMemoryDepartmentRepository>();
builder.Services.AddSingleton<IDispatchNotifier, InMemoryDispatchNotifier>();
builder.Services.AddSingleton<IPriorityStrategy, KeywordSeverityPriority>();
builder.Services.AddSingleton<DispatchService>();

var app = builder.Build();
app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
