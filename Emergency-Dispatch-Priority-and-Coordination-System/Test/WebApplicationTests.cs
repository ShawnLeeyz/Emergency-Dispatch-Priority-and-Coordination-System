using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Test;

/// <summary>Uses real HTTP requests to check authentication, pages, and the demo workflow.</summary>
[TestClass]
public sealed class WebApplicationTests
{
    [TestMethod]
    public async Task UnauthenticatedUser_IsRedirectedToLogin()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/Cases/Create");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Account/Login", response.Headers.Location!.AbsolutePath);
    }

    [TestMethod]
    [DataRow("dispatch01", "dispatch-demo", "/Cases/Create", HttpStatusCode.OK)]
    [DataRow("dispatch01", "dispatch-demo", "/Departments/Police", HttpStatusCode.Redirect)]
    [DataRow("police01", "department-demo", "/Departments/Police", HttpStatusCode.OK)]
    [DataRow("police01", "department-demo", "/Departments/Fire", HttpStatusCode.Redirect)]
    [DataRow("pol01", "unit-demo", "/ResponseUnits/POL-01", HttpStatusCode.OK)]
    [DataRow("pol01", "unit-demo", "/ResponseUnits/POL-02", HttpStatusCode.Redirect)]
    [DataRow("admin", "admin-demo", "/Admin", HttpStatusCode.OK)]
    [DataRow("admin", "admin-demo", "/Admin/AuditLog", HttpStatusCode.OK)]
    [DataRow("dispatch01", "dispatch-demo", "/Admin/AuditLog", HttpStatusCode.Redirect)]
    [DataRow("dispatch01", "dispatch-demo", "/Cases/OverridePriority?caseId=00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    [DataRow("police01", "department-demo", "/Cases/OverridePriority?caseId=00000000-0000-0000-0000-000000000001", HttpStatusCode.Redirect)]
    [DataRow("dispatch01", "dispatch-demo", "/Cases/Details/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    [DataRow("police01", "department-demo", "/Cases/Details/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    [DataRow("pol01", "unit-demo", "/Cases/Details/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    public async Task LoginAndRoleAccess_UseTheRealCookieAndPagePipeline(
        string username, string password, string path, HttpStatusCode expectedStatus)
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateClient(factory);
        await Login(client, username, password);

        var response = await client.GetAsync(path);

        Assert.AreEqual(expectedStatus, response.StatusCode);
        if (expectedStatus == HttpStatusCode.Redirect)
            Assert.AreEqual("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    [TestMethod]
    public async Task InvalidLogin_ShowsAnErrorAndDoesNotCreateAUserSession()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await PostLogin(client, "dispatch01", "wrong-password");
        var page = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(page, "The username or password is incorrect.");

        var protectedPage = await client.GetAsync("/Cases/Create");
        Assert.AreEqual(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.AreEqual("/Account/Login", protectedPage.Headers.Location!.AbsolutePath);
    }

    [TestMethod]
    public async Task DepartmentDashboard_RenderedPageContainsAutomaticRefreshScript()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateClient(factory);
        await Login(client, "police01", "department-demo");

        var page = await client.GetStringAsync("/Departments/Police");

        StringAssert.Contains(page, "setInterval");
        StringAssert.Contains(page, "5000");
    }

    [TestMethod]
    public async Task MainDemonstrationWorkflow_CreatesReportsAndSignsOffThroughRealHttpPages()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        using var dispatcherClient = CreateClient(factory);
        await Login(dispatcherClient, "dispatch01", "dispatch-demo");
        var createForm = new Dictionary<string, string>
        {
            ["Input.CallerName"] = "HTTP Workflow Caller",
            ["Input.CallerPhone"] = "021 555 0180",
            ["Input.IncidentType"] = "Medical emergency",
            ["Input.Description"] = "Patient requires urgent assistance.",
            ["Input.Location"] = "40 Queen Street",
            ["Input.Latitude"] = "-36.8485",
            ["Input.Longitude"] = "174.7633",
            ["Input.Severity"] = "2",
            ["Input.RequiredUnitTypes"] = "Medical"
        };

        // Act - submit the real Razor form, open its report, then sign off as the assigned unit.
        var createResponse = await PostForm(dispatcherClient, "/Cases/Create", createForm);
        var cases = factory.Services.GetRequiredService<ICaseRepository>();
        var dispatchCase = cases.GetAll()
            .Single(item => item.CallerName == "HTTP Workflow Caller");
        var assignedUnit = dispatchCase.AssignedUnits.Single();
        var reportResponse = await dispatcherClient.GetAsync($"/Cases/Details/{dispatchCase.Id}");
        var report = await reportResponse.Content.ReadAsStringAsync();

        using var unitClient = CreateClient(factory);
        var unitUsername = assignedUnit.Identifier.Equals("MED-01", StringComparison.OrdinalIgnoreCase)
            ? "med01" : "med02";
        await Login(unitClient, unitUsername, "unit-demo");
        var unitPage = await unitClient.GetStringAsync($"/ResponseUnits/{assignedUnit.Identifier}");
        var signOffResponse = await PostForm(unitClient,
            $"/ResponseUnits/{assignedUnit.Identifier}?handler=SignOff",
            new Dictionary<string, string> { ["caseId"] = dispatchCase.Id.ToString() });

        var completed = cases.Get(dispatchCase.Id)!;

        // Assert
        Assert.AreEqual(HttpStatusCode.Redirect, createResponse.StatusCode);
        StringAssert.StartsWith(createResponse.Headers.Location!.ToString(), "/?created=CASE-");
        Assert.AreEqual(HttpStatusCode.OK, reportResponse.StatusCode);
        StringAssert.Contains(report, "HTTP Workflow Caller");
        StringAssert.Contains(report, "Response coordination");
        StringAssert.Contains(unitPage, dispatchCase.CaseNumber);
        StringAssert.Contains(unitPage, "Travel distance");
        Assert.AreEqual(HttpStatusCode.Redirect, signOffResponse.StatusCode);
        Assert.AreEqual(CaseStatus.Closed, completed.Status);
        Assert.IsNotNull(completed.Assignments.Single().SignedOffAt);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task Login(HttpClient client, string username, string password)
    {
        var response = await PostLogin(client, username, password);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"Login failed for {username}.");
    }

    private static async Task<HttpResponseMessage> PostLogin(HttpClient client, string username, string password)
    {
        var loginPage = await client.GetStringAsync("/Account/Login");
        var match = Regex.Match(loginPage,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.IsTrue(match.Success, "The login page did not contain an anti-forgery token.");

        var form = new Dictionary<string, string>
        {
            ["Input.Username"] = username,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value)
        };
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
    }

    private static async Task<HttpResponseMessage> PostForm(HttpClient client, string path,
        Dictionary<string, string> values)
    {
        var page = await client.GetStringAsync(path.Split('?')[0]);
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.IsTrue(match.Success, $"The page {path} did not contain an anti-forgery token.");
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(), $"dispatch-web-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DispatchDatabase"] = $"Data Source={_databasePath};Pooling=False"
                }));
            builder.ConfigureServices(services =>
            {
                // Program has already registered its database by this stage. Replacing that singleton
                // guarantees HTTP tests cannot read from or write to the developer's application file.
                services.RemoveAll<SqliteDatabase>();
                services.AddSingleton(new SqliteDatabase($"Data Source={_databasePath};Pooling=False"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (File.Exists(_databasePath)) File.Delete(_databasePath);
            if (File.Exists(_databasePath + ".key")) File.Delete(_databasePath + ".key");
        }
    }
}

