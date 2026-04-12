using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Windows;

// ✅ Fix CS0234: شيل using MyWpfApp.Models لأن AppVersion دلوقتي في MyWpfApp
namespace MyWpfApp.Services
{
    public class UpdateService
    {
        // ✏️ غيّر الـ URL ده لملف version.json في الـ repo بتاعك
        private const string VERSION_JSON_URL =
            "https://github.com/Juba-Juba/MyWpfApp/blob/main/version.json";

        private readonly HttpClient _httpClient;
        private readonly Timer      _checkTimer;
        private bool                _isChecking = false;

        public event Action<AppVersion>? UpdateAvailable;
        public event Action?             NoUpdateFound;
        public event Action<string>?     CheckFailed;
        public event Action<int>?        DownloadProgress;

        public static string CurrentVersion =>
            Assembly.GetExecutingAssembly()
                    .GetName().Version?
                    .ToString(3) ?? "1.0.0";

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyWpfApp-Updater");

            _checkTimer = new Timer(
                async _ => await CheckForUpdateAsync(),
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(30)
            );

            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        private async void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                await Task.Delay(1000);
                await CheckForUpdateAsync();
            }
        }

        public async Task CheckForUpdateAsync()
        {
            if (_isChecking) return;
            _isChecking = true;

            try
            {
                var response = await _httpClient.GetStringAsync(VERSION_JSON_URL);
                var latestVersion = JsonSerializer.Deserialize<GitHubVersionInfo>(response);
                if (latestVersion == null) return;

                if (IsNewerVersion(latestVersion.Version, CurrentVersion))
                {
                    var update = new AppVersion
                    {
                        Version      = latestVersion.Version,
                        DownloadUrl  = latestVersion.DownloadUrl,
                        ReleaseNotes = latestVersion.ReleaseNotes,
                        ReleasedAt   = latestVersion.ReleasedAt
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                        UpdateAvailable?.Invoke(update));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        NoUpdateFound?.Invoke());
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    CheckFailed?.Invoke(ex.Message));
            }
            finally
            {
                _isChecking = false;
            }
        }

        public async Task DownloadAndInstallAsync(AppVersion update)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "MyWpfApp_Update.exe");

            using var response = await _httpClient.GetAsync(
                update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            long downloadedBytes = 0;

            using var stream     = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    int percent = (int)(downloadedBytes * 100 / totalBytes);
                    Application.Current.Dispatcher.Invoke(() =>
                        DownloadProgress?.Invoke(percent));
                }
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = tempPath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            if (!Version.TryParse(latest,  out var vLatest))  return false;
            if (!Version.TryParse(current, out var vCurrent)) return false;
            return vLatest > vCurrent;
        }

        public void Dispose()
        {
            _checkTimer?.Dispose();
            _httpClient?.Dispose();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }
    }

    internal class GitHubVersionInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;

        [JsonPropertyName("releasedAt")]
        public DateTime ReleasedAt { get; set; }
    }
}
