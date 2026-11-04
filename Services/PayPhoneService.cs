using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Proyecto.Services
{
    public interface IPayPhoneService
    {
        Task<PayPhoneCreateLinkResult> CreatePaymentLinkAsync(decimal amount, string reference, string clientTransactionId);
    }

    public class PayPhoneCreateLinkResult
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? PaymentId { get; set; }
        public string? ErrorMessage { get; set; }
        public JsonElement? RawResponse { get; set; }
    }

    public class PayPhoneService : IPayPhoneService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPhoneService> _logger;

        public PayPhoneService(HttpClient httpClient, IConfiguration configuration, ILogger<PayPhoneService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var token = _configuration["PayPhone:Token"] ?? _configuration["PAYPHONE_TOKEN"]
                ?? throw new InvalidOperationException("PayPhone Token not configured");
            var baseUrl = _configuration["PayPhone:BaseUrl"] ?? _configuration["PAYPHONE_BASE_URL"] ?? "https://pay.payphonetodoesposible.com";

            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<PayPhoneCreateLinkResult> CreatePaymentLinkAsync(decimal amount, string reference, string clientTransactionId)
        {
            var amountInCents = (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
            var txnId = clientTransactionId.Length > 15 ? clientTransactionId[..15] : clientTransactionId;

            var request = new PayPhoneLinkRequest
            {
                Amount = amountInCents,
                AmountWithoutTax = amountInCents,
                AmountWithTax = 0,
                Tax = 0,
                Service = 0,
                Tip = 0,
                Currency = "USD",
                Reference = reference,
                ClientTransactionId = txnId,
                AdditionalData = reference,
                OneTime = true,
                ExpireIn = 0,
                IsAmountEditable = false
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://pay.payphonetodoesposible.com/api/Links");

            var token = _configuration["PayPhone:Token"] ?? _configuration["PAYPHONE_TOKEN"];
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = JsonContent.Create(request);

            _logger.LogInformation("PayPhone request: POST /api/Links Body: {Body}", JsonSerializer.Serialize(request));

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayPhone response: Status={StatusCode} Body={Body}", (int)response.StatusCode, content);

                if (response.IsSuccessStatusCode)
                {
                    return new PayPhoneCreateLinkResult
                    {
                        Success = true,
                        PaymentUrl = content.Trim('"'),
                        RawResponse = JsonSerializer.Deserialize<JsonElement>($"\"{content.Trim('"')}\"")
                    };
                }

                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(content);
                    var errorMessage = errorJson.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString()
                        : $"HTTP {(int)response.StatusCode}: {content}";

                    _logger.LogError("PayPhone error: {Error}", errorMessage);

                    return new PayPhoneCreateLinkResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage,
                        RawResponse = errorJson
                    };
                }
                catch
                {
                    _logger.LogError("PayPhone error (non-JSON): HTTP {StatusCode}: {Body}", (int)response.StatusCode, content);
                    return new PayPhoneCreateLinkResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {(int)response.StatusCode}: {content}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayPhone exception");
                return new PayPhoneCreateLinkResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class PayPhoneLinkRequest
    {
        public int Amount { get; set; }
        public int AmountWithoutTax { get; set; }
        public int AmountWithTax { get; set; }
        public int Tax { get; set; }
        public int Service { get; set; }
        public int Tip { get; set; }
        public string Currency { get; set; } = "USD";
        public string Reference { get; set; } = string.Empty;
        public string ClientTransactionId { get; set; } = string.Empty;
        public string AdditionalData { get; set; } = string.Empty;
        public bool OneTime { get; set; } = true;
        public int ExpireIn { get; set; } = 0;
        public bool IsAmountEditable { get; set; } = false;
    }
}
