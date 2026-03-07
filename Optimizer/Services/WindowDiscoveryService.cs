using System;
using System.Collections.Generic;
using System.Linq;
using Optimizer.Helpers;
using Optimizer.Models;

namespace Optimizer.Services
{
    /// <summary>
    /// Service de découverte et filtrage des fenêtres de jeu.
    /// </summary>
    public class WindowDiscoveryService
    {
        private const string WindowFilterKeyword = "- Release";

        /// <summary>
        /// Récupère toutes les fenêtres Dofus actuellement ouvertes.
        /// </summary>
        public List<WindowInfo> GetDofusWindows()
        {
            var allWindows = WindowHelper.GetWindows();
            return allWindows
                .Where(w => w.Title.Contains(WindowFilterKeyword))
                .Select(w => new WindowInfo
                {
                    Handle = w.Handle,
                    WindowTitle = w.Title,
                    CharacterName = ExtractCharacterName(w.Title)
                })
                .ToList();
        }

        /// <summary>
        /// Extrait le nom du personnage depuis le titre de la fenêtre.
        /// Format attendu : "NomPersonnage - Account - Dofus X.X.X - Release"
        /// Source unique utilisée par CharactersViewModel et ScriptGenerator.
        /// </summary>
        public static string ExtractCharacterName(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return string.Empty;

            int firstSeparatorIndex = windowTitle.IndexOf(" - ", StringComparison.Ordinal);
            if (firstSeparatorIndex > 0)
                return windowTitle.Substring(0, firstSeparatorIndex).Trim();

            // Fallback : premier mot
            int spaceIndex = windowTitle.IndexOf(' ');
            return spaceIndex > 0 ? windowTitle.Substring(0, spaceIndex).Trim() : windowTitle.Trim();
        }
    }

    /// <summary>
    /// Informations d'une fenêtre de jeu.
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
    }
}