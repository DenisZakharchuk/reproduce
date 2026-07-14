namespace APINexus.Configuration;

public class UsersOptions
{
    public List<UserEntry> Accounts { get; set; } = [];
}

public class UserEntry
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
