namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

/// <summary>A stored system account. PasswordHash and PasswordSalt never contain the original password.</summary>
public sealed record UserAccount(
    string Username,
    string PasswordHash,
    string PasswordSalt,
    int HashIterations,
    string DisplayName,
    string Role,
    string? Scope);
