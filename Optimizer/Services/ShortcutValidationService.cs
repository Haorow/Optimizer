using System.Windows.Input;

namespace Optimizer.Services
{
    /// <summary>
    /// Service de validation des raccourcis (version simplifiée).
    /// </summary>
    public static class ShortcutValidationService
    {
        /// <summary>
        /// Vérifie si une touche est une touche modificatrice (Ctrl, Shift, Alt).
        /// </summary>
        public static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin ||
                   key == Key.System;
        }
    }
}