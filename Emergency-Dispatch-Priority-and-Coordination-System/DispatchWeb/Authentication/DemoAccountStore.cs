using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace DispatchWeb.Authentication;

public sealed class DemoAccountStore
{
    private readonly IUserAccountRepository _accounts;
    private readonly PasswordHasher _passwordHasher;

    public DemoAccountStore(IUserAccountRepository accounts, PasswordHasher passwordHasher)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        SeedDemoAccounts();
    }

    public DemoAccount? Validate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        // The database supplies the stored salt and hash. The entered password is hashed
        // with the same settings, then the two hashes are compared without decrypting anything.
        var storedAccount = _accounts.Get(username);
        if (storedAccount is null || !_passwordHasher.Verify(password, storedAccount.PasswordHash,
                storedAccount.PasswordSalt, storedAccount.HashIterations))
            return null;

        return ToDemoAccount(storedAccount);
    }

    public IReadOnlyCollection<DemoAccount> GetAll() =>
        _accounts.GetAll().Select(ToDemoAccount).ToArray();

    private void SeedDemoAccounts()
    {
        // These are fake university demonstration passwords. Each one is hashed with a new random
        // salt before storage, so the original password never appears in the SQLite database.
        Add("dispatch01", "dispatch-demo", "Alex Dispatcher", DemoRoles.Dispatcher, null);
        Add("dispatch02", "dispatch-demo", "Morgan Dispatcher", DemoRoles.Dispatcher, null);
        Add("medical01", "department-demo", "Jamie Medical Coordinator", DemoRoles.Department, "Medical");
        Add("medical02", "department-demo", "Taylor Medical Coordinator", DemoRoles.Department, "Medical");
        Add("police01", "department-demo", "Casey Police Coordinator", DemoRoles.Department, "Police");
        Add("police02", "department-demo", "Jordan Police Coordinator", DemoRoles.Department, "Police");
        Add("fire01", "department-demo", "Riley Fire Coordinator", DemoRoles.Department, "Fire");
        Add("fire02", "department-demo", "Avery Fire Coordinator", DemoRoles.Department, "Fire");
        Add("med01", "unit-demo", "MED-01 Crew", DemoRoles.ResponseUnit, "MED-01");
        Add("med02", "unit-demo", "MED-02 Crew", DemoRoles.ResponseUnit, "MED-02");
        Add("pol01", "unit-demo", "POL-01 Crew", DemoRoles.ResponseUnit, "POL-01");
        Add("pol02", "unit-demo", "POL-02 Crew", DemoRoles.ResponseUnit, "POL-02");
        Add("fir01", "unit-demo", "FIR-01 Crew", DemoRoles.ResponseUnit, "FIR-01");
        Add("fir02", "unit-demo", "FIR-02 Crew", DemoRoles.ResponseUnit, "FIR-02");
        Add("admin", "admin-demo", "Prototype Administrator", DemoRoles.Admin, null);
    }

    private void Add(string username, string password, string displayName, string role, string? scope)
    {
        if (_accounts.Get(username) is not null) return;
        var passwordHash = _passwordHasher.Create(password);
        _accounts.Add(new UserAccount(username, passwordHash.Hash, passwordHash.Salt,
            passwordHash.Iterations, displayName, role, scope));
    }

    private static DemoAccount ToDemoAccount(UserAccount account) =>
        new(account.Username, account.DisplayName, account.Role, account.Scope);
}
