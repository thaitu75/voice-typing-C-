using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VoiceTyping.Services
{
    public class WhisperApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.openai.com/v1/audio/transcriptions";

        public WhisperApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
        }

        public void SetApiKey(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> TranscribeAsync(byte[] audioData, string language = "")
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
                throw new InvalidOperationException("API key not set. Please configure your OpenAI API key.");

            using var content = new MultipartFormDataContent();
            
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "file", "audio.wav");
            content.Add(new StringContent("whisper-1"), "model");
            
            if (!string.IsNullOrEmpty(language))
            {
                content.Add(new StringContent(language), "language");
            }

            try
            {
                var response = await _httpClient.PostAsync(ApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JObject.Parse(responseContent);
                    var message = error["error"]?["message"]?.ToString() ?? "Unknown error";
                    throw new Exception($"Whisper API error: {message}");
                }

                var result = JObject.Parse(responseContent);
                return result["text"]?.ToString() ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timed out. Please try again.");
            }
        }
    }
}
