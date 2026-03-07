using System.Collections.Generic;
using System.Windows.Input;
using Optimizer.Models;

namespace Optimizer.Services
{
    /// <summary>
    /// Service contenant la liste complète des raccourcis autorisés.
    /// </summary>
    public static class ShortcutMappingService
    {
        private static readonly Dictionary<Key, ShortcutInfo> _allowedKeys = new Dictionary<Key, ShortcutInfo>
        {
            // === TOUCHES DE FONCTION ===
            { Key.F1, new ShortcutInfo("F1", "F1") },
            { Key.F2, new ShortcutInfo("F2", "F2") },
            { Key.F3, new ShortcutInfo("F3", "F3") },
            { Key.F4, new ShortcutInfo("F4", "F4") },
            { Key.F5, new ShortcutInfo("F5", "F5") },
            { Key.F6, new ShortcutInfo("F6", "F6") },
            { Key.F7, new ShortcutInfo("F7", "F7") },
            { Key.F8, new ShortcutInfo("F8", "F8") },
            { Key.F9, new ShortcutInfo("F9", "F9") },
            { Key.F10, new ShortcutInfo("F10", "F10") },
            { Key.F11, new ShortcutInfo("F11", "F11") },
            { Key.F12, new ShortcutInfo("F12", "F12") },

            // === LETTRES A-Z ===
            { Key.A, new ShortcutInfo("A", "a") },
            { Key.B, new ShortcutInfo("B", "b") },
            { Key.C, new ShortcutInfo("C", "c") },
            { Key.D, new ShortcutInfo("D", "d") },
            { Key.E, new ShortcutInfo("E", "e") },
            { Key.F, new ShortcutInfo("F", "f") },
            { Key.G, new ShortcutInfo("G", "g") },
            { Key.H, new ShortcutInfo("H", "h") },
            { Key.I, new ShortcutInfo("I", "i") },
            { Key.J, new ShortcutInfo("J", "j") },
            { Key.K, new ShortcutInfo("K", "k") },
            { Key.L, new ShortcutInfo("L", "l") },
            { Key.M, new ShortcutInfo("M", "m") },
            { Key.N, new ShortcutInfo("N", "n") },
            { Key.O, new ShortcutInfo("O", "o") },
            { Key.P, new ShortcutInfo("P", "p") },
            { Key.Q, new ShortcutInfo("Q", "q") },
            { Key.R, new ShortcutInfo("R", "r") },
            { Key.S, new ShortcutInfo("S", "s") },
            { Key.T, new ShortcutInfo("T", "t") },
            { Key.U, new ShortcutInfo("U", "u") },
            { Key.V, new ShortcutInfo("V", "v") },
            { Key.W, new ShortcutInfo("W", "w") },
            { Key.X, new ShortcutInfo("X", "x") },
            { Key.Y, new ShortcutInfo("Y", "y") },
            { Key.Z, new ShortcutInfo("Z", "z") },

            // === PAVÉ NUMÉRIQUE ===
            { Key.NumPad0, new ShortcutInfo("0 (pavé numérique)", "Numpad0") },
            { Key.NumPad1, new ShortcutInfo("1 (pavé numérique)", "Numpad1") },
            { Key.NumPad2, new ShortcutInfo("2 (pavé numérique)", "Numpad2") },
            { Key.NumPad3, new ShortcutInfo("3 (pavé numérique)", "Numpad3") },
            { Key.NumPad4, new ShortcutInfo("4 (pavé numérique)", "Numpad4") },
            { Key.NumPad5, new ShortcutInfo("5 (pavé numérique)", "Numpad5") },
            { Key.NumPad6, new ShortcutInfo("6 (pavé numérique)", "Numpad6") },
            { Key.NumPad7, new ShortcutInfo("7 (pavé numérique)", "Numpad7") },
            { Key.NumPad8, new ShortcutInfo("8 (pavé numérique)", "Numpad8") },
            { Key.NumPad9, new ShortcutInfo("9 (pavé numérique)", "Numpad9") },

            // === FLÈCHES ===
            { Key.Up, new ShortcutInfo("↑", "Up") },
            { Key.Down, new ShortcutInfo("↓", "Down") },
            { Key.Left, new ShortcutInfo("←", "Left") },
            { Key.Right, new ShortcutInfo("→", "Right") },

            // === TOUCHES SPÉCIALES ===
            { Key.Space, new ShortcutInfo("Espace", "Space") },
            { Key.Tab, new ShortcutInfo("Tabulation", "Tab") }
        };

        private static readonly Dictionary<MouseButton, ShortcutInfo> _allowedMouseButtons = new Dictionary<MouseButton, ShortcutInfo>
        {
            { MouseButton.Right, new ShortcutInfo("Bouton droit", "RButton") },
            { MouseButton.Middle, new ShortcutInfo("Bouton molette", "MButton") },
            { MouseButton.XButton1, new ShortcutInfo("Bouton latéral 1", "XButton1") },
            { MouseButton.XButton2, new ShortcutInfo("Bouton latéral 2", "XButton2") }
        };

        /// <summary>
        /// Vérifie si une touche est autorisée.
        /// </summary>
        public static bool IsKeyAllowed(Key key)
        {
            return _allowedKeys.ContainsKey(key);
        }

        /// <summary>
        /// Vérifie si un bouton de souris est autorisé.
        /// </summary>
        public static bool IsMouseButtonAllowed(MouseButton button)
        {
            return _allowedMouseButtons.ContainsKey(button);
        }

        /// <summary>
        /// Récupère les informations d'un raccourci clavier.
        /// </summary>
        public static ShortcutInfo? GetShortcutInfo(Key key)
        {
            return _allowedKeys.TryGetValue(key, out var info) ? info : null;
        }

        /// <summary>
        /// Récupère les informations d'un bouton de souris.
        /// </summary>
        public static ShortcutInfo? GetShortcutInfo(MouseButton button)
        {
            return _allowedMouseButtons.TryGetValue(button, out var info) ? info : null;
        }

        /// <summary>
        /// Convertit un nom affiché en touche AutoHotkey.
        /// </summary>
        public static string GetAhkKey(string displayName)
        {
            // Chercher dans les touches clavier
            foreach (var kvp in _allowedKeys)
            {
                if (kvp.Value.DisplayName == displayName)
                    return kvp.Value.AhkKey;
            }

            // Chercher dans les boutons souris
            foreach (var kvp in _allowedMouseButtons)
            {
                if (kvp.Value.DisplayName == displayName)
                    return kvp.Value.AhkKey;
            }

            return displayName; // Fallback
        }
    }
}