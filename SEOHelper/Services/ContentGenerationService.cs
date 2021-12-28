using Newtonsoft.Json;
using SEOHelper.Models;
using System.Text;

namespace SEOHelper.Service
{
    // When You Find a better api or a better Way To Generate Content Replace The Code Down Here :
    public class ContentGenerationService : IContentGenerationService
    {
        private static readonly string API_KEY = "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";
        private static readonly string API_Website = "https://api.openai.com/v1/engines/davinci/completions";
        public async Task<string[]> GenerateContent(string[] Keywords)
        {
            var prompt = "Get Generate A Description for a website Using the Keywords.\nKeywords:\n" + string.Join(',', Keywords) + "\n Description: ";
            var results = await CallAPIAsync(prompt);
            if (results.choices != null)
                return results.choices.Select(r => r.text).ToArray();
            else
                return new string[0];
        }

        public async Task<string[]> GenerateMeta(string[] Keywords, string? content)
        {
            string prompt;
            if (string.IsNullOrEmpty(content))
                prompt = "Get Generate A Meta Description for a website Using the Keywords.\nKeywords:\n" + string.Join(',', Keywords) + "\nMeta Description: ";
            else
                prompt = $"{content} tl;dr :";
            var results = await CallAPIAsync(prompt);
            if (results.choices != null)
                return results.choices.Select(r => r.text).ToArray();
            else
                return new string[0];
        }

        static async Task<APIResult> CallAPIAsync(string prompt)
        {
            var reqjson = new OpenAIObject() { prompt = prompt };
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + API_KEY);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(API_Website),
                    Content = new StringContent(JsonConvert.SerializeObject(reqjson), Encoding.UTF8, "application/json")
                };
                var response = client.SendAsync(request).ConfigureAwait(false);

                var result = response.GetAwaiter().GetResult().IsSuccessStatusCode ? await response.GetAwaiter().GetResult().Content.ReadFromJsonAsync<APIResult>() : new APIResult();
                return result;
            }
        }
    }
}
