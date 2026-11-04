using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;

namespace Proyecto.Services
{
    public interface IPayPalService
    {
        Task<string> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl, string referenceId);
        Task<PayPalCaptureResult> CaptureOrderAsync(string orderId);
        Task<Order> GetOrderAsync(string orderId);
    }

    public class PayPalCaptureResult
    {
        public bool Success { get; set; }
        public string? CaptureId { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? PayerId { get; set; }
        public string? ErrorMessage { get; set; }
        public Order? Order { get; set; }
    }

    public class PayPalService : IPayPalService
    {
        private readonly PayPalHttpClient _client;
        private readonly IConfiguration _configuration;

        public PayPalService(IConfiguration configuration)
        {
            _configuration = configuration;
            var clientId = _configuration["PayPal:ClientId"] ?? _configuration["PAYPAL_CLIENT_ID"]
                ?? throw new InvalidOperationException("PayPal ClientId not configured");
            var clientSecret = _configuration["PayPal:ClientSecret"] ?? _configuration["PAYPAL_CLIENT_SECRET"]
                ?? throw new InvalidOperationException("PayPal ClientSecret not configured");

            var environment = new SandboxEnvironment(clientId, clientSecret);
            _client = new PayPalHttpClient(environment);
        }

        public async Task<string> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl, string referenceId)
        {
            var requestId = Guid.NewGuid().ToString("N");

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.Headers.Add("PayPal-Request-Id", requestId);
                request.RequestBody(BuildOrderRequest(amount, currency, returnUrl, cancelUrl, referenceId));

                try
                {
                    var response = await _client.Execute(request);
                    var order = response.Result<Order>();

                    var approveLink = order.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;
                    if (string.IsNullOrEmpty(approveLink))
                    {
                        throw new InvalidOperationException("No approve link returned from PayPal");
                    }

                    return order.Id;
                }
                catch (Exception exception) when (attempt < 3 && IsTransientNetworkError(exception))
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt));
                }
            }

            throw new InvalidOperationException("No se pudo crear la orden de PayPal después de varios intentos.");
        }

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId)
        {
            var request = new OrdersCaptureRequest(orderId);
            request.RequestBody(new OrderActionRequest());

            try
            {
                var response = await _client.Execute(request);
                var order = response.Result<Order>();

                var capture = order.PurchaseUnits?
                    .FirstOrDefault()?
                    .Payments?
                    .Captures?
                    .FirstOrDefault();

                return new PayPalCaptureResult
                {
                    Success = capture?.Status == "COMPLETED",
                    CaptureId = capture?.Id,
                    Status = capture?.Status,
                    Amount = decimal.TryParse(capture?.Amount?.Value, out var amt) ? amt : 0,
                    Currency = capture?.Amount?.CurrencyCode,
                    PayerId = order.Payer?.PayerId,
                    Order = order
                };
            }
            catch (Exception ex)
            {
                return new PayPalCaptureResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<Order> GetOrderAsync(string orderId)
        {
            var request = new OrdersGetRequest(orderId);
            var response = await _client.Execute(request);
            return response.Result<Order>();
        }

        private OrderRequest BuildOrderRequest(decimal amount, string currency, string returnUrl, string cancelUrl, string referenceId)
        {
            return new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    BrandName = "EmprendeMarket",
                    LandingPage = "BILLING",
                    UserAction = "PAY_NOW"
                },
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        ReferenceId = referenceId,
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = currency,
                            Value = amount.ToString("F2"),
                            AmountBreakdown = new AmountBreakdown
                            {
                                ItemTotal = new Money
                                {
                                    CurrencyCode = currency,
                                    Value = amount.ToString("F2", CultureInfo.InvariantCulture)
                                }
                            }
                        },
                        Items = new List<Item>
                        {
                            new Item
                            {
                                Name = "Pedido EmprendeMarket",
                                UnitAmount = new Money
                                {
                                    CurrencyCode = currency,
                                    Value = amount.ToString("F2", CultureInfo.InvariantCulture)
                                },
                                Quantity = "1",
                                Category = "DIGITAL_GOODS"
                            }
                        }
                    }
                }
            };
        }

        private static bool IsTransientNetworkError(Exception exception)
        {
            return exception switch
            {
                HttpRequestException => true,
                SocketException => true,
                TimeoutException => true,
                TaskCanceledException => true,
                AggregateException aggregateException => aggregateException.InnerExceptions.Any(IsTransientNetworkError),
                _ when exception.InnerException != null => IsTransientNetworkError(exception.InnerException),
                _ => false
            };
        }
    }
}
