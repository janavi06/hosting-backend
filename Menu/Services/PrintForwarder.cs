// Services/PrintForwarder.cs
using System;
using System.Net.Http;
using System.Net.Http.Json; // Required for PostAsJsonAsync
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Restaurant_Menu.DTOs;
using Restaurant_Menu.Models;

namespace Restaurant_Menu.Services
{
    public class PrintForwarder : IPrintForwarder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PrintForwarder> _logger;

        public PrintForwarder(IHttpClientFactory httpClientFactory, ApplicationDbContext db, ILogger<PrintForwarder> logger)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _logger = logger;
        }

        public async Task<ForwardResult> ForwardPrintAsync(PrintRequestDto dto, int restaurantId)
        {
            try
            {
                var rest = await _db.Restaurants.SingleOrDefaultAsync(r => r.RestaurantID == restaurantId);
                if (rest == null)
                    return new ForwardResult { Success = false, Message = "Restaurant not found" };

                dto.KotPrinterName ??= rest.KotPrinterName;
                dto.BillPrinterName ??= rest.BillPrinterName;

                var printServiceUrl = string.IsNullOrWhiteSpace(rest.LocalPrintServiceUrl)
                    ? "http://localhost:9000"
                    : rest.LocalPrintServiceUrl.TrimEnd('/');

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);

                var response = await client.PostAsJsonAsync($"{printServiceUrl}/api/print", dto);

                var message = response.IsSuccessStatusCode
                    ? "Sent"
                    : await response.Content.ReadAsStringAsync();

                _logger.LogInformation("ForwardPrint: restaurant={restaurantId} url={url} result={status}", restaurantId, printServiceUrl, response.StatusCode);

                return new ForwardResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding print job for restaurant {restaurantId}", restaurantId);
                return new ForwardResult { Success = false, Message = ex.Message };
            }
        }
    }
}