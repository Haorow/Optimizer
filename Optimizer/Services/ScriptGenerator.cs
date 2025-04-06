using System;
using System.IO;
using System.Text;
using System.Windows;

namespace Optimizer
{
    public static class ScriptGenerator
    {
        public static void Generate_MC_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // En-tête du script
                script.AppendLine(";#NoTrayIcon");
                script.AppendLine("");

                if (ahkData.MouseCloneEnabled)
                {
                    // Filtrer les fenêtres avec TglBtn_MC_WindowStatus activé
                    var activeWindows_MC = ahkData.WindowTitles.Where(title => ahkData.ActiveWindows_MC.Contains(title)).ToArray();
                    if (activeWindows_MC.Length == 0)
                    {
                        // Script vide si aucune fenêtre n'est activée
                        script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");
                        script.AppendLine("; Aucune fenêtre active pour Mouse Clone");
                    }
                    else
                    {
                        script.AppendLine("; - Mouse Clone -");
                        script.AppendLine($"{ahkData.MouseCloneShortcut}::");
                        script.AppendLine("{");
                        script.AppendLine($"    KeyWait(\"{ahkData.MouseCloneShortcut}\") ; Attendre que la touche soit relâchée avant d'accepter une nouvelle pression");
                        script.AppendLine("");

                        // Gestion des délais
                        script.AppendLine("    ; Définition des délais en millisecondes");
                        if (ahkData.MouseCloneDelays)
                        {
                            script.AppendLine($"    DelayMin := {ahkData.MouseCloneMinDelay}");
                            script.AppendLine($"    DelayMax := {ahkData.MouseCloneMaxDelay}");
                        }
                        else
                        {
                            script.AppendLine("    DelayMin := 0");
                            script.AppendLine("    DelayMax := 0");
                        }

                        script.AppendLine("");

                        // Liste des fenêtres actives
                        script.AppendLine("    winTitles := [");
                        foreach (var title in activeWindows_MC)
                        {
                            script.AppendLine($"        \"{title}\",");
                        }
                        script.AppendLine("    ]");

                        script.AppendLine("");

                        script.AppendLine("    for title in winTitles");
                        script.AppendLine("    {");
                        script.AppendLine("        if WinExist(title)");
                        script.AppendLine("        {");

                        // Ajouter WinActivate si le layout est IndividualWindows
                        if (ahkData.MouseCloneLayout == "IndividualWindows")
                        {
                            script.AppendLine("            WinActivate(title) ; Mettre la fenêtre au premier plan");
                            script.AppendLine("            Sleep(50) ; Petit délai pour s'assurer que la fenêtre est bien active");
                        }

                        script.AppendLine("            ControlClick(, title,, \"left\")");
                        script.AppendLine("");

                        // Générer un délai aléatoire si les délais sont activés
                        script.AppendLine("            ; Générer un délai aléatoire si les délais sont activés");
                        if (ahkData.MouseCloneDelays)
                        {
                            script.AppendLine("            DelayRand := Random(DelayMin, DelayMax)");
                            script.AppendLine("            Sleep(DelayRand)");
                        }
                        else
                        {
                            script.AppendLine("            Sleep(0)");
                        }

                        script.AppendLine("        }");
                        script.AppendLine("    }");
                        script.AppendLine("}");
                        script.AppendLine("return");
                    }
                }
                else
                {
                    script.AppendLine("; Aucune fenêtre active pour Mouse Clone");
                }

                // Sauvegarder dans un fichier
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MouseClone.ahk");
                File.WriteAllText(filePath, script.ToString());

