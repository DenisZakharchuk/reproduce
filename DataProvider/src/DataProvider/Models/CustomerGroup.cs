namespace DataProvider.Models;

public class CustomerGroup
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? ConsumtionGroup { get; set; }
    public string? PaymentData { get; set; }
    public int MemberCount { get; set; }
}
