namespace DispatchWeb.Authentication;

public sealed class DemoAccountStore
{
    private readonly IReadOnlyDictionary<string, DemoAccount> _accounts;

    public DemoAccountStore(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "demo-accounts.txt");
        _accounts = File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(Parse)
            .ToDictionary(account => account.Username, StringComparer.OrdinalIgnoreCase);
    }

    public DemoAccount? Validate(string username, string password) =>
        _accounts.TryGetValue(username.Trim(), out var account) && account.Password == password
            ? account
            : null;

    public IReadOnlyCollection<DemoAccount> GetAll() => _accounts.Values.OrderBy(account => account.Role).ThenBy(account => account.Username).ToArray();

    private static DemoAccount Parse(string line)
    {
        var values = line.Split('|');
        if (values.Length != 5 || values.Take(4).Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Each demo account must use username|password|display name|role|scope format.");

        return new DemoAccount(values[0].Trim(), values[1], values[2].Trim(), values[3].Trim(),
            string.IsNullOrWhiteSpace(values[4]) ? null : values[4].Trim());
    }
}
