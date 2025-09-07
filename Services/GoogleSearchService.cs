using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThesisNest.Services
{
    public class GoogleSearchService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _cx;

        public GoogleSearchService(HttpClient http, string apiKey, string cx)
        {
            _http = http;
            _apiKey = apiKey ?? "";
            _cx = cx ?? "";
        }

        public async Task<List<(string title, string snippet, string link)>> SearchAsync(string query, int num = 3)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_cx))
                    return new List<(string, string, string)>();

                var url = $"https://www.googleapis.com/customsearch/v1?key={_apiKey}&cx={_cx}&q={Uri.EscapeDataString(query)}&num={num}";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var list = new List<(string, string, string)>();
                if (doc.RootElement.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var title = item.GetProperty("title").GetString() ?? "";
                        var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                        var link = item.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
                        list.Add((title, snippet, link));
                    }
                }
                return list;
            }
            catch
            {
                return new List<(string, string, string)>();
            }
        }
    }
}
