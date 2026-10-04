namespace DataProvider.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ConsumtionGroup { get; set; }
    public string? PaymentData { get; set; }
}
