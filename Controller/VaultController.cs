using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinanceSecretsLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VaultController : ControllerBase
    {
        private readonly ILogger<VaultController> _logger;

        public VaultController(ILogger<VaultController> logger)
        {
            _logger = logger;
        }

        [HttpGet("loan")]
        public async Task<IActionResult> CalculateLoan(decimal principal = 10000, double interest = 10, int months = 12)
        {
            string? vaultAddr = Environment.GetEnvironmentVariable("VAULT_ADDR");
            string? token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
            string? secretPath = Environment.GetEnvironmentVariable("VAULT_SECRET_PATH"); // ✅ sesuai env kamu

            if (string.IsNullOrWhiteSpace(vaultAddr) ||
                string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(secretPath))
            {
                _logger.LogError("Environment variable tidak lengkap. VAULT_ADDR={VaultAddr}, VAULT_TOKEN={VaultToken}, VAULT_SECRET_PATH={SecretPath}", vaultAddr, token, secretPath);
                return StatusCode(500, "Vault configuration missing");
            }

            try
            {
                // 🔒 Ambil API_KEY dari Vault
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
                };

                using var clientVault = new HttpClient(handler) { BaseAddress = new Uri(vaultAddr) };
                clientVault.DefaultRequestHeaders.Add("X-Vault-Token", token);

                var vaultResp = await clientVault.GetAsync($"/v1/{secretPath}");
                var vaultJson = await vaultResp.Content.ReadAsStringAsync();

                if (!vaultResp.IsSuccessStatusCode)
                {
                    _logger.LogError("Vault error {Status}: {Body}", vaultResp.StatusCode, vaultJson);
                    return StatusCode((int)vaultResp.StatusCode, vaultJson);
                }

                _logger.LogInformation("Vault raw response: {Json}", vaultJson);

                using var doc = JsonDocument.Parse(vaultJson);
                string? apiKey = ExtractApiKey(doc);

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("API_KEY tidak ditemukan di Vault path {Path}", secretPath);
                    return StatusCode(404, "API_KEY tidak ditemukan di Vault");
                }

                // 💰 Hitung cicilan dalam USD
                var monthlyRate = (interest / 100) / 12;
                var n = months;
                var monthlyPaymentUSD = (double)principal *
                    (monthlyRate * Math.Pow(1 + monthlyRate, n)) /
                    (Math.Pow(1 + monthlyRate, n) - 1);

                // 🌍 Panggil ExchangeRate API pakai API_KEY dari Vault
                using var client = new HttpClient();
                var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD";
                var response = await client.GetStringAsync(url);

                using var exDoc = JsonDocument.Parse(response);
                var rate = exDoc.RootElement.GetProperty("conversion_rates").GetProperty("IDR").GetDecimal();

                return Ok(new
                {
                    PrincipalUSD = principal,
                    InterestRate = interest,
                    Months = months,
                    MonthlyPaymentUSD = Math.Round(monthlyPaymentUSD, 2),
                    MonthlyPaymentIDR = Math.Round(monthlyPaymentUSD * (double)rate, 2),
                    ExchangeRate = rate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saat ambil data dari Vault / ExchangeRate");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        private string? ExtractApiKey(JsonDocument doc)
        {
            try
            {
                // KV v2: data.data.API_KEY
                if (doc.RootElement.TryGetProperty("data", out var outer) &&
                    outer.TryGetProperty("data", out var inner) &&
                    inner.TryGetProperty("API_KEY", out var keyElem))
                {
                    return keyElem.GetString();
                }

                // KV v1: data.API_KEY
                if (doc.RootElement.TryGetProperty("data", out var v1Data) &&
                    v1Data.TryGetProperty("API_KEY", out var v1KeyElem))
                {
                    return v1KeyElem.GetString();
                }
            }
            catch (Exception ex)
            {
                // supaya ga throw
                _logger.LogWarning(ex, "ExtractApiKey gagal parse JSON");
            }

            return null;
        }
    }
}