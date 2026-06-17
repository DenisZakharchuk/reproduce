namespace ClientAPI.Models;

// New Core Data Service response model.
// Shape differs from OriginalCoreDataResponse — do not unify.
public class NewCoreDataResponse
{
    public string Identifier { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    // TODO: add fields as they appear in the real New service contract
}
