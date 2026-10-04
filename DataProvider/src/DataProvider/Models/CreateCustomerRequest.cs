namespace DataProvider.Models;

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ConsumtionGroup { get; set; }
    public string? PaymentData { get; set; }
}
