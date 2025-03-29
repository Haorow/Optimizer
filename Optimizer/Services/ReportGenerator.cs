using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static Optimizer.Interface;

namespace Optimizer
{
    public static class ReportGenerator
    {
        /// <summary>
        /// Génère un rapport des données récoltées dans l'application.
        /// </summary>
        /// <param name="personnages">La liste des personnages à inclure dans le rapport.</param>
        /// <param name="mcGlobalStatus">État global de Mouse Clone.</param>
        /// <param name="mcShortcut">Raccourci de Mouse Clone.</param>
        /// <param name="mcDelays">État des délais de Mouse Clone.</param>
        /// <param name="mcMinDelay">Délai minimum de Mouse Clone.</param>
        /// <param name="mcMaxDelay">Délai maximum de Mouse Clone.</param>
        /// <param name="mcCursorPos">Position du curseur de Mouse Clone.</param>
        /// <param name="hcGlobalStatus">État global de Hotkey Clone.</param>
        /// <param name="hcShortcut">Raccourci de Hotkey Clone.</param>
        /// <param name="hcDelays">État des délais de Hotkey Clone.</param>
        /// <param name="hcMinDelay">Délai minimum de Hotkey Clone.</param>
        /// <param name="hcMaxDelay">Délai maximum de Hotkey Clone.</param>
        /// <param name="wsGlobalStatus">État global de Window Switcher.</param>
        /// <param name="wsShortcut">Raccourci de Window Switcher.</param>
        /// <param name="etGlobalStatus">État global de Easy Team.</param>
        /// <param name="etLeader">Leader sélectionné pour Easy Team.</param>
        /// <param name="etTchatPos">Position du tchat pour Easy Team.</param>

        public static void GenerateDataReport(
    ObservableCollection<Personnage> personnages,
    bool mcGlobalStatus,
    string mcShortcut,
    bool mcDelays,
    string mcMinDelay,
    string mcMaxDelay,
    object mcCursorPos,
    bool hcGlobalStatus,
    string hcShortcut,
    bool hcDelays,
    string hcMinDelay,
    string hcMaxDelay,
    bool wsGlobalStatus,
    string wsShortcut,
    bool etGlobalStatus,
    object etLeader,
    string etTchatPos)
        {
            try
            {
                var report = new StringBuilder();

                // Titre du rapport
                report.AppendLine("Rapport des données récoltées dans l'application Optimizer");
                report.AppendLine("-----");

                // Liste des fenêtres
                report.AppendLine("Liste des fenêtres :");
                foreach (var personnage in personnages)
                {
                    report.AppendLine($"Window : \"{personnage.WindowName}\", \"{personnage.CharacterName}\", Handle: {personnage.Handle}, Order: {personnage.Order}, " +
                                      $"MC: {(personnage.MouseClone ? "Enabled" : "Disabled")}, " +
                                      $"HC: {(personnage.HotkeyClone ? "Enabled" : "Disabled")}, " +
                                      $"WS: {(personnage.WindowSwitcher ? "Enabled" : "Disabled")}, " +
                                      $"ET: {(personnage.EasyTeam ? "Enabled" : "Disabled")}");
                }

                report.AppendLine("-----");

                // Paramètres globaux : Mouse Clone
                report.AppendLine("Paramètres globaux :");
                report.AppendLine("Mouse Clone");
                report.AppendLine($"MC : {(mcGlobalStatus ? "Enabled" : "Disabled")}");
                report.AppendLine($"MC Shortcut : {mcShortcut}");
                report.AppendLine($"MC Delays : {(mcDelays ? "Checked" : "Unchecked")}");
                report.AppendLine($"MC Min Delay : {mcMinDelay.Replace("ms", "")}");
                report.AppendLine($"MC Max Delay : {mcMaxDelay.Replace("ms", "")}");

                // Récupérer la position du curseur
                var cursorPos = mcCursorPos as ComboBoxItem;
                report.AppendLine($"MC Cursor Pos : {cursorPos?.Content?.ToString() ?? "Non défini"}");

                // Paramètres globaux : Hotkey Clone
                report.AppendLine("Hotkey Clone");
                report.AppendLine($"HC : {(hcGlobalStatus ? "Enabled" : "Disabled")}");
                report.AppendLine($"HC Shortcut : {hcShortcut}");
                report.AppendLine($"HC Delays : {(hcDelays ? "Checked" : "Unchecked")}");
                report.AppendLine($"HC Min Delay : {hcMinDelay.Replace("ms", "")}");
                report.AppendLine($"HC Max Delay : {hcMaxDelay.Replace("ms", "")}");

                // Paramètres globaux : Window Switcher
                report.AppendLine("Window Switcher");
                report.AppendLine($"WS : {(wsGlobalStatus ? "Enabled" : "Disabled")}");
                report.AppendLine($"WS Shortcut : {wsShortcut}");

                // Paramètres globaux : Easy Team
                report.AppendLine("Easy Team");
                report.AppendLine($"ET : {(etGlobalStatus ? "Enabled" : "Disabled")}");

                // Récupérer le leader sélectionné
                var leaderOption = etLeader as Interface.LeaderOption;
                report.AppendLine($"ET Leader : {leaderOption?.DisplayName ?? "Non défini"}");

                report.AppendLine($"ET Tchat Pos : {etTchatPos}");

                // Sauvegarder le rapport dans un fichier texte
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataReport.txt");
                File.WriteAllText(filePath, report.ToString());

                // Informer l'utilisateur
                System.Windows.MessageBox.Show($"Rapport généré : {filePath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du rapport : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}