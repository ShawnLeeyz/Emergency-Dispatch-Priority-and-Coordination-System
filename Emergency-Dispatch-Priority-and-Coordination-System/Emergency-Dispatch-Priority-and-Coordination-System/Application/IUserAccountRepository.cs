using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Defines how the seeded demonstration accounts are stored and loaded.</summary>
public interface IUserAccountRepository
{
    void Add(UserAccount account);
    UserAccount? Get(string username);
    IReadOnlyCollection<UserAccount> GetAll();
}
