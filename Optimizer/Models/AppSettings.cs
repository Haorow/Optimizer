namespace Optimizer.Models
{
    public class AppSettings
    {
        // ⚠︎ Les valeurs par défaut doivent être synchronisées avec celles de MainViewModel.cs
        // Propriétés concernées : ExecutionSpeedIndex,
        //                         MouseCloneShortcut, MouseCloneDelaysEnabled, MouseCloneMinDelay, MouseCloneMaxDelay, MouseCloneLayoutIndex,
        //                         HotkeyCloneShortcut, HotkeyCloneDelaysEnabled, HotkeyCloneMinDelay, HotkeyCloneMaxDelay,
        //                         WindowSwitcherShortcut,
        //                         AutoFollowShortcut

        #region Setup
        public int ExecutionSpeedIndex { get; set; } = 1;

        #endregion

        #region Mouse Clone

        public bool IsMouseCloneEnabled { get; set; }
        public string MouseCloneShortcut { get; set; } = "F1";
        public bool MouseCloneDelaysEnabled { get; set; } = true;
        public int MouseCloneMinDelay { get; set; } = 50;
        public int MouseCloneMaxDelay { get; set; } = 125;
        public int MouseCloneLayoutIndex { get; set; } = 1;

        #endregion

        #region Hotkey Clone

        public bool IsHotkeyCloneEnabled { get; set; }
        public string HotkeyCloneShortcut { get; set; } = "F2";
        public bool HotkeyCloneDelaysEnabled { get; set; } = true;
        public int HotkeyCloneMinDelay { get; set; } = 50;
        public int HotkeyCloneMaxDelay { get; set; } = 125;

        #endregion

        #region Window Switcher

        public bool IsWindowSwitcherEnabled { get; set; }
        public string WindowSwitcherShortcut { get; set; } = "F3";

        #endregion

        #region Easy Team

        public string? SelectedLeaderName { get; set; }

        #endregion

        #region AutoFollow

        public bool IsMouseCloneAutoFollowEnabled { get; set; }
        public bool IsHotkeyCloneAutoFollowEnabled { get; set; }
        public string AutoFollowShortcut { get; set; } = "F5";

        #endregion

        #region Interface

        public bool IsSettingsPanelExpanded { get; set; }
        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }

        #endregion
    }
}