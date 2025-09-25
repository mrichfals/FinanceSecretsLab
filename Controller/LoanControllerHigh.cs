using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FinanceSecretsLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanHighController : ControllerBase
    {
        private readonly IConfiguration _config;

        public LoanHighController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("calculate")]
        public async Task<IActionResult> Calculate(decimal principal = 10000, double interest = 10, int months = 12)
        {
            var apiKey = _config["ExchangeApi:ApiKey"]; // ✅ diambil dari appsettings.json

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