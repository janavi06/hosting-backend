using System.Net.Http;

public class SelfPingService : BackgroundService
{
    private readonly HttpClient _httpClient;

    public SelfPingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = "https://api.scanui.site/api/health";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _httpClient.GetAsync(url, stoppingToken);
                Console.WriteLine($"Self-ping at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Self-ping failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
        }
    }
}