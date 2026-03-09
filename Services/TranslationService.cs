using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoiceTyping.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private const string ChatApiUrl = "https://api.openai.com/v1/chat/completions";

        public TranslationService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(1)
            };
        }

        public void SetApiKey(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> TranslateAsync(string text)
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
                throw new InvalidOperationException("API key not set. Please configure your OpenAI API key.");

            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var requestBody = new
            {
                model = "gpt-5-nano",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a translator. Translate the following Vietnamese text to English. Output only the translation, nothing else. Do not add any explanations, notes, or quotation marks."
                    },
                    new
                    {
                        role = "user",
                        content = text
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(ChatApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JObject.Parse(responseContent);
                    var message = error["error"]?["message"]?.ToString() ?? "Unknown error";
                    throw new Exception($"Translation API error: {message}");
                }

                var result = JObject.Parse(responseContent);
                var translation = result["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();
                return translation ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Translation request timed out. Please try again.");
            }
        }
    }
}
