using Optimizer.Services;

namespace Optimizer.Converters
{
    /// <summary>
    /// Helpers de conversion pour la génération de scripts AutoHotkey v2.
    /// La construction de AhkData est désormais directe dans MainViewModel.BuildAhkData().
    /// </summary>
    public static class AhkConverter
    {
        /// <summary>
        /// Formate un raccourci pour AHK v2 en déléguant à ShortcutMappingService.
        /// Fallback sur la valeur brute si le raccourci n'est pas reconnu.
        /// </summary>
        public static string FormatShortcut(string shortcut)
        {
            if (string.IsNullOrEmpty(shortcut))
                return shortcut;

            var ahkKey = ShortcutMappingService.GetAhkKey(shortcut);
            return !string.IsNullOrEmpty(ahkKey) ? ahkKey : shortcut;
        }

        /// <summary>
        /// Convertit le nom du layout en valeur interne AHK.
        /// </summary>
        public static string ConvertLayout(string? layout)
        {
            return layout switch
            {
                "Fenêtre unique" => "SingleWindow",
                "Fenêtres individuelles" => "IndividualWindows",
                _ => "SingleWindow"
            };
        }
    }
}