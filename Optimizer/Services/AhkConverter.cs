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

            // Initialiser ActiveWindows_MC pour Mouse Clone
            ahkData.ActiveWindows_MC = rawData.ContainsKey("ActiveWindows_MC") && rawData["ActiveWindows_MC"] is HashSet<string> activeWindows_MC
                ? activeWindows_MC
                : new HashSet<string>();

            // Initialiser ActiveWindows_HC pour Hotkey Clone
            ahkData.ActiveWindows_HC = rawData.ContainsKey("ActiveWindows_HC") && rawData["ActiveWindows_HC"] is HashSet<string> activeWindows_HC
                ? activeWindows_HC
                : new HashSet<string>();

            // Initialiser ActiveWindows_WS pour Window Switcher
            ahkData.ActiveWindows_WS = rawData.ContainsKey("ActiveWindows_WS") && rawData["ActiveWindows_WS"] is HashSet<string> activeWindows_WS
                ? activeWindows_WS
                : new HashSet<string>();

            // Initialiser ActiveWindows_ET pour Easy Team
            ahkData.ActiveWindows_ET = rawData.ContainsKey("ActiveWindows_ET") && rawData["ActiveWindows_ET"] is HashSet<string> activeWindows_ET
                ? activeWindows_ET
                : new HashSet<string>();

            // Mouse Clone
            ahkData.MouseCloneEnabled = (bool)rawData["MC_GlobalStatus"];
            ahkData.MouseCloneShortcut = FormatShortcut(rawData["MC_Shortcut"].ToString());
            ahkData.MouseCloneDelays = (bool)rawData["MC_Delays"];
            ahkData.MouseCloneMinDelay = int.Parse(rawData["MC_MinDelay"].ToString().Replace("ms", ""));
            ahkData.MouseCloneMaxDelay = int.Parse(rawData["MC_MaxDelay"].ToString().Replace("ms", ""));
            ahkData.MouseCloneLayout = ConvertLayout(rawData["MC_Layout"]?.ToString());

            // Hotkey Clone
            ahkData.HotkeyCloneEnabled = (bool)rawData["HC_GlobalStatus"];
            ahkData.HotkeyCloneShortcut = FormatShortcut(rawData["HC_Shortcut"].ToString());
            ahkData.HotkeyCloneDelays = (bool)rawData["HC_Delays"];
            ahkData.HotkeyCloneMinDelay = int.Parse(rawData["HC_MinDelay"].ToString().Replace("ms", ""));
            ahkData.HotkeyCloneMaxDelay = int.Parse(rawData["HC_MaxDelay"].ToString().Replace("ms", ""));

            // Window Switcher
            ahkData.WindowSwitcherEnabled = (bool)rawData["WS_GlobalStatus"];
            ahkData.WindowSwitcherShortcut = FormatShortcut(rawData["WS_Shortcut"].ToString());

            // Easy Team
            if ((bool)rawData["ET_GlobalStatus"])
            {
                ahkData.EasyTeamEnabled = true;

                // Récupérer le titre de la fenêtre du chef d'équipe
                ahkData.EasyTeamLeaderWindow = rawData["ET_Leader"] as string;
            }
            else
            {
                ahkData.EasyTeamEnabled = false; // Désactiver Easy Team si non activé
            }

            return ahkData;
        }

        // Formate un raccourci pour AHK v2.
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

        // Convertit la position du curseur pour AHK v2.
        private static string ConvertLayout(string layout)
        {
            return layout switch
            {
                "Fenêtre unique" => "SingleWindow",
                "Fenêtres individuelles" => "IndividualWindows",
                _ => "SingleWindow" // Valeur par défaut
            };
        }
    }

    // Classe pour stocker les données converties pour AHK v2.
    public class AhkData
    {
        public string[] WindowTitles { get; set; }

        public bool MouseCloneEnabled { get; set; }
        public string MouseCloneShortcut { get; set; }
        public bool MouseCloneDelays { get; set; }
        public int MouseCloneMinDelay { get; set; }
        public int MouseCloneMaxDelay { get; set; }
        public string MouseCloneLayout { get; set; }
        public HashSet<string> ActiveWindows_MC { get; set; }

        public bool HotkeyCloneEnabled { get; set; }
        public string HotkeyCloneShortcut { get; set; }
        public bool HotkeyCloneDelays { get; set; }
        public int HotkeyCloneMinDelay { get; set; }
        public int HotkeyCloneMaxDelay { get; set; }
        public HashSet<string> ActiveWindows_HC { get; set; }

        public bool WindowSwitcherEnabled { get; set; }
        public string WindowSwitcherShortcut { get; set; }
        public HashSet<string> ActiveWindows_WS { get; set; }

        public bool EasyTeamEnabled { get; set; }
        public string EasyTeamLeaderWindow { get; set; }
        public HashSet<string> ActiveWindows_ET { get; set; }
    }
}