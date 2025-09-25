using Microsoft.AspNetCore.Mvc;

namespace FinanceSecretsLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeController : ControllerBase
    {
        private const string ApiKey = "FRONTEND-HARDCODED-123";

        [HttpGet("usd-to-idr")]
        public IActionResult GetRate([FromQuery] string key)
        {
            if (key != ApiKey)
            {
                return Unauthorized(new { error = "API Key invalid" });
            }

            // dummy kurs
            var rate = 15000m;

            return Ok(new
            {
                from = "USD",
                to = "IDR",
                rate = rate,
                updatedAt = DateTime.UtcNow
            });
        }
    }
}
