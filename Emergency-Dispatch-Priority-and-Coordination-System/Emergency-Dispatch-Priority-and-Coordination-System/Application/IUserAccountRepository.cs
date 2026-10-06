using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public interface IUserAccountRepository
{
    void Add(UserAccount account);
    UserAccount? Get(string username);
    IReadOnlyCollection<UserAccount> GetAll();
}
