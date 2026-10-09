using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

/// <summary>Connects demonstration account storage to SQLite.</summary>
public sealed class SqliteUserAccountRepository(SqliteDatabase database) : IUserAccountRepository
{
    public void Add(UserAccount account) => database.AddAccount(account);
    public UserAccount? Get(string username) => database.GetAccount(username);
    public IReadOnlyCollection<UserAccount> GetAll() => database.GetAccounts();
}
