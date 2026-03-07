using System;
using System.IO;
using System.Text.Json;
using Optimizer.Models;
using Optimizer.Helpers;

namespace Optimizer.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private readonly string _backupFilePath;
        private readonly string _tempFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public SettingsService(string settingsFilePath)
        {
            _settingsFilePath = settingsFilePath;
            _backupFilePath = settingsFilePath + ".bak";
            _tempFilePath = settingsFilePath + ".tmp";
            _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        }

        /// <summary>
        /// Sauvegarde les paramètres via écriture atomique (.tmp → rename).
        /// Une copie .bak est créée après chaque sauvegarde réussie.
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonOptions);

                // Écriture dans le fichier temporaire
                File.WriteAllText(_tempFilePath, json);

                // Remplacement atomique
                File.Move(_tempFilePath, _settingsFilePath, overwrite: true);

                // Sauvegarde du backup après écriture réussie
                File.Copy(_settingsFilePath, _backupFilePath, overwrite: true);

                Logger.Log("Réglages sauvegardés avec succès !");
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde des réglages : {ex.Message}");

                try
                {
                    if (File.Exists(_tempFilePath))
                        File.Delete(_tempFilePath);
                }
                catch
                {
                    // Silencieux — le .tmp sera écrasé à la prochaine sauvegarde
                }
            }
        }

        /// <summary>
        /// Charge les paramètres depuis settings.json.
        /// En cas de fichier absent ou corrompu, tente de restaurer depuis settings.json.bak.
        /// Retourne les valeurs par défaut si aucune source valide n'existe.
        /// </summary>
        public AppSettings LoadSettings()
        {
            // Tentative 1 : fichier principal
            var settings = TryLoadFrom(_settingsFilePath, "principal");
            if (settings != null)
                return settings;

            // Tentative 2 : backup
            settings = TryLoadFrom(_backupFilePath, "backup");
            if (settings != null)
            {
                Logger.Log("Restauration depuis le backup réussie. Le fichier principal sera régénéré à la prochaine sauvegarde.");
                return settings;
            }

            // Fallback : valeurs par défaut
            Logger.Log("Aucun fichier de réglages valide trouvé. Utilisation des valeurs par défaut.");
            return new AppSettings();
        }

        /// <summary>
        /// Tente de désérialiser un fichier de paramètres.
        /// Retourne null si le fichier est absent, vide ou corrompu.
        /// </summary>
        private AppSettings? TryLoadFrom(string filePath, string label)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

                if (settings != null)
                {
                    Logger.Log($"Réglages chargés avec succès depuis le fichier {label} !");
                    return settings;
                }

                Logger.Log($"Fichier de réglages {label} invalide (désérialisation nulle).");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Fichier de réglages {label} corrompu : {ex.Message}");
                return null;
            }
        }
    }
}