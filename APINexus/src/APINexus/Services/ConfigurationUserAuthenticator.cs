using APINexus.Configuration;
using APINexus.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace APINexus.Services;

/// <summary>
/// Validates credentials against the "Users" configuration section.
/// Password hashes are produced with <see cref="PasswordHasher{TUser}"/> — never store
/// plaintext passwords in configuration.
/// </summary>
internal sealed class ConfigurationUserAuthenticator(IOptionsMonitor<UsersOptions> options)
    : IUserAuthenticator
{
    private readonly PasswordHasher<string> _hasher = new();

    public AuthenticatedUser? Validate(string username, string password)
    {
        var account = options.CurrentValue.Accounts
            .FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

        if (account is null)
            return null;

        var result = _hasher.VerifyHashedPassword(username, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return new AuthenticatedUser(account.Username, account.Roles);
    }
}
