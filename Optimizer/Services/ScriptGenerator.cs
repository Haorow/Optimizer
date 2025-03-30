using System;
using System.IO;
using System.Text;
using System.Windows;

namespace Optimizer
{
    public static class ScriptGenerator
    {
        /*public static void GenerateMouseCloneScript(AhkData ahkData)
        {
            try
            {
                var script = new StringBuilder();

                // En-tête du script
                script.AppendLine(";#NoTrayIcon");
                script.AppendLine("");

                if (ahkData.MouseCloneEnabled)
                {
                    script.AppendLine("; --- Mouse Clone ---");
                    script.AppendLine($"{ahkData.MouseCloneShortcut}::");
                    script.AppendLine("{");
                    script.AppendLine($"    KeyWait(\"{ahkData.MouseCloneShortcut}\") ; Attendre que la touche soit relâchée avant d'accepter une nouvelle pression");
                    script.AppendLine("");
                    script.AppendLine("    ; Définition des délais en millisecondes");
                    script.AppendLine($"    DelayMin := {ahkData.MouseCloneMinDelay}");
                    script.AppendLine($"    DelayMax := {ahkData.MouseCloneMaxDelay}");
                    script.AppendLine("");

                    // Filtrer les fenêtres avec TglBtn_MC_WindowStatus activé
                    var activeWindows = ahkData.WindowTitles
                        .Where(title => ahkData.ActiveWindows.Contains(title))
                        .ToArray();

                    if (activeWindows.Length > 0)
                    {
                        script.AppendLine("    winTitles := [");
                        foreach (var title in activeWindows)
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
                        script.AppendLine("            ; Générer un délai aléatoire entre DelayMin et DelayMax");
                        script.AppendLine("            DelayRand := Random(DelayMin, DelayMax)");
                        script.AppendLine("            Sleep(DelayRand)");
                        script.AppendLine("        }");
                        script.AppendLine("    }");
                    }
                    else
                    {
                        script.AppendLine("    ; Aucune fenêtre active pour Mouse Clone");
                    }

                    script.AppendLine("}");
                    script.AppendLine("return");
                    script.AppendLine("");
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
        }*/

        public static void Generate_MC_Script()
        {
            // Contenu du script AHK temporaire
            string scriptContent =
@"; Script MC Temporaire

; Définir la fonction ShowMsgBox
ShowMsgBox(*) {
    MsgBox(""Ceci est un script Mouse Clone temporaire !"")
    ExitApp()
}

; Assigner F1 pour appeler ShowMsgBox
Hotkey(""F1"", ShowMsgBox)

; Boucle infinie pour maintenir le script actif
while true {
    Sleep(100)
}";

            // Chemin du fichier temporaire
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MC-Temp.ahk");

            // Écrire le contenu dans le fichier
            File.WriteAllText(scriptPath, scriptContent);

            // Informer l'utilisateur
            System.Windows.MessageBox.Show($"Script MC temporaire généré : {scriptPath}", "MC", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Generate_HC_Script()
        {
            // Contenu du script AHK temporaire
            string scriptContent =
@"; Script HC Temporaire

; Définir la fonction ShowMsgBox
ShowMsgBox(*) {
    MsgBox(""Ceci est un script Hotkey Clone temporaire !"")
    ExitApp()
}

; Assigner F2 pour appeler ShowMsgBox
Hotkey(""F2"", ShowMsgBox)

; Boucle infinie pour maintenir le script actif
while true {
    Sleep(100)
}";

            // Chemin du fichier temporaire
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HC-Temp.ahk");

            // Écrire le contenu dans le fichier
            File.WriteAllText(scriptPath, scriptContent);

            // Informer l'utilisateur
            System.Windows.MessageBox.Show($"Script HC temporaire généré : {scriptPath}", "HC", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Generate_WS_Script()
        {
            // Contenu du script AHK temporaire
            string scriptContent =
@"; Script MC Temporaire

; Définir la fonction ShowMsgBox
ShowMsgBox(*) {
    MsgBox(""Ceci est un script Window Switcher temporaire !"")
    ExitApp()
}

; Assigner F3 pour appeler ShowMsgBox
Hotkey(""F3"", ShowMsgBox)

; Boucle infinie pour maintenir le script actif
while true {
    Sleep(100)
}";

            // Chemin du fichier temporaire
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WS-Temp.ahk");

            // Écrire le contenu dans le fichier
            File.WriteAllText(scriptPath, scriptContent);

            // Informer l'utilisateur
            System.Windows.MessageBox.Show($"Script WS temporaire généré : {scriptPath}", "WS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Generate_ET_Script()
        {
            // Contenu du script AHK temporaire
            string scriptContent =
@"; Script ET Temporaire

; Définir la fonction ShowMsgBox
ShowMsgBox(*) {
    MsgBox(""Ceci est un script Easy Team temporaire !"")
    ExitApp()
}

; Assigner F4 pour appeler ShowMsgBox
Hotkey(""F4"", ShowMsgBox)

; Boucle infinie pour maintenir le script actif
while true {
    Sleep(100)
}";

            // Chemin du fichier temporaire
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ET-Temp.ahk");

            // Écrire le contenu dans le fichier
            File.WriteAllText(scriptPath, scriptContent);

            // Informer l'utilisateur
            System.Windows.MessageBox.Show($"Script ET temporaire généré : {scriptPath}", "ET", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}