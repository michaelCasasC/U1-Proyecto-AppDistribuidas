namespace Proyecto.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);
    Task<PaymentResult> VerifyPaymentAsync(string providerReference);
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
    public string ReferenceId { get; set; } = "";
    public string MetodoPago { get; set; } = "PayPal";
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? ProviderOrderId { get; set; }
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public string? PayerId { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}
