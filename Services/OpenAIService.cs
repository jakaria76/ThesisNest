using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ThesisNest.Services
{
    public class OpenAIService
    {
        private readonly IHttpClientFactory _http;
        private const string Model = "gpt-3.5-turbo-0125"; // broadly available & stable

        public OpenAIService(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task<string> GetReplyAsync(string userMessage, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return string.Empty;

            var client = _http.CreateClient("OpenAI");

            var body = new
            {
                model = Model,
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant for a university thesis portal named ThesisNest. Keep answers concise." },
                    new { role = "user",   content = userMessage }
                },
                max_tokens = 400,
                temperature = 0.6
            };

            using var res = await client.PostAsJsonAsync("v1/chat/completions", body, ct);
            var text = await res.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"[OpenAI] status={(int)res.StatusCode} {res.ReasonPhrase} model={Model}");
            if (!res.IsSuccessStatusCode)
            {
                // এখানে পুরো error body প্রিন্ট হবে → আসল কারণ
                Console.WriteLine(text);
                return string.Empty;
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                var reply = doc.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString();
                return reply?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[OpenAI PARSE ERROR] " + ex);
                Console.WriteLine(text);
                return string.Empty;
            }
        }
    }
}
