namespace APINexus.Models;

public record AuthenticatedUser(string Username, IReadOnlyList<string> Roles);
