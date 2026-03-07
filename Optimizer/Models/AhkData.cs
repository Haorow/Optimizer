using System.Collections.Generic;

namespace Optimizer.Models
{
    /// <summary>
    /// Données formatées pour la génération de scripts AutoHotkey v2.
    /// </summary>
    public class AhkData
    {
        public string[] WindowTitles { get; set; } = [];
        public bool IsOptimizerVisible { get; set; }

        // Setup
        /// <summary>
        /// Délai inter-actions en ms selon la puissance du PC.
        /// 0 = Rapide, 100 = Normal, 200 = Lent.
        /// Utilisé via Sleep() explicites dans les scripts (SetWinDelay est toujours -1).
        /// </summary>
        public int SpeedDelay { get; set; } = 0;

        // Mouse Clone
        public bool MouseCloneEnabled { get; set; }
        public string MouseCloneShortcut { get; set; } = string.Empty;
        public bool MouseCloneDelays { get; set; }
        public int MouseCloneMinDelay { get; set; }
        public int MouseCloneMaxDelay { get; set; }
        public string MouseCloneLayout { get; set; } = string.Empty;
        public HashSet<string> ActiveWindows_MC { get; set; } = [];

        // Mouse Clone + AutoFollow
        public bool MouseCloneAutoFollowEnabled { get; set; }
        public string MouseCloneAutoFollowShortcut { get; set; } = string.Empty;

        // Hotkey Clone
        public bool HotkeyCloneEnabled { get; set; }
        public string HotkeyCloneShortcut { get; set; } = string.Empty;
        public bool HotkeyCloneDelays { get; set; }
        public int HotkeyCloneMinDelay { get; set; }
        public int HotkeyCloneMaxDelay { get; set; }
        public HashSet<string> ActiveWindows_HC { get; set; } = [];

        // Hotkey Clone + AutoFollow
        public bool HotkeyCloneAutoFollowEnabled { get; set; }
        public string HotkeyCloneAutoFollowShortcut { get; set; } = string.Empty;

        // Window Switcher
        public bool WindowSwitcherEnabled { get; set; }
        public string WindowSwitcherShortcut { get; set; } = string.Empty;
        public HashSet<string> ActiveWindows_WS { get; set; } = [];

        // Easy Team
        public string? EasyTeamLeaderWindow { get; set; }
        public HashSet<string> ActiveWindows_ET { get; set; } = [];

        // AutoFollow
        public string AutoFollowShortcut { get; set; } = string.Empty;
    }
}