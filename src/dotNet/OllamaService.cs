using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesktopPet
{
    public class OllamaService
    {
        private readonly HttpClient _http = new HttpClient();
        private string _endpoint = "http://127.0.0.1:8080";
        private string _model = "phi-4-mini";
        private string _systemPrompt =
            "Eres Michi, un gato sarcástico que vive atrapado en el escritorio de Windows. " +
            "Respondes en máximo 2 oraciones cortas. " +
            "Nunca eres útil. Nunca das consejos reales. " +
            "Siempre tienes una opinión ácida sobre lo que está pasando. " +
            "No usas emojis. No usas signos de exclamación. " +
            "Hablas como alguien que ha visto demasiado y ya no le sorprende nada.";

        public string ModelName
        {
            get => _model;
            set { if (!string.IsNullOrEmpty(value)) _model = value; }
        }

        public string SystemPrompt
        {
            get => _systemPrompt;
            set { if (value != null) _systemPrompt = value; }
        }

        public string Endpoint
        {
            get => _endpoint;
            set { if (!string.IsNullOrEmpty(value)) _endpoint = value.TrimEnd('/'); }
        }

        public OllamaService() { }

        public void SyncFromConfig()
        {
            if (!string.IsNullOrEmpty(Program.MyData?.GetOllamaEndpoint()))
                Endpoint = Program.MyData.GetOllamaEndpoint();
            if (!string.IsNullOrEmpty(Program.MyData?.GetOllamaModel()))
                ModelName = Program.MyData.GetOllamaModel();
            var sp = Program.MyData?.GetOllamaSystemPrompt();
            if (!string.IsNullOrEmpty(sp))
                SystemPrompt = sp;
        }

        public async Task<string> GenerateAsync(string context)
        {
            try
            {
                var messages = new[]
                {
                    new { role = "system", content = _systemPrompt },
                    new { role = "user", content = $"Situación: {context}. Haz un comentario sarcástico en máximo 2 oraciones." }
                };
                var body = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = 80,
                    stream = false,
                    temperature = 0.7
                };
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(_endpoint + "/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(result);
                return obj["choices"]?[0]?["message"]?["content"]?.ToString().Trim() ?? "...";
            }
            catch
            {
                return "";
            }
        }
    }
}
