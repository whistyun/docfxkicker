using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker_translationhelp
{
    public class DeepLTranslate
    {
        private const string TranslateFreeUrl = "https://api-free.deepl.com/v2/translate";
        private const string TranslateProUrl = "https://api.deepl.com/v2/translate";

        private const string PlanEnvName = "DEEPL_PLAN";
        private const string AuthKeyEnvName = "DEEPL_AUTH_KEY";

        public static async Task<string?> Translate(
            string textLang,
            string text,
            string translateLang)
        {
            var plan = Environment.GetEnvironmentVariable(PlanEnvName);
            var authKey = Environment.GetEnvironmentVariable(AuthKeyEnvName);

            if (string.IsNullOrEmpty(plan))
                throw new InvalidOperationException($"'{PlanEnvName}' EnvVar is not set");

            if (string.IsNullOrEmpty(authKey))
                throw new InvalidOperationException($"'{AuthKeyEnvName}' EnvVar is not set");


            var url = plan.ToLower() switch
            {
                "free" => TranslateFreeUrl,
                "pro" => TranslateProUrl,
                _ => throw new InvalidOperationException($"'{PlanEnvName}' EnvVar should be 'free' or 'pro'")
            };

            var values = new Dictionary<string, string>
            {
                {"source_lang", textLang},
                {"text", text},
                {"target_lang", translateLang},

                {"auth_key",  authKey}
           };
            var content = new UTF8FormUrlEncodedContent(values);

            using var client = new HttpClient();
            using var responseTask = await client.PostAsync(url, content);
            responseTask.EnsureSuccessStatusCode();

            var responseBody = await responseTask.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<DeepLResponse>(responseBody);

            return responseData?.Translations?.FirstOrDefault()?.Text;
        }

        class DeepLResponse
        {

            [JsonProperty("translations")]
            public List<DeepLTranslationData>? Translations { get; set; }
        }

        class DeepLTranslationData
        {
            [JsonProperty("detected_source_language")]
            public string? Language { get; set; }

            [JsonProperty("text")]
            public string? Text { get; set; }
        }
    }
}
