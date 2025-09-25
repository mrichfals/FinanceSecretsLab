using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinanceSecretsLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanEnvController : ControllerBase
    {
        [HttpGet("calculate")]
        public async Task<IActionResult> Calculate(decimal principal = 10000, double interest = 10, int months = 12)
        {
            // ✅ Secret diambil dari Environment Variable
            var apiKey = Environment.GetEnvironmentVariable("EXCHANGE_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API Key tidak ditemukan di environment variable" });
            }

            var monthlyRate = (interest / 100) / 12;
            var n = months;
            var monthlyPayment = (double)principal *
                (monthlyRate * Math.Pow(1 + monthlyRate, n)) /
                (Math.Pow(1 + monthlyRate, n) - 1);

            using var client = new HttpClient();
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD";

            try
            {
                var response = await client.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);
                var rate = doc.RootElement.GetProperty("conversion_rates").GetProperty("IDR").GetDecimal();

                return Ok(new
                {
                    PrincipalUSD = principal,
                    InterestRate = interest,
                    Months = months,
                    MonthlyPaymentUSD = Math.Round(monthlyPayment, 2),
                    MonthlyPaymentIDR = Math.Round(monthlyPayment * (double)rate, 2),
                    ExchangeRate = rate
                });
            }
            catch
            {
                var rate = 15000m; // fallback

                return Ok(new
                {
                    PrincipalUSD = principal,
                    InterestRate = interest,
                    Months = months,
                    MonthlyPaymentUSD = Math.Round(monthlyPayment, 2),
                    MonthlyPaymentIDR = Math.Round(monthlyPayment * (double)rate, 2),
                    ExchangeRate = rate
                });
            }
        }
    }
}