namespace ClientAPI.Models;

// Original Core Data Service response model.
// Shape differs from NewCoreDataResponse — do not unify.
public class OriginalCoreDataResponse
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    // TODO: add fields as they appear in the real Original service contract
}
