using APINexus.Models;

namespace APINexus.Services;

public interface IUserAuthenticator
{
    AuthenticatedUser? Validate(string username, string password);
}
