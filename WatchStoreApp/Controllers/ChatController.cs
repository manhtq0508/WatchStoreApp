using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;

namespace WatchStoreApp.Controllers
{
    public class ChatController(MyAppContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration) : Controller
    {
        private readonly string _geminiApiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException($"GeminiApiKey is not configured");

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var message = request.Message?.ToLower() ?? "";
            string reply = "";
            Product? matchedProduct = null;

            // Keywords tìm kiếm sản phẩm
            var searchKeywords = new[] { "mua", "xem", "tìm", "giá", "giá tiền", "sản phẩm", "bao nhiêu" };
            bool isProductQuery = searchKeywords.Any(k => message.Contains(k));

            if (isProductQuery)
            {
                // Tìm sản phẩm trong database, include Brand để tránh lazy loading null
                var products = await context.Products
                                             .Include(p => p.Brand)
                                             .ToListAsync();

                // Tìm sản phẩm có tên/brand khớp tin nhắn
                matchedProduct = products.FirstOrDefault(p =>
                    (p.Name != null && message.Contains(p.Name.ToLower())) ||
                    (p.Brand != null && p.Brand.BrandName != null && message.Contains(p.Brand.BrandName.ToLower()))
                );

                if (matchedProduct == null)
                {
                    var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        if (word.Length > 3)
                        {
                            matchedProduct = products.FirstOrDefault(p =>
                                (p.Name != null && p.Name.ToLower().Contains(word)) ||
                                (p.Brand != null && p.Brand.BrandName != null && p.Brand.BrandName.ToLower().Contains(word))
                            );
                            if (matchedProduct != null) break;
                        }
                    }
                }

                if (matchedProduct != null)
                {
                    reply = $"Tôi tìm thấy sản phẩm phù hợp với yêu cầu của bạn! Bạn có thể xem chi tiết bên dưới. 👇";

                    // Trả về product + reply
                    return Json(new
                    {
                        reply = reply,
                        product = new
                        {
                            productId = matchedProduct.ProductId,
                            name = matchedProduct.Name,
                            sellingPrice = matchedProduct.SellingPrice,
                            imageUrl = matchedProduct.ImageUrl1,
                            watchType = matchedProduct.WatchType
                        }
                    });
                }
                else
                {
                    // Nếu không tìm thấy sản phẩm, vẫn gửi AI
                    reply = await GetBotReplyFromApi(message);
                    return Json(new { reply, product = (object?)null });
                }
            }
            else
            {
                // Không phải query sản phẩm => gửi API
                reply = await GetBotReplyFromApi(message);
                return Json(new { reply, product = (object?)null });
            }
        }

        private async Task<string> GetBotReplyFromApi(string message)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest:generateContent?key={_geminiApiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = message } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 150
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    using var errorDoc = JsonDocument.Parse(responseContent);
                    var errorMessage = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                    return $"Xin lỗi, hệ thống đang bận. Vui lòng thử lại sau. ({errorMessage})";
                }

                using var doc = JsonDocument.Parse(responseContent);
                var botReply = doc.RootElement
                                  .GetProperty("candidates")[0]
                                  .GetProperty("content")
                                  .GetProperty("parts")[0]
                                  .GetProperty("text")
                                  .GetString();

                return botReply ?? "Xin lỗi, tôi không nhận được câu trả lời từ hệ thống AI.";
            }
            catch
            {
                return "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.";
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
