namespace APINexus.Models;

public record ApiSourceDescriptor(
    string Id,
    string Title,
    string SpecUrl,
    IReadOnlyList<string> RequiredRoles);
