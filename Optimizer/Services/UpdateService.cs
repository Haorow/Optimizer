using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Optimizer.Services
{
    public class UpdateService
    {
        // ── Configuration ────────────────────────────────────────────────────
        private const string GitHubApiUrl = "https://api.github.com/repos/Haorow/Optimizer/releases/latest";
        private const string ExeName = "Optimizer.exe";
        private const string TempExeName = "Optimizer_new.exe";
        private const string UpdateBatName = "update.bat";

        // ── Événements ───────────────────────────────────────────────────────
        /// <summary>Progression du téléchargement (0.0 → 1.0).</summary>
        public event Action<double>? ProgressChanged;

        /// <summary>Appelé quand aucune mise à jour n'est disponible.</summary>
        public event Action? AlreadyUpToDate;

        /// <summary>Appelé quand une erreur survient (message en paramètre).</summary>
        public event Action<string>? UpdateError;

        /// <summary>Appelé juste avant la fermeture pour mise à jour — permet à l'UI de se préparer.</summary>
        public event Action? UpdateReady;

        // ── Point d'entrée principal ──────────────────────────────────────────
        /// <summary>
        /// Vérifie si une mise à jour est disponible.
        /// Si oui, télécharge, génère le .bat et déclenche la fermeture.
        /// </summary>
        public async Task CheckAndUpdateAsync()
        {
            // Nettoyage préventif des fichiers résiduels (crash précédent)
            CleanupResidualFiles();

            try
            {
                var (hasUpdate, downloadUrl) = await FetchLatestReleaseAsync();

                if (!hasUpdate || downloadUrl is null)
                {
                    AlreadyUpToDate?.Invoke();
                    return;
                }

                await DownloadUpdateAsync(downloadUrl);
                GenerateUpdateBat();
                UpdateReady?.Invoke();
            }
            catch (Exception ex)
            {
                UpdateError?.Invoke(ex.Message);
            }
        }

        // ── Vérification de version ───────────────────────────────────────────
        private async Task<(bool hasUpdate, string? downloadUrl)> FetchLatestReleaseAsync()
        {
            using var client = CreateHttpClient();
            var response = await client.GetAsync(GitHubApiUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            // Tag GitHub : "v1.2.0" → Version : "1.2.0"
            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string remoteVersion = tagName.TrimStart('v');

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? name = asset.GetProperty("name").GetString();
                    if (name is not null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            bool hasUpdate = IsNewerVersion(remoteVersion);
            return (hasUpdate && downloadUrl is not null, downloadUrl);
        }

        private static bool IsNewerVersion(string remoteVersion)
        {
            var local = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (!Version.TryParse(remoteVersion, out var remote))
                return false;

            return remote > local;
        }

        // ── Téléchargement ────────────────────────────────────────────────────
        private async Task DownloadUpdateAsync(string downloadUrl)
        {
            string tempPath = GetAppPath(TempExeName);

            using var client = CreateHttpClient();
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using var srcStream = await response.Content.ReadAsStreamAsync();
            using var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write);

            var buffer = new byte[81920]; // 80 Ko par chunk
            long bytesRead = 0;
            int read;

            while ((read = await srcStream.ReadAsync(buffer)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (totalBytes.HasValue && totalBytes > 0)
                    ProgressChanged?.Invoke((double)bytesRead / totalBytes.Value);
            }

            // S'assurer que la barre atteint 100%
            ProgressChanged?.Invoke(1.0);
        }

        // ── Génération du script de remplacement ─────────────────────────────
        private static void GenerateUpdateBat()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = GetAppPath(ExeName);
            string tmpPath = GetAppPath(TempExeName);
            string batPath = GetAppPath(UpdateBatName);

            // Le .bat attend la fermeture d'Optimizer, remplace l'exe, relance, puis se supprime
            string bat = $"""
                @echo off
                timeout /t 2 /nobreak >nul
                move /y "{tmpPath}" "{exePath}"
                start "" "{exePath}"
                del "%~f0"
                """;

            File.WriteAllText(batPath, bat);

            // Lancer le .bat en arrière-plan, fenêtre cachée
            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        // ── Nettoyage ─────────────────────────────────────────────────────────
        /// <summary>
        /// Supprime les fichiers résiduels d'une mise à jour interrompue.
        /// Appelé au démarrage de l'application.
        /// </summary>
        private static void CleanupResidualFiles()
        {
            TryDelete(GetAppPath(TempExeName));
            TryDelete(GetAppPath(UpdateBatName));
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* silencieux */ }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string GetAppPath(string fileName)
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            // GitHub exige un User-Agent
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Optimizer", GetLocalVersion()));
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static string GetLocalVersion()
            => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}