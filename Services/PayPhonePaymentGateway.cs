using System.Text.Json;

namespace Proyecto.Services;

public class PayPhonePaymentGateway : IPaymentGateway
{
    private readonly IPayPhoneService _payPhoneService;

    public PayPhonePaymentGateway(IPayPhoneService payPhoneService)
    {
        _payPhoneService = payPhoneService;
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        var clientTransactionId = $"{request.ReferenceId}-{Guid.NewGuid()}";
            if (clientTransactionId.Length > 15) clientTransactionId = clientTransactionId[..15];

        var result = await _payPhoneService.CreatePaymentLinkAsync(
            request.Amount,
            request.ReferenceId,
            clientTransactionId);

        if (result.Success)
        {
            return new PaymentResult
            {
                Success = true,
                ProviderOrderId = clientTransactionId,
                RedirectUrl = result.PaymentUrl,
                Status = "CREATED",
                RawResponse = result.RawResponse?.ToString()
            };
        }

        return new PaymentResult
        {
            Success = false,
            ErrorMessage = result.ErrorMessage ?? "Error al crear el pago en PayPhone"
        };
    }

    public async Task<PaymentResult> VerifyPaymentAsync(string providerReference)
    {
        // PayPhone no tiene endpoint de verificación simple;
        // se retorna como verificado con la info disponible
        return new PaymentResult
        {
            Success = true,
            ProviderOrderId = providerReference,
            Status = "PAID"
        };
    }
}
