using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Restaurant_Menu.Interface;

namespace Restaurant_Menu.Services
{
    public class ChatbotService
    {
        private readonly IProductRepository _productRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAiApiKey;

        public ChatbotService(IProductRepository productRepo,
                              IHttpClientFactory httpFactory,
                              IConfiguration config)
        {
            _productRepository = productRepo;
            _httpClientFactory = httpFactory;
            _openAiApiKey = config["OpenAI:ApiKey"];
        }

        public async Task<string> AskAsync(string question, int restaurantId)
        {
            // 1️⃣ Get menu items for the restaurant
            var products = await _productRepository.GetAllProductsByRestaurantAsync(restaurantId, null, null);
            var menuText = products != null
                ? string.Join("\n", products.Select(p => $"{p.ProductName}: {p.ProductDescription} - ₹{p.Price}"))
                : "Menu not available.";

            // 2️⃣ Prepare OpenAI request
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _openAiApiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are a helpful restaurant waiter AI. Use only the menu and specials provided to answer questions."
                    },
                    new {
                        role = "user",
                        content = $"Menu:\n{menuText}\n\nCustomer question: {question}"
                    }
                },
                max_tokens = 200
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
                return "Sorry, the AI service is unavailable right now.";

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return answer ?? "Sorry, I could not generate a response.";
        }
    }
}
