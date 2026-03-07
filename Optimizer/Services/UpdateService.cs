using System;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        private const string TempZipName = "Optimizer_update.zip";
        private const string TempExtractDir = "Optimizer_update_tmp";
        private const string UpdateBatName = "update.bat";

        // ── Événements ───────────────────────────────────────────────────────
        /// <summary>Progression du téléchargement (0.0 → 1.0).</summary>
        public event Action<double>? ProgressChanged;

        /// <summary>Appelé quand aucune mise à jour n'est disponible.</summary>
        public event Action? AlreadyUpToDate;

        /// <summary>Appelé quand une erreur survient (message en paramètre).</summary>
        public event Action<string>? UpdateError;

        /// <summary>Appelé juste avant la fermeture pour mise à jour.</summary>
        public event Action? UpdateReady;

        // ── Point d'entrée principal ──────────────────────────────────────────
        public async Task CheckAndUpdateAsync()
        {
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
                ExtractUpdate();
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

            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string remoteVersion = tagName.TrimStart('v', 'V');

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? name = asset.GetProperty("name").GetString();
                    if (name is not null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
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
            var localFull = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            var local = new Version(localFull.Major, localFull.Minor, localFull.Build);

            if (!Version.TryParse(remoteVersion, out var remote))
                return false;

            return remote > local;
        }

        // ── Téléchargement ────────────────────────────────────────────────────
        private async Task DownloadUpdateAsync(string downloadUrl)
        {
            string tempPath = GetAppPath(TempZipName);

            using var client = CreateHttpClient();
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using var srcStream = await response.Content.ReadAsStreamAsync();
            using var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write);

            var buffer = new byte[81920];
            long bytesRead = 0;
            int read;

            while ((read = await srcStream.ReadAsync(buffer)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (totalBytes.HasValue && totalBytes > 0)
                    ProgressChanged?.Invoke((double)bytesRead / totalBytes.Value);
            }

            ProgressChanged?.Invoke(1.0);
        }

        // ── Extraction du zip ─────────────────────────────────────────────────
        private static void ExtractUpdate()
        {
            string zipPath = GetAppPath(TempZipName);
            string extractDir = GetAppPath(TempExtractDir);

            // Nettoyer le dossier temporaire s'il existe déjà
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);

            ZipFile.ExtractToDirectory(zipPath, extractDir);

            // Supprimer le zip maintenant qu'il est extrait
            File.Delete(zipPath);
        }

        // ── Génération du script de remplacement ─────────────────────────────
        private static void GenerateUpdateBat()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = GetAppPath(ExeName);
            string extractDir = GetAppPath(TempExtractDir);
            string batPath = GetAppPath(UpdateBatName);

            // Le .bat :
            // 1. Attend la fermeture d'Optimizer
            // 2. Copie tous les fichiers du dossier extrait vers le dossier de l'app
            // 3. Supprime le dossier temporaire
            // 4. Relance Optimizer
            // 5. Se supprime lui-même
            string bat = $"""
                @echo off
                timeout /t 2 /nobreak >nul
                xcopy /E /Y /I "{extractDir}\*" "{appDir}"
                rmdir /S /Q "{extractDir}"
                start "" "{exePath}"
                del "%~f0"
                """;

            File.WriteAllText(batPath, bat);

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
            TryDelete(GetAppPath(TempZipName));
            TryDelete(GetAppPath(UpdateBatName));
            TryDeleteDir(GetAppPath(TempExtractDir));
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* silencieux */ }
        }

        private static void TryDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
            catch { /* silencieux */ }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string GetAppPath(string fileName)
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
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