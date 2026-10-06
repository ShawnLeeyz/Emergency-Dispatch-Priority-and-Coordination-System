using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Test;

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

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(), $"dispatch-web-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DispatchDatabase"] = $"Data Source={_databasePath}"
                }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (File.Exists(_databasePath)) File.Delete(_databasePath);
        }
    }
}
