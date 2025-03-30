using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Optimizer
{
    public static class AhkConverter
    {
        /// <summary>
        /// Convertit les données de l'application en un format compatible avec AHK v2.
        /// </summary>
        public static AhkData ConvertToAhkData(Dictionary<string, object> rawData)
        {
            var ahkData = new AhkData();

            // Ajouter les noms des fenêtres
            var personnages = rawData["Personnages"] as ObservableCollection<Personnage>;
            ahkData.WindowTitles = personnages.Select(p => p.WindowName).ToArray();

            // Initialiser ActiveWindows
            ahkData.ActiveWindows = rawData.ContainsKey("ActiveWindows") && rawData["ActiveWindows"] is HashSet<string> activeWindows
                ? activeWindows
                : new HashSet<string>(); // Initialisation explicite d'un ensemble vide

            // Mouse Clone
            ahkData.MouseCloneEnabled = (bool)rawData["MC_GlobalStatus"];
            ahkData.MouseCloneShortcut = FormatShortcut(rawData["MC_Shortcut"].ToString());
            ahkData.MouseCloneMinDelay = int.Parse(rawData["MC_MinDelay"].ToString().Replace("ms", ""));
            ahkData.MouseCloneMaxDelay = int.Parse(rawData["MC_MaxDelay"].ToString().Replace("ms", ""));
            ahkData.MouseCloneLayout = ConvertLayout(rawData["MC_Layout"]?.ToString());


            // Hotkey Clone
            ahkData.HotkeyCloneEnabled = (bool)rawData["HC_GlobalStatus"];
            ahkData.HotkeyCloneShortcut = FormatShortcut(rawData["HC_Shortcut"].ToString());
            ahkData.HotkeyCloneMinDelay = int.Parse(rawData["HC_MinDelay"].ToString().Replace("ms", ""));
            ahkData.HotkeyCloneMaxDelay = int.Parse(rawData["HC_MaxDelay"].ToString().Replace("ms", ""));

            // Window Switcher
            ahkData.WindowSwitcherEnabled = (bool)rawData["WS_GlobalStatus"];
            ahkData.WindowSwitcherShortcut = FormatShortcut(rawData["WS_Shortcut"].ToString());

            // Easy Team
            ahkData.EasyTeamEnabled = (bool)rawData["ET_GlobalStatus"];
            ahkData.EasyTeamLeader = ConvertLeader(rawData["ET_Leader"]?.ToString());
            ahkData.EasyTeamTchatPos = ConvertTchatPosition(rawData["ET_TchatPos"]?.ToString());

            return ahkData;
        }

        /// <summary>
        /// Formate un raccourci pour AHK v2.
        /// </summary>
        private static string FormatShortcut(string shortcut)
        {
            var keyMap = new Dictionary<string, string>
            {
                { "Ctrl", "^" },
                { "Shift", "+" },
                { "Alt", "!" },
                { "Bouton gauche", "LButton" },
                { "Bouton droit", "RButton" },
                { "Bouton du milieu", "MButton" },
                { "Bouton latéral 1", "XButton1" },
                { "Bouton latéral 2", "XButton2" }
            };

            foreach (var mapping in keyMap)
            {
                shortcut = shortcut.Replace(mapping.Key, mapping.Value);
            }

            return shortcut;
        }

        /// <summary>
        /// Convertit la position du curseur pour AHK v2.
        /// </summary>
        private static string ConvertLayout(string layout)
        {
            return layout switch
            {
                "Fenêtre unique" => "SingleWindow",
                "Fenêtres individuelles" => "IndividualWindows",
                _ => "SingleWindow" // Valeur par défaut
            };
        }

        /// <summary>
        /// Convertit le leader sélectionné pour AHK v2.
        /// </summary>
        private static string ConvertLeader(string leader)
        {
            return leader == "Définir un chef d'équipe" ? null : leader;
        }

        /// <summary>
        /// Convertit la position du tchat pour AHK v2.
        /// </summary>
        private static (int X, int Y) ConvertTchatPosition(string tchatPos)
        {
            return tchatPos == "Définir la position du tchat" ? (0, 0) : (0, 0); // Valeurs par défaut
        }
    }

    /// <summary>
    /// Classe pour stocker les données converties pour AHK v2.
    /// </summary>
    public class AhkData
    {
        public string[] WindowTitles { get; set; }
        public HashSet<string> ActiveWindows { get; set; }

        public bool MouseCloneEnabled { get; set; }
        public string MouseCloneShortcut { get; set; }
        public int MouseCloneMinDelay { get; set; }
        public int MouseCloneMaxDelay { get; set; }
        public string MouseCloneLayout { get; set; }

        public bool HotkeyCloneEnabled { get; set; }
        public string HotkeyCloneShortcut { get; set; }
        public int HotkeyCloneMinDelay { get; set; }
        public int HotkeyCloneMaxDelay { get; set; }

        public bool WindowSwitcherEnabled { get; set; }
        public string WindowSwitcherShortcut { get; set; }

        public bool EasyTeamEnabled { get; set; }
        public string EasyTeamLeader { get; set; }
        public (int X, int Y) EasyTeamTchatPos { get; set; }
    }
}