namespace Optimizer.Models
{
    /// <summary>
    /// Informations sur un raccourci autorisé.
    /// </summary>
    public class ShortcutInfo
    {
        /// <summary>
        /// Nom affiché à l'utilisateur (ex: "F1", "A", "Num5", "↑").
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Touche au format AutoHotkey v2 (ex: "F1", "a", "Numpad5", "Up").
        /// </summary>
        public string AhkKey { get; set; }
        public ShortcutInfo(string displayName, string ahkKey)
        {
            DisplayName = displayName;
            AhkKey = ahkKey;
        }
    }
}