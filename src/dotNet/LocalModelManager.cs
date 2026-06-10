using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
    public class LocalModelManager : IDisposable
    {
        private Process _serverProcess;
        private string _modelsDir;
        private string _serverExe;
        private string _modelPath;
        private bool _ready = false;
        private string _status = "Initializing...";

        public string ModelsDirectory => _modelsDir;
        public string ModelPath => _modelPath;
        public bool IsReady => _ready;
        public string Status => _status;

        public event Action StatusChanged;

        private const string LLAMA_SERVER_URL =
            "https://github.com/ggml-org/llama.cpp/releases/download/b4897/llama-b4897-bin-win-msvc-x64.zip";
        private const string MODEL_URL =
            "https://huggingface.co/bartowski/Phi-4-mini-instruct-GGUF/resolve/main/Phi-4-mini-instruct-Q4_K_M.gguf";
        private const string MODEL_FILENAME = "Phi-4-mini-instruct-Q4_K_M.gguf";
        private const string SERVER_FILENAME = "llama-server.exe";

        public LocalModelManager()
        {
            _modelsDir = Path.Combine(Application.StartupPath, "models");
            _serverExe = Path.Combine(_modelsDir, SERVER_FILENAME);
            _modelPath = Path.Combine(_modelsDir, MODEL_FILENAME);
            Directory.CreateDirectory(_modelsDir);
        }

        public async Task EnsureModelReadyAsync()
        {
            bool serverExists = File.Exists(_serverExe);
            bool modelExists = File.Exists(_modelPath);

            if (!serverExists)
            {
                _status = "Downloading llama-server...";
                StatusChanged?.Invoke();
                await DownloadFileAsync(LLAMA_SERVER_URL, Path.Combine(_modelsDir, "llama.zip"));
                try
                {
                    ZipFile.ExtractToDirectory(Path.Combine(_modelsDir, "llama.zip"), _modelsDir);
                    var extracted = Directory.GetFiles(_modelsDir, "llama-server.exe", SearchOption.AllDirectories);
                    if (extracted.Length > 0 && extracted[0] != _serverExe)
                        File.Copy(extracted[0], _serverExe, true);
                }
                finally
                {
                    if (File.Exists(Path.Combine(_modelsDir, "llama.zip")))
                        File.Delete(Path.Combine(_modelsDir, "llama.zip"));
                }
                serverExists = File.Exists(_serverExe);
            }

            if (!modelExists)
            {
                _status = "Downloading Phi-4-mini model (~2.5 GB)...";
                StatusChanged?.Invoke();
                await DownloadFileAsync(MODEL_URL, _modelPath + ".tmp");
                if (File.Exists(_modelPath + ".tmp"))
                    File.Move(_modelPath + ".tmp", _modelPath);
                modelExists = File.Exists(_modelPath);
            }

            if (serverExists && modelExists)
            {
                _status = "Starting local inference server...";
                StatusChanged?.Invoke();
                StartServer();
            }
            else
            {
                _status = serverExists ? "Model not found" : "Server binary not found";
                StatusChanged?.Invoke();
            }
        }

        private void StartServer()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
                return;

            _serverProcess = new Process();
            _serverProcess.StartInfo.FileName = _serverExe;
            _serverProcess.StartInfo.Arguments =
                $"-m \"{_modelPath}\" --port 8080 --ctx-size 2048 -ngl 0 --threads 4 --no-mmap";
            _serverProcess.StartInfo.UseShellExecute = false;
            _serverProcess.StartInfo.RedirectStandardOutput = true;
            _serverProcess.StartInfo.RedirectStandardError = true;
            _serverProcess.StartInfo.CreateNoWindow = true;

            _serverProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains("starting") || e.Data.Contains("build info"))
                    {
                        // still starting
                    }
                    if (e.Data.Contains("waiting for") || e.Data.Contains("ready") ||
                        e.Data.Contains("all slots"))
                    {
                        _ready = true;
                        _status = "Server ready on http://127.0.0.1:8080";
                        StatusChanged?.Invoke();
                    }
                }
            };

            try
            {
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                Task.Run(async () =>
                {
                    await Task.Delay(8000);
                    if (!_ready)
                    {
                        _ready = true;
                        _status = "Server started (assuming ready)";
                        StatusChanged?.Invoke();
                    }
                });
            }
            catch (Exception ex)
            {
                _status = "Failed to start server: " + ex.Message;
                StatusChanged?.Invoke();
            }
        }

        private async Task DownloadFileAsync(string url, string destPath)
        {
            using (var wc = new WebClient())
            {
                var tcs = new TaskCompletionSource<bool>();
                wc.DownloadProgressChanged += (s, e) =>
                {
                    _status = $"Downloading {Path.GetFileName(destPath)}: {e.ProgressPercentage}% ({e.BytesReceived / 1048576}MB / {e.TotalBytesToReceive / 1048576}MB)";
                    StatusChanged?.Invoke();
                };
                wc.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                        tcs.TrySetException(e.Error);
                    else
                        tcs.TrySetResult(true);
                };
                await wc.DownloadFileTaskAsync(url, destPath);
            }
        }

        public void Stop()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try { _serverProcess.Kill(); } catch { }
                _serverProcess.Dispose();
                _serverProcess = null;
            }
            _ready = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
