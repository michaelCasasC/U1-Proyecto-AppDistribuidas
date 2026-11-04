using System.Text.Json;

namespace Proyecto.Services;

public class PayPalPaymentGateway : IPaymentGateway
{
    private readonly IPayPalService _payPalService;

    public PayPalPaymentGateway(IPayPalService payPalService)
    {
        _payPalService = payPalService;
    }

    public string Metodo => "PayPal";

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var orderId = await _payPalService.CreateOrderAsync(
                request.Amount, request.Currency, request.ReturnUrl, request.CancelUrl, request.ReferenceId);

            return new PaymentResult
            {
                Success = true,
                ProviderOrderId = orderId,
                RedirectUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={orderId}",
                Status = "CREATED"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> VerifyPaymentAsync(string providerReference)
    {
        try
        {
            var capture = await _payPalService.CaptureOrderAsync(providerReference);

            return new PaymentResult
            {
                Success = capture.Success,
                ProviderOrderId = providerReference,
                TransactionId = capture.CaptureId,
                Status = capture.Status,
                PayerId = capture.PayerId,
                RawResponse = JsonSerializer.Serialize(capture)
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
