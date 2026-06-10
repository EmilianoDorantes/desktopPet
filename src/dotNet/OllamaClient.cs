using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesktopPet
{
    public static class OllamaClient
    {
        private static readonly HttpClient client = new HttpClient();
        private static string endpoint = "http://127.0.0.1:8080";
        private static string model = "phi-4-mini";
        private static string systemPrompt = "Eres una mascota virtual adorable y graciosa. Tus respuestas deben ser muy cortas, maximo 2 oraciones. Respondes en espanol.";
        private static bool enabled = true;
        private static bool useOpenAiFormat = true;

        public static void Configure(string ep, string m, string sp, bool en)
        {
            if (!string.IsNullOrEmpty(ep)) endpoint = ep.TrimEnd('/');
            if (!string.IsNullOrEmpty(m)) model = m;
            if (sp != null) systemPrompt = sp;
            enabled = en;
        }

        public static bool IsEnabled() => enabled;
        public static string GetModel() => model;
        public static string GetEndpoint() => endpoint;
        public static string GetSystemPrompt() => systemPrompt;
        public static bool UseOpenAiFormat
        {
            get => useOpenAiFormat;
            set => useOpenAiFormat = value;
        }

        public static async Task<string> GenerateAsync(string prompt)
        {
            if (!enabled) return "";
            try
            {
                if (useOpenAiFormat)
                    return await GenerateOpenAiAsync(prompt);
                else
                    return await GenerateOllamaAsync(prompt);
            }
            catch
            {
                return "";
            }
        }

        private static async Task<string> GenerateOpenAiAsync(string prompt)
        {
            var messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            };
            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 80,
                stream = false,
                temperature = 0.7
            };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint + "/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(result);
            return obj["choices"]?[0]?["message"]?["content"]?.ToString().Trim() ?? "...";
        }

        private static async Task<string> GenerateOllamaAsync(string prompt)
        {
            var payload = new
            {
                model = model,
                prompt = prompt,
                system = systemPrompt,
                stream = false,
                options = new { num_predict = 80 }
            };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint + "/api/generate", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(result);
            return obj["response"]?.ToString().Trim() ?? "...";
        }

        public static async Task<bool> CheckConnectionAsync()
        {
            try
            {
                if (useOpenAiFormat)
                {
                    var response = await client.GetAsync(endpoint + "/health");
                    return response.IsSuccessStatusCode;
                }
                else
                {
                    var response = await client.GetAsync(endpoint + "/api/tags");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<string>> ListModelsAsync()
        {
            var models = new List<string>();
            try
            {
                if (useOpenAiFormat)
                {
                    models.Add(model);
                    return models;
                }

                var response = await client.GetAsync(endpoint + "/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    var modelsArray = obj["models"] as JArray;
                    if (modelsArray != null)
                    {
                        foreach (var m in modelsArray)
                        {
                            var name = m["name"]?.ToString();
                            if (!string.IsNullOrEmpty(name))
                                models.Add(name);
                        }
                    }
                }
            }
            catch { }
            return models;
        }
    }
}
