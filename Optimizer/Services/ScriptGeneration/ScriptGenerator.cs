using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Optimizer.Models;
using Optimizer.Services;
using Optimizer.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace Optimizer.Services.ScriptGeneration
{
    public static class ScriptGenerator
    {
        #region Mouse Clone

        public static void Generate_MC_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // === EN-TÊTE ===
                AppendAhkHeader(script);

                // === CONFIGURATION ===
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");

                if (!ahkData.MouseCloneEnabled || ahkData.ActiveWindows_MC.Count == 0)
                {
                    SaveEmptyScript("MouseClone.ahk", "Aucune fenêtre active pour Mouse Clone");
                    return;
                }

                // Filtrer les fenêtres actives
                var activeWindows_MC = ahkData.WindowTitles
                    .Where(title => ahkData.ActiveWindows_MC.Contains(title))
                    .ToArray();

                if (activeWindows_MC.Length == 0)
                {
                    SaveEmptyScript("MouseClone.ahk", "Aucune fenêtre active pour Mouse Clone");
                    return;
                }

                // Liste des fenêtres actives
                script.AppendLine("global ActiveWindows := [");
                foreach (var title in activeWindows_MC)
                {
                    script.AppendLine($"    \"{EscapeString(title)}\",");
                }
                script.AppendLine("]");
                script.AppendLine("");

                // Configuration des délais
                script.AppendLine("; Configuration des délais");
                script.AppendLine($"global SpeedDelayMs := {ahkData.SpeedDelay}");
                script.AppendLine($"global DelaysEnabled := {(ahkData.MouseCloneDelays ? "true" : "false")}");
                script.AppendLine($"global DelayMin := {ahkData.MouseCloneMinDelay}");
                script.AppendLine($"global DelayMax := {ahkData.MouseCloneMaxDelay}");
                script.AppendLine("");

                // Configuration du layout
                script.AppendLine("; Configuration du mode de réplication");
                script.AppendLine($"global IndividualWindows := {(ahkData.MouseCloneLayout == "IndividualWindows" ? "true" : "false")}");
                script.AppendLine("");

                // Configuration AutoFollow (si activé)
                if (ahkData.MouseCloneAutoFollowEnabled)
                {
                    ValidateLeaderWindow(ahkData.EasyTeamLeaderWindow, "Mouse Clone AutoFollow");

                    script.AppendLine("; Configuration AutoFollow");
                    script.AppendLine($"global LeaderWindow := \"{EscapeString(ahkData.EasyTeamLeaderWindow)}\"");
                    script.AppendLine($"global AutoFollowKey := \"{GetValidAhkKey(ahkData.AutoFollowShortcut)}\"");
                    script.AppendLine("");
                }

                // === FONCTIONS UTILITAIRES ===
                script.AppendLine("; === FONCTIONS UTILITAIRES ===");
                script.AppendLine("");

                script.AppendLine("; Vérifie si la fenêtre active est autorisée");
                script.AppendLine("IsWindowActive() {");
                script.AppendLine("    CurrentWindowTitle := WinGetTitle(\"A\")");
                script.AppendLine("    for title in ActiveWindows {");
                script.AppendLine("        if (CurrentWindowTitle = title) {");
                script.AppendLine("            return true");
                script.AppendLine("        }");
                script.AppendLine("    }");
                script.AppendLine("    return false");
                script.AppendLine("}");
                script.AppendLine("");

                script.AppendLine("; Active le hotkey uniquement si une fenêtre de la liste est active");
                script.AppendLine("IsAnyWindowActive() {");
                script.AppendLine("    for title in ActiveWindows {");
                script.AppendLine("        if (WinActive(title)) {");
                script.AppendLine("            return true");
                script.AppendLine("        }");
                script.AppendLine("    }");
                script.AppendLine("    return false");
                script.AppendLine("}");
                script.AppendLine("");

                // Fonction PerformClicks
                script.AppendLine("; Fonction commune pour effectuer les clics");
                script.AppendLine("; La première fenêtre est traitée sans délai — les suivantes attendent SpeedDelayMs + délai aléatoire");
                script.AppendLine("PerformClicks() {");
                script.AppendLine("    InitialWindow := WinGetTitle(\"A\")");
                script.AppendLine("    isFirst := true");
                script.AppendLine("");
                script.AppendLine("    for title in ActiveWindows {");
                script.AppendLine("        if (WinExist(title)) {");
                script.AppendLine("            ; Délais inter-fenêtres (ignorés pour la première fenêtre)");
                script.AppendLine("            if (!isFirst) {");
                script.AppendLine("                if (SpeedDelayMs > 0)");
                script.AppendLine("                    Sleep(SpeedDelayMs)");
                script.AppendLine("                if (DelaysEnabled) {");
                script.AppendLine("                    DelayRand := Random(DelayMin, DelayMax)");
                script.AppendLine("                    Sleep(DelayRand)");
                script.AppendLine("                }");
                script.AppendLine("            }");
                script.AppendLine("            isFirst := false");
                script.AppendLine("");
                script.AppendLine("            if (IndividualWindows) {");
                script.AppendLine("                ; Mode fenêtres individuelles : WinActivate + Click + ControlClick");
                script.AppendLine("                WinActivate(title)");
                script.AppendLine("                WinWaitActive(title,, 1)");
                script.AppendLine("                Sleep(SpeedDelayMs > 0 ? SpeedDelayMs : 10)");
                script.AppendLine("                Click(\"left\")");
                script.AppendLine("                ControlClick(, title,, \"left\")");
                script.AppendLine("            } else {");
                script.AppendLine("                ; Mode fenêtre unique (réservé) : ControlClick sans activation");
                script.AppendLine("                ControlClick(, title,, \"left\")");
                script.AppendLine("            }");
                script.AppendLine("        }");
                script.AppendLine("    }");
                script.AppendLine("");
                script.AppendLine("    ; Revenir à la fenêtre initiale en mode individuel");
                script.AppendLine("    if (IndividualWindows && WinExist(InitialWindow)) {");
                script.AppendLine("        if (SpeedDelayMs > 0)");
                script.AppendLine("            Sleep(SpeedDelayMs)");
                script.AppendLine("        WinActivate(InitialWindow)");
                script.AppendLine("        WinWaitActive(InitialWindow,, 1)");
                script.AppendLine("    }");
                script.AppendLine("}");
                script.AppendLine("");

                // === HOTKEY PRINCIPAL ===
                script.AppendLine("; === HOTKEY PRINCIPAL ===");
                script.AppendLine("");
                script.AppendLine("#HotIf IsAnyWindowActive()");

                string ahkShortcut = GetValidAhkKey(ahkData.MouseCloneShortcut);
                script.AppendLine($"{ahkShortcut}::");
                script.AppendLine("{");
                script.AppendLine("    if (!IsWindowActive()) {");
                script.AppendLine("        return");
                script.AppendLine("    }");
                script.AppendLine("");
                script.AppendLine("    PerformClicks()");
                script.AppendLine("}");
                script.AppendLine("");

                // === HOTKEY MC + AUTOFOLLOW (si activé) ===
                if (ahkData.MouseCloneAutoFollowEnabled)
                {
                    script.AppendLine("; === HOTKEY MC + AUTOFOLLOW ===");
                    script.AppendLine("");

                    string ahkAutoFollowShortcut = ConvertToAltShortcut(ahkData.MouseCloneShortcut);

                    script.AppendLine($"{ahkAutoFollowShortcut}::");
                    script.AppendLine("{");
                    script.AppendLine("    if (!IsWindowActive()) {");
                    script.AppendLine("        return");
                    script.AppendLine("    }");
                    script.AppendLine("");
                    script.AppendLine("    ; Étape 1 : Effectuer les clics sur toutes les fenêtres");
                    script.AppendLine("    PerformClicks()");
                    script.AppendLine("");
                    script.AppendLine("    ; Étape 2 : Basculer sur la fenêtre du Leader");
                    script.AppendLine("    if (WinExist(LeaderWindow)) {");
                    script.AppendLine("        WinActivate(LeaderWindow)");
                    script.AppendLine("        WinWaitActive(LeaderWindow,, 1)");
                    script.AppendLine("        Sleep(50)");
                    script.AppendLine("");
                    script.AppendLine("        ; Étape 3 : Envoyer le raccourci AutoFollow");
                    script.AppendLine("        SendInput(\"{\" AutoFollowKey \"}\")");
                    script.AppendLine("    }");
                    script.AppendLine("}");
                    script.AppendLine("");
                }

                script.AppendLine("#HotIf");

                SaveScript("MouseClone.ahk", script.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du script Mouse Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Hotkey Clone

        public static void Generate_HC_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // === EN-TÊTE ===
                AppendAhkHeader(script, includeWinActivateForce: true);

                // === CONFIGURATION ===
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");

                var activeWindows_HC = ahkData.WindowTitles
                    .Where(title => ahkData.ActiveWindows_HC.Contains(title))
                    .ToArray();

                if (activeWindows_HC.Length == 0)
                {
                    SaveEmptyScript("HotkeyClone.ahk", "Aucune fenêtre active pour Hotkey Clone");
                    return;
                }

                // === RÉORGANISER L'ORDRE DES FENÊTRES ===
                string[] orderedWindows;

                if (!string.IsNullOrEmpty(ahkData.EasyTeamLeaderWindow) &&
                    activeWindows_HC.Contains(ahkData.EasyTeamLeaderWindow))
                {
                    orderedWindows = new[] { ahkData.EasyTeamLeaderWindow }
                        .Concat(activeWindows_HC.Where(w => w != ahkData.EasyTeamLeaderWindow))
                        .ToArray();
                }
                else
                {
                    orderedWindows = activeWindows_HC;
                }

                // Définir les fenêtres dans le bon ordre
                for (int i = 0; i < orderedWindows.Length; i++)
                {
                    script.AppendLine($"global Window{i + 1} := \"{EscapeString(orderedWindows[i])}\"");
                }
                script.AppendLine("");

                // Configuration des délais
                script.AppendLine("; Configuration des délais");
                script.AppendLine($"global SpeedDelayMs := {ahkData.SpeedDelay}");
                script.AppendLine($"global DelaysEnabled := {(ahkData.HotkeyCloneDelays ? "true" : "false")}");
                script.AppendLine($"global DelayMin := {ahkData.HotkeyCloneMinDelay}");
                script.AppendLine($"global DelayMax := {ahkData.HotkeyCloneMaxDelay}");
                script.AppendLine("");

                // Configuration AutoFollow (si activé)
                if (ahkData.HotkeyCloneAutoFollowEnabled)
                {
                    ValidateLeaderWindow(ahkData.EasyTeamLeaderWindow, "Hotkey Clone AutoFollow");

                    script.AppendLine("; Configuration AutoFollow");
                    script.AppendLine($"global LeaderWindow := \"{EscapeString(ahkData.EasyTeamLeaderWindow ?? string.Empty)}\"");
                    script.AppendLine($"global AutoFollowKey := \"{GetValidAhkKey(ahkData.AutoFollowShortcut)}\"");
                    script.AppendLine("");
                }

                // === VARIABLES GLOBALES ===
                script.AppendLine("; === VARIABLES GLOBALES ===");
                script.AppendLine("global Toggle := false");
                script.AppendLine("global TextBuffer := \"\"");
                script.AppendLine("global ClickPos := []");
                script.AppendLine("global IH := InputHook(\"V\")");
                script.AppendLine("");

                // === CAPTURE DES CLICS ===
                script.AppendLine("; === CAPTURE DES CLICS PENDANT L'ENREGISTREMENT ===");
                script.AppendLine("~LButton:: {");
                script.AppendLine("    global Toggle, ClickPos");
                script.AppendLine("    if (Toggle) {");
                script.AppendLine("        MouseGetPos(&x, &y, &winID)");
                script.AppendLine("        if (WinExist(\"ahk_id \" winID) && WinActive(\"ahk_id \" winID)) {");
                script.AppendLine("            if (WinActive(Window1)) {");
                script.AppendLine("                MouseGetPos(&x, &y)");
                script.AppendLine("                WinGetPos(&winX, &winY,,, Window1)");
                script.AppendLine("                ClickPos.Push({x: x - winX, y: y - winY})");
                script.AppendLine("            } else {");
                script.AppendLine("                Toggle := false");
                script.AppendLine("                ClickPos := []");
                script.AppendLine("                TextBuffer := \"\"");
                script.AppendLine("                MsgBox(\"Enregistrement annulé : clic en dehors de la fenêtre cible.\")");
                script.AppendLine("            }");
                script.AppendLine("        } else {");
                script.AppendLine("            Toggle := false");
                script.AppendLine("            ClickPos := []");
                script.AppendLine("            TextBuffer := \"\"");
                script.AppendLine("            MsgBox(\"Enregistrement annulé : clic en dehors de la fenêtre cible.\")");
                script.AppendLine("        }");
                script.AppendLine("    }");
                script.AppendLine("    return");
                script.AppendLine("}");
                script.AppendLine("");

                // === HOTKEY PRINCIPAL ===
                script.AppendLine("; === HOTKEY PRINCIPAL ===");
                script.AppendLine("");

                AppendIsAnyWindowActiveFunction(script, orderedWindows.Select((w, i) => $"Window{i + 1}").ToArray());

                script.AppendLine("#HotIf IsAnyWindowActive()");
                string ahkShortcut = GetValidAhkKey(ahkData.HotkeyCloneShortcut);

                script.AppendLine($"{ahkShortcut}:: {{");
                script.AppendLine("    ProcessHotkeyClone(false)");
                script.AppendLine("}");
                script.AppendLine("");

                if (ahkData.HotkeyCloneAutoFollowEnabled)
                {
                    script.AppendLine($"!{ahkShortcut}:: {{");
                    script.AppendLine("    ProcessHotkeyClone(true)");
                    script.AppendLine("}");
                    script.AppendLine("");
                }

                script.AppendLine("#HotIf");
                script.AppendLine("");

                // === FONCTION PRINCIPALE ===
                script.AppendLine("; === FONCTION DE TRAITEMENT ===");
                script.AppendLine("ProcessHotkeyClone(withAutoFollow) {");
                script.AppendLine("    global Toggle, TextBuffer, ClickPos, IH");
                script.AppendLine("");

                script.AppendLine("    if (!Toggle) {");
                script.AppendLine("        ; === DÉMARRAGE DE L'ENREGISTREMENT ===");
                script.AppendLine("        ClickPos := []");
                script.AppendLine("        TextBuffer := \"\"");
                script.AppendLine("        IH.Stop()");
                script.AppendLine("        IH := InputHook(\"V\")");
                script.AppendLine("");
                script.AppendLine("        if (WinExist(Window1)) {");
                script.AppendLine("            if (!WinActive(Window1)) {");
                script.AppendLine("                WinActivate(Window1)");
                script.AppendLine("                WinWaitActive(Window1,, 2)");
                script.AppendLine("                Sleep(50)");
                script.AppendLine("            }");
                script.AppendLine("            Toggle := true");
                script.AppendLine("            TextBuffer := \"\"");
                script.AppendLine("            ClickPos := []");
                script.AppendLine("            IH.Start()");
                script.AppendLine("        } else {");
                script.AppendLine("            MsgBox(\"La fenêtre cible n'existe pas.\")");
                script.AppendLine("        }");
                script.AppendLine("    } else {");
                script.AppendLine("        ; === FIN DE L'ENREGISTREMENT ET RÉPLICATION ===");
                script.AppendLine("        Toggle := false");
                script.AppendLine("        IH.Stop()");
                script.AppendLine("        TextBuffer := IH.Input");
                script.AppendLine("");
                script.AppendLine("        ; Détection de la méthode");
                script.AppendLine("        Methode := (StrLen(TextBuffer) <= 1) ? \"Standard\" : \"Alternative\"");
                script.AppendLine("");

                script.AppendLine("        ; Réplication sur les autres fenêtres");
                script.AppendLine("        ; La première fenêtre répliquée est traitée sans délai");
                var otherWindows = orderedWindows.Skip(1).Select((w, i) => $"Window{i + 2}").ToArray();
                script.AppendLine($"        isFirst := true");
                script.AppendLine($"        for window in [{string.Join(", ", otherWindows)}] {{");
                script.AppendLine("            try {");
                script.AppendLine("                if (!isFirst) {");
                script.AppendLine("                    if (SpeedDelayMs > 0)");
                script.AppendLine("                        Sleep(SpeedDelayMs)");
                script.AppendLine("                    if (DelaysEnabled) {");
                script.AppendLine("                        DelayRand := Random(DelayMin, DelayMax)");
                script.AppendLine("                        Sleep(DelayRand)");
                script.AppendLine("                    }");
                script.AppendLine("                }");
                script.AppendLine("                isFirst := false");
                script.AppendLine("");
                script.AppendLine("                WinActivate(window)");
                script.AppendLine("                WinWaitActive(window,, 2)");
                script.AppendLine("                Sleep(SpeedDelayMs > 0 ? SpeedDelayMs : 50)");
                script.AppendLine("");
                script.AppendLine("                ; Reproduction des clics");
                script.AppendLine("                if (ClickPos.Length > 0) {");
                script.AppendLine("                    for pos in ClickPos {");
                script.AppendLine("                        WinGetPos(&winX, &winY,,, window)");
                script.AppendLine("                        MouseMove(winX + pos.x, winY + pos.y, 0)");
                script.AppendLine("                        Click");
                script.AppendLine("                        Sleep(SpeedDelayMs > 0 ? SpeedDelayMs : 50)");
                script.AppendLine("                    }");
                script.AppendLine("                }");
                script.AppendLine("");
                script.AppendLine("                ; Méthode Standard");
                script.AppendLine("                if (Methode = \"Standard\") {");
                script.AppendLine("                    SetKeyDelay(20, 20)");
                script.AppendLine("                    Loop Parse TextBuffer {");
                script.AppendLine("                        if (!WinActive(window)) {");
                script.AppendLine("                            WinActivate(window)");
                script.AppendLine("                            WinWaitActive(window,, 2)");
                script.AppendLine("                        }");
                script.AppendLine("                        SendInput(\"{Raw}\" A_LoopField)");
                script.AppendLine("                        Sleep(20)");
                script.AppendLine("                    }");
                script.AppendLine("                } else {");
                script.AppendLine("                    ; Méthode Alternative");
                script.AppendLine("                    SavedClip := ClipboardAll()");
                script.AppendLine("                    A_Clipboard := \"\"");
                script.AppendLine("                    A_Clipboard := TextBuffer");
                script.AppendLine("                    ClipWait(2)");
                script.AppendLine("                    SendInput(\"^v\")");
                script.AppendLine("                    Sleep(50)");
                script.AppendLine("                    A_Clipboard := SavedClip");
                script.AppendLine("                }");
                script.AppendLine("");
                script.AppendLine("            } catch {");
                script.AppendLine("                MsgBox(\"Échec de la réplication dans \" window \"!\")");
                script.AppendLine("            }");
                script.AppendLine("        }");
                script.AppendLine("");

                // === RETOUR À LA FENÊTRE APPROPRIÉE ===
                if (ahkData.HotkeyCloneAutoFollowEnabled)
                {
                    script.AppendLine("        ; Retour à la fenêtre appropriée");
                    script.AppendLine("        if (withAutoFollow) {");
                    script.AppendLine($"            if (WinExist(\"{EscapeString(ahkData.EasyTeamLeaderWindow ?? string.Empty)}\")) {{");
                    script.AppendLine("                if (SpeedDelayMs > 0)");
                    script.AppendLine("                    Sleep(SpeedDelayMs)");
                    script.AppendLine($"                WinActivate(\"{EscapeString(ahkData.EasyTeamLeaderWindow ?? string.Empty)}\")");
                    script.AppendLine($"                WinWaitActive(\"{EscapeString(ahkData.EasyTeamLeaderWindow ?? string.Empty)}\",, 1)");
                    script.AppendLine("                Sleep(50)");
                    string autoFollowKey = GetValidAhkKey(ahkData.AutoFollowShortcut);
                    script.AppendLine($"                SendInput(\"{{{autoFollowKey}}}\")");
                    script.AppendLine("            } else {");
                    script.AppendLine("                if (SpeedDelayMs > 0)");
                    script.AppendLine("                    Sleep(SpeedDelayMs)");
                    script.AppendLine("                WinActivate(Window1)");
                    script.AppendLine("                WinWaitActive(Window1,, 1)");
                    script.AppendLine("            }");
                    script.AppendLine("        } else {");
                    script.AppendLine("            if (SpeedDelayMs > 0)");
                    script.AppendLine("                Sleep(SpeedDelayMs)");
                    script.AppendLine("            WinActivate(Window1)");
                    script.AppendLine("            WinWaitActive(Window1,, 1)");
                    script.AppendLine("        }");
                }
                else
                {
                    script.AppendLine("        ; Retour à la fenêtre source");
                    script.AppendLine("        if (SpeedDelayMs > 0)");
                    script.AppendLine("            Sleep(SpeedDelayMs)");
                    script.AppendLine("        WinActivate(Window1)");
                    script.AppendLine("        WinWaitActive(Window1,, 1)");
                }

                script.AppendLine("    }");
                script.AppendLine("}");
                script.AppendLine("");

                script.AppendLine("; Empêcher la saisie hors de Window1 pendant l'enregistrement");
                script.AppendLine("#HotIf Toggle && WinActive(Window1)");
                script.AppendLine("~*::Return");
                script.AppendLine("#HotIf");

                SaveScript("HotkeyClone.ahk", script.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du script Hotkey Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Window Switcher

        public static void Generate_WS_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // === EN-TÊTE ===
                AppendAhkHeader(script);

                // === CONFIGURATION ===
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");

                var activeWindows_WS = ahkData.WindowTitles
                    .Where(title => ahkData.ActiveWindows_WS.Contains(title))
                    .ToArray();

                if (activeWindows_WS.Length == 0)
                {
                    SaveEmptyScript("WindowSwitcher.ahk", "Aucune fenêtre active pour Window Switcher");
                    return;
                }

                // Définir les fenêtres
                for (int i = 0; i < activeWindows_WS.Length; i++)
                {
                    script.AppendLine($"global Window{i + 1} := \"{EscapeString(activeWindows_WS[i])}\"");
                }
                script.AppendLine("");

                // === HOTKEY PRINCIPAL ===
                script.AppendLine("; === HOTKEY PRINCIPAL ===");
                script.AppendLine("");

                AppendIsAnyWindowActiveFunction(script, activeWindows_WS.Select((w, i) => $"Window{i + 1}").ToArray());

                script.AppendLine("#HotIf IsAnyWindowActive()");
                string ahkShortcut = GetValidAhkKey(ahkData.WindowSwitcherShortcut);
                script.AppendLine($"{ahkShortcut}::");
                script.AppendLine("{");
                script.AppendLine("    ; Basculer vers la fenêtre suivante");

                for (int i = 0; i < activeWindows_WS.Length; i++)
                {
                    string currentWindow = $"Window{i + 1}";
                    string nextWindow = i < activeWindows_WS.Length - 1 ? $"Window{i + 2}" : "Window1";

                    script.AppendLine($"    if (WinActive({currentWindow})) {{");
                    script.AppendLine($"        if (WinExist({nextWindow})) {{");
                    script.AppendLine($"            WinActivate({nextWindow})");
                    script.AppendLine("        }");
                    script.AppendLine("        return");
                    script.AppendLine("    }");
                }

                script.AppendLine("");
                script.AppendLine("    ; Si aucune fenêtre active, activer la première");
                script.AppendLine("    if (WinExist(Window1)) {");
                script.AppendLine("        WinActivate(Window1)");
                script.AppendLine("    } else if (WinExist(Window2)) {");
                script.AppendLine("        WinActivate(Window2)");
                script.AppendLine("    }");
                script.AppendLine("}");
                script.AppendLine("");
                script.AppendLine("#HotIf");

                SaveScript("WindowSwitcher.ahk", script.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du script Window Switcher : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Easy Team

        public static void Generate_ET_Script(AhkData ahkData)
        {
            try
            {
                if (ahkData.ActiveWindows_ET == null || ahkData.ActiveWindows_ET.Count < 2)
                {
                    throw new InvalidOperationException("Easy Team nécessite au moins 2 fenêtres actives !");
                }

                ValidateLeaderWindow(ahkData.EasyTeamLeaderWindow, "Easy Team");

                var script = new StringBuilder();

                // === EN-TÊTE ===
                AppendAhkHeader(script, includeWinActivateForce: true);

                // === CONFIGURATION ===
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");
                script.AppendLine($"global LeaderWindow := \"{EscapeString(ahkData.EasyTeamLeaderWindow)}\"");
                script.AppendLine($"global SpeedDelayMs := {ahkData.SpeedDelay}");
                script.AppendLine("");

                // === VÉRIFICATIONS ===
                script.AppendLine("; === VÉRIFICATIONS ===");
                script.AppendLine("if (!WinExist(LeaderWindow)) {");
                script.AppendLine("    MsgBox(\"La fenêtre du leader n'existe pas !\", \"Erreur\", 16)");
                script.AppendLine("    ExitApp");
                script.AppendLine("}");
                script.AppendLine("");

                // === ACTIVATION DE LA FENÊTRE ===
                script.AppendLine("; === ACTIVATION DE LA FENÊTRE DU LEADER ===");
                script.AppendLine("WinActivate(LeaderWindow)");
                script.AppendLine("WinWaitActive(LeaderWindow,, 2)");
                script.AppendLine("Sleep(100)");
                script.AppendLine("");

                // === OUVERTURE DU TCHAT ===
                script.AppendLine("; === OUVERTURE DU TCHAT ===");
                script.AppendLine("SendInput(\"{Space}\")");
                script.AppendLine("Sleep(150)");
                script.AppendLine("");

                // === INVITATIONS ===
                // La première invitation est envoyée sans délai SpeedDelay.
                script.AppendLine("; === INVITATIONS DES MEMBRES ===");
                bool isFirstInvite = true;
                foreach (var window in ahkData.ActiveWindows_ET.Where(w => w != ahkData.EasyTeamLeaderWindow))
                {
                    string characterName = ExtractCharacterName(window);
                    script.AppendLine($"; Invitation de {characterName}");
                    if (!isFirstInvite)
                    {
                        script.AppendLine("if (SpeedDelayMs > 0)");
                        script.AppendLine($"    Sleep(SpeedDelayMs)");
                    }
                    isFirstInvite = false;
                    script.AppendLine($"SendInput(\"/invite {characterName}\")");
                    script.AppendLine("Sleep(SpeedDelayMs > 0 ? SpeedDelayMs : 50)");
                    script.AppendLine("SendInput(\"{Enter}\")");
                    script.AppendLine("Sleep(50)");
                }
                script.AppendLine("");

                // === RETOUR À OPTIMIZER ===
                if (ahkData.IsOptimizerVisible)
                {
                    script.AppendLine("; === RETOUR À OPTIMIZER ===");
                    script.AppendLine("if WinExist(\"Optimizer\")");
                    script.AppendLine("{");
                    script.AppendLine("    WinActivate(\"Optimizer\")");
                    script.AppendLine("    Sleep(50)");
                    script.AppendLine("}");
                }
                script.AppendLine("ExitApp");

                SaveScript("EasyTeam.ahk", script.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du script Easy Team : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Méthodes utilitaires

        private static void AppendAhkHeader(StringBuilder script, bool includeWinActivateForce = false)
        {
            script.AppendLine("#Requires AutoHotkey v2.0");
            script.AppendLine("#NoTrayIcon");
            script.AppendLine("#SingleInstance Force");
            if (includeWinActivateForce)
            {
                script.AppendLine("#WinActivateForce");
            }
            // SetWinDelay désactivé — les délais inter-fenêtres sont gérés via Sleep(SpeedDelayMs) explicites
            script.AppendLine("SetWinDelay(-1)");
            script.AppendLine("");
        }

        private static void AppendIsAnyWindowActiveFunction(StringBuilder script, string[] windowVariables)
        {
            script.AppendLine("; Active le hotkey uniquement si une fenêtre de la liste est active");
            script.AppendLine("IsAnyWindowActive() {");

            foreach (var windowVar in windowVariables)
            {
                script.AppendLine($"    if (WinActive({windowVar})) {{");
                script.AppendLine("        return true");
                script.AppendLine("    }");
            }

            script.AppendLine("    return false");
            script.AppendLine("}");
            script.AppendLine("");
        }

        private static void SaveEmptyScript(string fileName, string reason)
        {
            var script = new StringBuilder();
            script.AppendLine("#Requires AutoHotkey v2.0");
            script.AppendLine("#NoTrayIcon");
            script.AppendLine("#SingleInstance Force");
            script.AppendLine("SetWinDelay(-1)");
            script.AppendLine("");
            script.AppendLine($"; {reason}");

            SaveScript(fileName, script.ToString());
        }

        private static string GetValidAhkKey(string shortcut)
        {
            string ahkKey = ShortcutMappingService.GetAhkKey(shortcut);

            if (string.IsNullOrEmpty(ahkKey))
            {
                throw new InvalidOperationException($"Raccourci invalide : '{shortcut}' ne peut pas être converti en touche AutoHotkey.");
            }

            return ahkKey;
        }

        private static void ValidateLeaderWindow(string? leaderWindow, string feature)
        {
            if (string.IsNullOrEmpty(leaderWindow))
            {
                throw new InvalidOperationException($"{feature} activé mais aucun leader défini !");
            }
        }

        private static string EscapeString(string? input)
        {
            return input?.Replace("\"", "\"\"") ?? string.Empty;
        }

        private static string ExtractCharacterName(string windowName)
        => WindowDiscoveryService.ExtractCharacterName(windowName);

        private static string ConvertToAltShortcut(string shortcut)
        {
            string ahkKey = GetValidAhkKey(shortcut);
            return "!" + ahkKey;
        }

        private static void SaveScript(string fileName, string content)
        {
            try
            {
                string scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");

                if (!Directory.Exists(scriptsDirectory))
                {
                    Directory.CreateDirectory(scriptsDirectory);
                    Logger.Log($"Dossier Scripts créé : {scriptsDirectory}");
                }

                string filePath = Path.Combine(scriptsDirectory, fileName);
                File.WriteAllText(filePath, content);

                Logger.Log($"✅ Script généré : {fileName} ({content.Length} caractères)");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Erreur lors de la sauvegarde du script {fileName} : {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}