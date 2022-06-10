using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker_translationhelp
{
    public class GoogleTranslate
    {
        private const string TranslateUrl = "https://translation.googleapis.com/language/translate/v2";

        private const string AuthKeyEnvName = "GOOGLE_AUTH_KEY";

        public static async Task<string?> Translate(
            string textLang,
            string text,
            string translateLang)
        {
            var authKey = Environment.GetEnvironmentVariable(AuthKeyEnvName);

            if (string.IsNullOrEmpty(authKey))
                throw new InvalidOperationException($"'{AuthKeyEnvName}' EnvVar is not set");

            var values = new Dictionary<string, string>
            {
                { "source", textLang},
                {"q", text},
                {"target", translateLang},

                {"key", authKey}
            };
            var content = new FormUrlEncodedContent(values);

            using var client = new HttpClient();
            using var responseTask = await client.PostAsync(TranslateUrl, content);
            responseTask.EnsureSuccessStatusCode();

            var responseBody = await responseTask.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<GoogleResponse>(responseBody);

            return responseData?.Translations?.FirstOrDefault()?.Text;
        }
    }

    class GoogleResponse
    {

        [JsonProperty("translations")]
        public List<GoogleTranslationData>? Translations { get; set; }
    }

    class GoogleTranslationData
    {
        [JsonProperty("detectedSourceLanguage")]
        public string? Language { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("translatedText")]
        public string? Text { get; set; }
    }
}
