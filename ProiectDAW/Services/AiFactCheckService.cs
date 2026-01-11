using System.Text;
using Newtonsoft.Json;

namespace ProiectDAW.Services
{
    public interface IAiFactCheckService
    {
        Task<(int Score, string Label, string Justification)> GetContentTrustScoreAsync(string title, string content);
    }

    public class AiFactCheckService : IAiFactCheckService
    {
        private readonly string? _apiKey;
        private readonly HttpClient _httpClient;

        public AiFactCheckService(IConfiguration configuration)
        {
            _apiKey = Environment.GetEnvironmentVariable("VITE_OPENAI_API_KEY") ?? configuration["VITE_OPENAI_API_KEY"];
            _httpClient = new HttpClient();
        }

        public async Task<(int Score, string Label, string Justification)> GetContentTrustScoreAsync(string title, string content)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return (0, "Error", "OpenAI API Key not found.");
            }

            var prompt = $@"
You are an expert fact-checker. accurate and impartial.
Analyze the following news article for truthfulness and credibility.

Title: {title}
Content: {content}

Provide a JSON response with the following fields:
- score: A number between 0 and 100 representing the trust score (0 = Fake/False, 100 = True/Verified).
- label: A short label (e.g., 'Verified', 'Likely True', 'Unverified', 'Misleading', 'Fake News').
- justification: A concise explanation (max 2 sentences) of why you gave this score.

Target JSON format:
{{
  ""score"": 85,
  ""label"": ""Likely True"",
  ""justification"": ""The article cites reliable sources and aligns with known events.""
}}
";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that outputs JSON." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                dynamic? result = JsonConvert.DeserializeObject(responseString);
                string? contentResponse = result?.choices?[0]?.message?.content?.ToString();
                
                if (string.IsNullOrEmpty(contentResponse)) return (0, "Error", "Empty response from AI");

                // Clean contentResponse if it contains markdown code blocks
                contentResponse = contentResponse.Replace("```json", "").Replace("```", "").Trim();

                dynamic factCheck = JsonConvert.DeserializeObject(contentResponse);

                int score = (int)factCheck.score;
                string label = (string)factCheck.label;
                string justification = (string)factCheck.justification;

                return (score, label, justification);
            }
            catch (Exception ex)
            {
                // In production, log the error
                Console.WriteLine($"AI Fact Check Error: {ex.Message}");
                return (0, "Error", "Could not verify content due to technical issues.");
            }
        }
    }
}
