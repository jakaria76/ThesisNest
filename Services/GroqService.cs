using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThesisNest.Services
{
    public sealed class GroqResult
    {
        public bool Ok { get; init; }
        public HttpStatusCode Status { get; init; }
        public string Url { get; init; } = "";
        public string Reason { get; init; } = "";
        public string ErrorBody { get; init; } = "";
        public string Text { get; init; } = "";
    }

    public class GroqService
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public GroqService(IHttpClientFactory http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<GroqResult> AskAsync(string userMessage, CancellationToken ct = default)
        {
            var client = _http.CreateClient("Groq");

            // choose a Groq model you have access to
            // Common: "mixtral-8x7b-32768" or "llama3-8b-8192"
            var model = _cfg["Groq:ModelId"] ?? "llama-3.1-8b-instant";


            var url = new Uri(client.BaseAddress!, "chat/completions").ToString();

            var reqBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user",   content = userMessage }
                },
                max_tokens = 256,
                temperature = 0.6
            };

            HttpResponseMessage res;
            string bodyText;

            try
            {
                res = await client.PostAsJsonAsync("chat/completions", reqBody, ct);
                bodyText = await res.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                return new GroqResult
                {
                    Ok = false,
                    Status = 0,
                    Url = url,
                    Reason = ex.Message,
                    ErrorBody = ""
                };
            }

            if (!res.IsSuccessStatusCode)
            {
                return new GroqResult
                {
                    Ok = false,
                    Status = res.StatusCode,
                    Url = url,
                    Reason = res.ReasonPhrase ?? res.StatusCode.ToString(),
                    ErrorBody = bodyText
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(bodyText);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return new GroqResult
                {
                    Ok = true,
                    Status = res.StatusCode,
                    Url = url,
                    Text = content.Trim()
                };
            }
            catch
            {
                // fallback if shape differs
                return new GroqResult
                {
                    Ok = true,
                    Status = res.StatusCode,
                    Url = url,
                    Text = bodyText
                };
            }
        }
    }
}
