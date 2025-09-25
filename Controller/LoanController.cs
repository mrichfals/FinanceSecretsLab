using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace FinanceSecretsLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanController : ControllerBase
    {
        // ❌ Hardcoded API Key (Level Medium)
        private static string ApiKey = "2e0a372b7913d6233c85c95c";

        [HttpGet("calculate")]
        public async Task<IActionResult> Calculate(decimal principal = 10000, double interest = 10, int months = 12)
        {
            // bunga per bulan
            var monthlyRate = (interest / 100) / 12;
            var n = months;

            // rumus cicilan bulanan (amortization)
            var monthlyPayment = (double)principal *
                (monthlyRate * Math.Pow(1 + monthlyRate, n)) /
                (Math.Pow(1 + monthlyRate, n) - 1);

            // ambil kurs USD → IDR dari API eksternal
            using var client = new HttpClient();
            var url = $"https://v6.exchangerate-api.com/v6/{ApiKey}/latest/USD";

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
                // fallback kurs dummy supaya demo tetap jalan
                var rate = 15000m;

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