                // Informer l'utilisateur
                System.Windows.MessageBox.Show($"Script AHK généré : {filePath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du script AHK : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Generate_HC_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // En-tête du script
                script.AppendLine("#Requires AutoHotkey v2.0");
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");

                // Récupérer les fenêtres avec Hotkey Clone activé
                var activeWindows_HC = ahkData.WindowTitles
                    .Where(title => ahkData.ActiveWindows_HC.Contains(title))
                    .ToArray();

                if (activeWindows_HC.Length > 0)
                {
                    // Définir les fenêtres dynamiquement
                    for (int i = 0; i < activeWindows_HC.Length; i++)
                    {
                        script.AppendLine($"Window{i + 1} := \"{activeWindows_HC[i]}\"");
                    }

                    script.AppendLine("; =====================");
                    script.AppendLine("#SingleInstance Force");
                    script.AppendLine("#WinActivateForce");

                    // Variables globales
                    script.AppendLine("; Variables globales");
                    script.AppendLine("Toggle := false");
                    script.AppendLine("TextBuffer := \"\"");
                    script.AppendLine("ClickPos := []");
                    script.AppendLine("IH := InputHook(\"V\")");

                    // Capture des clics pendant l'enregistrement
                    script.AppendLine("~LButton:: {");
                    script.AppendLine("    global Toggle, ClickPos");
                    script.AppendLine("    if (Toggle && WinActive(Window1)) {");
                    script.AppendLine("        MouseGetPos &x, &y");
                    script.AppendLine("        WinGetPos &winX, &winY,,, \"A\"");
                    script.AppendLine("        ClickPos.Push({x: x - winX, y: y - winY})");
                    script.AppendLine("    }");
                    script.AppendLine("    return");
                    script.AppendLine("}");

                    // Raccourci unique pour tout gérer
                    script.AppendLine($"{ahkData.HotkeyCloneShortcut}:: {{");
                    script.AppendLine("    global Toggle, TextBuffer, ClickPos, IH");

                    script.AppendLine("    if (!Toggle) {");
                    script.AppendLine("        ; Démarrage de l'enregistrement");
                    script.AppendLine("        MsgBox(\"Enregistrement démarré dans \" Window1)");
                    script.AppendLine("        WinActivate(Window1)");
                    script.AppendLine("        Sleep(200) ; Attente pour stabilisation");
                    script.AppendLine("        WinWaitActive(Window1,, 2) ; Vérification supplémentaire");
                    script.AppendLine("        Toggle := true");
                    script.AppendLine("        TextBuffer := \"\"");
                    script.AppendLine("        ClickPos := []");
                    script.AppendLine("        IH.Start()");
                    script.AppendLine("    }");
                    script.AppendLine("    else {");
                    script.AppendLine("        ; Fin de l'enregistrement");
                    script.AppendLine("        Toggle := false");
                    script.AppendLine("        IH.Stop()");
                    script.AppendLine("        TextBuffer := IH.Input");
                    script.AppendLine("        MsgBox(\"Enregistrement terminé\")");

                    script.AppendLine("        ; Détection de la méthode à utiliser");
                    script.AppendLine("        Methode := (StrLen(TextBuffer) <= 1) ? \"Standard\" : \"Alternative\"");
                    script.AppendLine("        MsgBox(\"Méthode \" Methode \" appliquée.`nTexte enregistré : \" TextBuffer \"`nNombre de clics détectés : \" ClickPos.Length)");

                    // Reproduction sur toutes les fenêtres activées SAUF Window1
                    script.AppendLine("        for index, window in [" + string.Join(", ", activeWindows_HC.Skip(1).Select(w => $"\"{w}\"")) + "] {");
                    script.AppendLine("            try {");
                    script.AppendLine("                ; Générer un délai aléatoire si les délais sont activés");
                    if (ahkData.HotkeyCloneDelays)
                    {
                        script.AppendLine($"                DelayRand := Random({ahkData.HotkeyCloneMinDelay}, {ahkData.HotkeyCloneMaxDelay})");
                        script.AppendLine("                Sleep(DelayRand)");
                    }
                    else
                    {
                        script.AppendLine("                Sleep(0)");
                    }
                    script.AppendLine("                WinActivate(window)");
                    script.AppendLine("                Sleep(200)");
                    script.AppendLine("                WinWaitActive(window,, 2)");

                    script.AppendLine("                ; Reproduction des clics");
                    script.AppendLine("                if (ClickPos.Length > 0) {");
                    script.AppendLine("                    for pos in ClickPos {");
                    script.AppendLine("                        WinGetPos &winX, &winY,,, window");
                    script.AppendLine("                        MouseMove winX + pos.x, winY + pos.y, 0");
                    script.AppendLine("                        Click");
                    script.AppendLine("                        Sleep(300)");
                    script.AppendLine("                    }");
                    script.AppendLine("                }");

                    script.AppendLine("                ; Méthode STANDARD : envoi caractère par caractère");
                    script.AppendLine("                if (Methode = \"Standard\") {");
                    script.AppendLine("                    SetKeyDelay(50, 30)");
                    script.AppendLine("                    Loop Parse TextBuffer {");
                    script.AppendLine("                        if (!WinActive(window)) {");
                    script.AppendLine("                            WinActivate(window)");
                    script.AppendLine("                            Sleep(50)");
                    script.AppendLine("                        }");
                    script.AppendLine("                        SendInput(\"{Raw}\" A_LoopField)");
                    script.AppendLine("                        Sleep(50)");
                    script.AppendLine("                    }");
                    script.AppendLine("                }");
                    script.AppendLine("                ; Méthode ALTERNATIVE : copier-coller haute fidélité");
                    script.AppendLine("                else {");
                    script.AppendLine("                    SavedClip := ClipboardAll()");
                    script.AppendLine("                    A_Clipboard := \"\"");
                    script.AppendLine("                    A_Clipboard := TextBuffer");
                    script.AppendLine("                    ClipWait(2)");
                    script.AppendLine("                    SendInput(\"^v\")");
                    script.AppendLine("                    Sleep(100)");
                    script.AppendLine("                    A_Clipboard := SavedClip");
                    script.AppendLine("                }");

                    script.AppendLine("                MsgBox(\"Réplication terminée dans \" window)");
                    script.AppendLine("            }");
                    script.AppendLine("            catch {");
                    script.AppendLine("                MsgBox(\"Échec de la réplication dans \" window \"!\")");
                    script.AppendLine("            }");
                    script.AppendLine("        }");

                    script.AppendLine("        ; Remet la fenêtre source au premier plan");
                    script.AppendLine("        WinActivate(Window1)");
                    script.AppendLine("    }");
                    script.AppendLine("    return");
                    script.AppendLine("}");

                    script.AppendLine("; Empêcher la saisie hors de Window1 pendant l'enregistrement");
                    script.AppendLine("#HotIf Toggle && WinActive(Window1)");
                    script.AppendLine("~*::Return");
                    script.AppendLine("#HotIf");
                }
                else
                {
                    script.AppendLine("; Aucune fenêtre active pour Hotkey Clone");
                }

                // Sauvegarder dans un fichier
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HotkeyClone.ahk");
                File.WriteAllText(filePath, script.ToString());

                // Informer l'utilisateur
                System.Windows.MessageBox.Show($"Script AHK généré : {filePath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du script AHK : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Generate_WS_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // En-tête du script
                script.AppendLine("#SingleInstance Force");
                script.AppendLine("; === CONFIGURATION DYNAMIQUE ===");

                // Récupérer les fenêtres avec Window Switcher activé
                var activeWindows_WS = ahkData.WindowTitles
                    .Where(title => ahkData.ActiveWindows_WS.Contains(title))
                    .ToArray();

                if (activeWindows_WS.Length == 0)
                {
                    // Script vide si aucune fenêtre n'est activée
                    script.AppendLine("; Aucune fenêtre active pour Window Switcher");
                }
                else
                {
                    // Définir les fenêtres dynamiquement
                    for (int i = 0; i < activeWindows_WS.Length; i++)
                    {
                        script.AppendLine($"Window{i + 1} := \"{activeWindows_WS[i]}\"");
                    }

                    // Ajouter le raccourci et la logique de basculement entre fenêtres
                    script.AppendLine("");
                    script.AppendLine($"{ahkData.WindowSwitcherShortcut}::");
                    script.AppendLine("{");

                    // Logique de basculement entre les fenêtres
                    script.AppendLine("    ; Vérifier quelle fenêtre est active et basculer vers la suivante");
                    for (int i = 0; i < activeWindows_WS.Length; i++)
                    {
                        string currentWindow = $"Window{i + 1}";
                        string nextWindow = i < activeWindows_WS.Length - 1 ? $"Window{i + 2}" : "Window1";

                        script.AppendLine($"    if WinActive({currentWindow})");
                        script.AppendLine("    {");
                        script.AppendLine($"        if WinExist({nextWindow})");
                        script.AppendLine($"            WinActivate({nextWindow})");
                        script.AppendLine("        return");
                        script.AppendLine("    }");
                    }

                    // Si aucune fenêtre active, activer la première fenêtre disponible
                    script.AppendLine("    ; Si aucune fenêtre active, activer la première fenêtre");
                    script.AppendLine("    if WinExist(Window1)");
                    script.AppendLine("        WinActivate(Window1)");
                    script.AppendLine("    else if WinExist(Window2)");
                    script.AppendLine("        WinActivate(Window2)");

                    script.AppendLine("}");
                    script.AppendLine("return");
                }

                // Sauvegarder dans un fichier
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindowSwitcher.ahk");
                File.WriteAllText(filePath, script.ToString());

                // Informer l'utilisateur
                System.Windows.MessageBox.Show($"Script AHK généré : {filePath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du script AHK : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Generate_ET_Script(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // En-tête du script
                script.AppendLine("#Requires AutoHotkey v2.0");
                script.AppendLine("; === CONFIGURATION ===");
                script.AppendLine($"WindowTitle := \"{ahkData.EasyTeamLeaderWindow}\"");

                script.AppendLine("#SingleInstance Force");
                script.AppendLine("#WinActivateForce");

                // Activer la fenêtre cible
                script.AppendLine("; Active la fenêtre cible et attend qu'elle soit bien au premier plan");
                script.AppendLine("if !WinExist(WindowTitle) {");
                script.AppendLine("    MsgBox(\"La fenêtre cible n'existe pas !\", \"Erreur\", 16) ; 16 correspond à l'icône d'erreur");
                script.AppendLine("    ExitApp");
                script.AppendLine("}");
                script.AppendLine("WinActivate(WindowTitle)");
                script.AppendLine("Sleep(300)");
                script.AppendLine("WinWaitActive(WindowTitle,, 2)");

                // Simuler l'appui sur la touche TAB
                script.AppendLine("; Simule l'appui sur la touche TAB");
                script.AppendLine("SendInput(\"{Tab}\")");
                script.AppendLine("Sleep(300)");

                // Inviter les personnages (sauf le leader)
                foreach (var window in ahkData.ActiveWindows_ET.Where(w => w != ahkData.EasyTeamLeaderWindow))
                {
                    // Extraire le nom du personnage depuis le nom de la fenêtre
                    string characterName = ExtractCharacterName(window);

                    script.AppendLine($"; Invite le personnage : {characterName}");
                    script.AppendLine($"SendInput(\"/invite {characterName}\")");
                    script.AppendLine("Sleep(200) ; Délai pour éviter un envoi trop rapide");
                    script.AppendLine("SendInput(\"{Enter}\")");
                    script.AppendLine("Sleep(300) ; Délai après la touche Entrée");
                }

                script.AppendLine("Sleep(200)");
                script.AppendLine("MsgBox(\"Action terminée avec succès !\", \"Succès\")");
                script.AppendLine("ExitApp");

                // Sauvegarder dans un fichier
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EasyTeam.ahk");
                File.WriteAllText(filePath, script.ToString());

                // Informer l'utilisateur
                System.Windows.MessageBox.Show($"Script AHK généré : {filePath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du script AHK : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Méthode pour extraire le nom du personnage depuis le nom de la fenêtre
        private static string ExtractCharacterName(string windowName)
        {
            if (string.IsNullOrEmpty(windowName))
                return string.Empty;

            // Supposons que le nom du personnage est toujours avant le premier tiret ("-")
            int firstDashIndex = windowName.IndexOf('-');
            if (firstDashIndex > 0)
            {
                return windowName.Substring(0, firstDashIndex).Trim();
            }

            // Si aucun tiret n'est trouvé, retourner le nom complet de la fenêtre
            return windowName.Trim();
        }
    }
}