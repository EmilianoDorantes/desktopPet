using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
    public class TriggerEngine
    {
        private readonly OllamaService _ollama;
        private readonly PerformanceHelper _perf;
        private readonly Dictionary<string, DateTime> _lastTrigger = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, string> _contexts;
        private readonly Random _random = new Random();
        private readonly TimeSpan _globalCooldown = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _triggerCooldown = TimeSpan.FromMinutes(45);
        private System.Threading.Timer _timer;
        private DateTime _nextRandomTrigger = DateTime.MinValue;
        private DateTime _sessionStart = DateTime.Now;
        private string _previousWindow = "";
        private string _rapidSwitchApp = "";
        private int _rapidSwitchCount = 0;
        private DateTime _rapidSwitchStart = DateTime.MinValue;
        

        public event Action<string> OnMessageGenerated;

        public TriggerEngine(OllamaService ollama, PerformanceHelper perf)
        {
            _ollama = ollama;
            _perf = perf;

            _contexts = new Dictionary<string, string>
            {
                ["usuario_abandonó_pc"] = "el usuario lleva más de 2 horas sin tocar el teclado ni el mouse, la PC sigue encendida",
                ["usuario_fantasma"] = "el usuario lleva 30 minutos sin hacer nada, pero la PC está encendida",
                ["usuario_trasnochando"] = "son las 3am y el usuario sigue frente a la computadora",
                ["debería_dormir"] = "son las 11pm o más tarde y el usuario sigue trabajando",
                ["hora_comida_ignorada"] = "son las 2pm y probablemente el usuario no ha comido nada",
                ["lunes_por_la_mañana"] = "es lunes antes de las 10am, el inicio de otra semana",
                ["viernes_por_fin"] = "es viernes después de las 5pm, fin de la semana laboral",
                ["san_valentín"] = "es 14 de febrero y el usuario está frente a su computadora",
                ["fin_de_mes"] = "es fin de mes, los últimos días, cuando se acumula todo lo que no se hizo",
                ["sufriendo_en_excel"] = "el usuario lleva tiempo trabajando en Excel",
                ["trabajando_muy_duro"] = "el usuario está viendo Netflix o algún servicio de streaming en horario laboral",
                ["productividad_máxima"] = "el usuario está viendo YouTube en vez de trabajar",
                ["perdido_en_stack"] = "el usuario está en Stack Overflow o GitHub buscando soluciones a algún error",
                ["buscando_trabajo"] = "el usuario está navegando LinkedIn",
                ["reunión_interminable"] = "el usuario está en una videollamada de Teams, Zoom o Meet",
                ["algo_explotó"] = "el usuario abrió el Administrador de Tareas, algo en la PC está fallando",
                ["bateria_agonizando"] = "la batería de la laptop está al 10% o menos y sin cargador conectado",
                ["cargador_olvidado"] = "la batería está al 100% pero sigue conectada al cargador",
                ["pomodoro_olvidado"] = "el usuario lleva casi una hora seguida sin descansar",
                ["sesión_interminable"] = "el usuario lleva más de 2 horas continuas frente a la computadora sin pausa",
                ["indecisión_crónica"] = "el usuario abrió y cerró la misma aplicación varias veces en pocos minutos",
                ["pc_en_llamas"] = "el CPU está al 90% o más, la computadora está trabajando al máximo",
                ["ram_sufriendo"] = "la memoria RAM disponible es menos de 512MB, el sistema está al límite",
                ["momento_existencial"] = "no pasa nada en especial, el gato simplemente decidió aparecer sin razón",
            };
        }

        public void Start()
        {
            _sessionStart = DateTime.Now;
            _timer = new System.Threading.Timer(EvaluateTriggers, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private async void EvaluateTriggers(object state)
        {
            var trigger = DetectActiveTrigger();
            if (trigger == null) return;
            if (!CanFire(trigger)) return;

            var context = _contexts.ContainsKey(trigger) ? _contexts[trigger] : trigger;
            string message = await _ollama.GenerateAsync(context);

            RegisterFire(trigger);
            if (!string.IsNullOrEmpty(message))
            {
                OnMessageGenerated?.Invoke(message);
            }
        }

        private string DetectActiveTrigger()
        {
            var idle = WindowsApiHelper.GetIdleTime();
            var window = WindowsApiHelper.GetActiveWindowTitle();
            int hour = DateTime.Now.Hour;
            var day = DateTime.Now.DayOfWeek;
            var now = DateTime.Now;
            var sessionDuration = DateTime.Now - _sessionStart;

            UpdateWindowTracking(window);

            if (idle.TotalHours >= 2)
                return "usuario_abandonó_pc";

            if (idle.TotalMinutes >= 30 && idle.TotalHours < 2)
                return "usuario_fantasma";

            if (hour >= 0 && hour < 5)
                return "usuario_trasnochando";

            if (hour >= 23)
                return "debería_dormir";

            if (hour == 13 || hour == 14)
                return "hora_comida_ignorada";

            if (day == DayOfWeek.Monday && hour < 10)
                return "lunes_por_la_mañana";

            if (day == DayOfWeek.Friday && hour >= 17)
                return "viernes_por_fin";

            if (now.Month == 2 && now.Day == 14)
                return "san_valentín";

            if (now.Day >= 28)
                return "fin_de_mes";

            if (_perf.IsAvailable)
            {
                if (_perf.GetCpuUsage() > 90f)
                    return "pc_en_llamas";
                if (_perf.GetAvailableRamMB() < 512f)
                    return "ram_sufriendo";
            }

            try
            {
                var battery = SystemInformation.PowerStatus;
                if (battery.PowerLineStatus == PowerLineStatus.Offline && battery.BatteryLifePercent <= 0.10f)
                    return "bateria_agonizando";
                if (battery.PowerLineStatus == PowerLineStatus.Online && battery.BatteryLifePercent >= 0.99f)
                    return "cargador_olvidado";
            }
            catch { }

            if (!string.IsNullOrEmpty(window))
            {
                if (window.Contains("excel") || window.Contains(".xlsx"))
                    return "sufriendo_en_excel";
                if (window.Contains("netflix") || window.Contains("disney") ||
                    window.Contains("hbo") || window.Contains("prime video"))
                    return "trabajando_muy_duro";
                if (window.Contains("youtube"))
                    return "productividad_máxima";
                if (window.Contains("stackoverflow") || window.Contains("github"))
                    return "perdido_en_stack";
                if (window.Contains("linkedin"))
                    return "buscando_trabajo";
                if (window.Contains("teams") || window.Contains("zoom") ||
                    window.Contains("meet") || window.Contains("webex"))
                    return "reunión_interminable";
                if (window.Contains("task manager") || window.Contains("administrador de tareas"))
                    return "algo_explotó";
            }

            if (sessionDuration.TotalMinutes >= 52 && sessionDuration.TotalMinutes < 60)
                return "pomodoro_olvidado";

            if (sessionDuration.TotalHours >= 2)
                return "sesión_interminable";

            if (HasIndecision())
                return "indecisión_crónica";

            if (ShouldFireRandom())
                return "momento_existencial";

            return null;
        }

        private void UpdateWindowTracking(string currentWindow)
        {
            if (currentWindow == _previousWindow) return;

            if (!string.IsNullOrEmpty(currentWindow) && currentWindow == _rapidSwitchApp)
            {
                _rapidSwitchCount++;
            }
            else
            {
                _rapidSwitchApp = currentWindow;
                _rapidSwitchCount = 1;
                _rapidSwitchStart = DateTime.Now;
            }

            _previousWindow = currentWindow;
        }

        private bool HasIndecision()
        {
            return _rapidSwitchCount >= 4 &&
                   DateTime.Now - _rapidSwitchStart < TimeSpan.FromMinutes(10);
        }

        private bool ShouldFireRandom()
        {
            if (DateTime.Now < _nextRandomTrigger) return false;
            int minutes = _random.Next(45, 91);
            _nextRandomTrigger = DateTime.Now.AddMinutes(minutes);
            return true;
        }

        private bool CanFire(string trigger)
        {
            var now = DateTime.Now;

            if (_lastTrigger.ContainsKey("__global__") &&
                now - _lastTrigger["__global__"] < _globalCooldown)
                return false;

            if (_lastTrigger.ContainsKey(trigger) &&
                now - _lastTrigger[trigger] < _triggerCooldown)
                return false;

            return true;
        }

        private void RegisterFire(string trigger)
        {
            _lastTrigger[trigger] = DateTime.Now;
            _lastTrigger["__global__"] = DateTime.Now;
        }
    }
}
