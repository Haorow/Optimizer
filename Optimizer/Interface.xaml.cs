using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

public static class WindowHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static List<(IntPtr Handle, string Title)> GetWindows()
    {
        var windows = new List<(IntPtr Handle, string Title)>();

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);

                if (!string.IsNullOrEmpty(title.ToString()))
                {
                    windows.Add((hWnd, title.ToString()));
                }
            }
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}

namespace Optimizer
{
    public partial class Interface : Window
    {
        // DÉCLARATIONS
        private Dictionary<System.Windows.Controls.Button, bool> _waitingButtons = new Dictionary<System.Windows.Controls.Button, bool>();
        private DispatcherTimer _errorTimer;
        private System.Windows.Controls.Button _currentErrorButton;
        private Dictionary<System.Windows.Controls.TextBox, System.Windows.Controls.TextBox> _minMaxPairs;
        private TaskbarIcon taskbarIcon;

        // Ajoute cette classe imbriquée
        public class LeaderOption
        {
            public string DisplayName { get; set; }
            public Personnage Value { get; set; }
        }

        // METHODE : CHARGEMENT DE LA FENETRE ET DE LA LISTE
        public Interface()
        {
            InitializeComponent();
            this.DataContext = new CharactersViewModel();

            // Centrer la fenêtre au démarrage
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Ajuster la position après le chargement de la fenêtre
            this.Loaded += (s, e) =>
            {
                // Décalage vers le haut pour centrer l'application lorsque le menu Paramètres est déplié
                double offsetY = 145;
                this.Top -= offsetY;
            };

            // Obtenir le chemin absolu de l'icône par rapport au répertoire de l'exécutable
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "optimizer_icon.ico");

            // Vérifier si le fichier existe
            if (!File.Exists(iconPath))
            {
                throw new FileNotFoundException($"Le fichier d'icône n'a pas été trouvé : {iconPath}");
            }

            // Initialiser l'icône de la barre des tâches
            taskbarIcon = new TaskbarIcon
            {
                Icon = new Icon(iconPath),
                ToolTipText = "Optimizer",
                Visibility = Visibility.Collapsed
            };

            // Initialiser le menu contextuel personnalisé
            InitializeContextMenu();

            // Gérer le double-clic sur l'icône pour restaurer la fenêtre
            taskbarIcon.TrayMouseDoubleClick += (s, e) => RestoreWindow();

            // Initialiser les paires Min/Max
            _minMaxPairs = new Dictionary<System.Windows.Controls.TextBox, System.Windows.Controls.TextBox>
            {
                { TxtBox_MC_MinDelay, TxtBox_MC_MaxDelay },
                { TxtBox_HC_MinDelay, TxtBox_HC_MaxDelay },
                { TxtBox_MC_MaxDelay, TxtBox_MC_MinDelay },
                { TxtBox_HC_MaxDelay, TxtBox_HC_MinDelay }
            };

            // Définir le temps d'affichage du message "Raccourci déjà utilisé !"
            _errorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _errorTimer.Tick += (s, e) =>
            {
                if (_currentErrorButton != null)
                {
                    _currentErrorButton.Content = "Définir un raccourci...";
                    _currentErrorButton = null;
                    _errorTimer.Stop();
                }
            };

            // Forcer la désactivation initiale du bouton de position du tchat
            Btn_ET_TchatPos.IsEnabled = false;
        }

        // TRAYICON (BOUTON MASQUER) : GESTION DU MENU CONTEXTUEL
        private void InitializeContextMenu()
        {
            // Créer un nouveau ContextMenu
            var contextMenu = new ContextMenu();

            // Appliquer le style personnalisé pour le ContextMenu
            contextMenu.Style = (Style)this.FindResource(typeof(ContextMenu));

            // Option "Afficher"
            var showMenuItem = new MenuItem { Header = "Afficher" };
            showMenuItem.Style = (Style)this.FindResource(typeof(MenuItem)); // Appliquer le style personnalisé
            showMenuItem.Click += (s, e) => RestoreWindow();
            contextMenu.Items.Add(showMenuItem);

            // Option "Fermer"
            var exitMenuItem = new MenuItem { Header = "Fermer" };
            exitMenuItem.Style = (Style)this.FindResource(typeof(MenuItem)); // Appliquer le style personnalisé
            exitMenuItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            contextMenu.Items.Add(exitMenuItem);

            // Associer le menu contextuel à l'icône de la barre des tâches
            taskbarIcon.ContextMenu = contextMenu;
        }

        // TRAYICON (BOUTON MASQUER) : AFFICHER LA FENÊTRE
        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            taskbarIcon.Visibility = Visibility.Collapsed;
        }

        // BARRE DE TITRE : DEPLACEMENT DE LA FENETRE
        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        // BOUTON MASQUER : MASQUER L'APPLICATION DANS LA BARRE DES TACHES
        private void Btn_Hide_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            taskbarIcon.Visibility = Visibility.Visible;
        }

        // BOUTON REDUIRE : MINIMISER L'APPLICATION
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // BOUTON FERMER : FERMER L'APPLICATION
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // BOUTON PARAMETRES : DEPLOIEMENT DU PANNEAU DE CONFIGURATION
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Agrandir la fenêtre à 680 quand le bouton est coché
            this.Height = 680;
        }

        // BOUTON PARAMETRES : REPLI DU PANNEAU DE CONFIGURATION
        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // Réduire la fenêtre à 390 quand le bouton est décoché
            this.Height = 390;
        }

        // DATAGRID : GESTION ET AFFICHAGE DES DONNEES
        public class CharactersViewModel : INotifyPropertyChanged
        {
            // Collection observable des personnages (liée à la DataGrid)
            public ObservableCollection<Personnage> Personnages { get; set; }

            // Option par défaut pour la ComboBox des leaders
            private static readonly LeaderOption _defaultLeaderOption = new LeaderOption
            {
                DisplayName = "Définir un chef d'équipe",
                Value = null
            };

            // Initialise les personnages (chargé ou vide)
            public CharactersViewModel()
            {
                Personnages = new ObservableCollection<Personnage>();
                LoadWindows();
            }

            // Événement de notification de changement de propriété
            public event PropertyChangedEventHandler PropertyChanged;

            // Méthode utilitaire pour déclencher les notifications
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            // Méthode principale de chargement des fenêtres
            public void LoadWindows()
            {
                // Sauvegarde de l'identifiant unique
                string previousLeaderName = SelectedLeader?.CharacterName;

                // Sauvegarder la sélection actuelle
                bool wasDefaultSelected = SelectedLeader == null;

                // 1. Sauvegarder la sélection actuelle
                string selectedLeaderName = SelectedLeader?.CharacterName;

                var windows = WindowHelper.GetWindows();
                var filteredWindows = windows.Where(w => w.Title.Contains("- Release")).ToList();

                // Mettre à jour les fenêtres existantes ou en ajouter de nouvelles
                foreach (var window in filteredWindows)
                {
                    var characterName = ExtractCharacterName(window.Title);
                    var existingPersonnage = Personnages.FirstOrDefault(p => p.CharacterName == characterName);

                    if (existingPersonnage == null)
                    {
                        // Ajouter un nouveau personnage si non existant
                        Personnages.Add(new Personnage
                        {
                            Handle = window.Handle,
                            WindowName = window.Title,
                            CharacterName = characterName,
                            MouseClone = false,
                            HotkeyClone = false,
                            WindowSwitcher = false,
                            EasyTeam = false
                        });
                    }
                    else
                    {
                        // Mettre à jour les propriétés nécessaires pour les fenêtres existantes
                        existingPersonnage.WindowName = window.Title;
                        existingPersonnage.Handle = window.Handle;
                    }
                }

                // Supprimer les personnages qui ne sont plus dans les fenêtres actuelles
                var characterNames = filteredWindows.Select(w => ExtractCharacterName(w.Title)).ToHashSet();
                for (int i = Personnages.Count - 1; i >= 0; i--)
                {
                    if (!characterNames.Contains(Personnages[i].CharacterName))
                    {
                        Personnages.RemoveAt(i);
                    }
                }

                // Restauration de la sélection du Leader précédent
                if (!string.IsNullOrEmpty(previousLeaderName))
                {
                    SelectedLeader = Personnages.FirstOrDefault(p => p.CharacterName == previousLeaderName);
                }

                // Mise à jour des ordres
                UpdateOrder();
                // Mise à jour des options de leader
                UpdateLeaderOptions();

                if (wasDefaultSelected)
                {
                    SelectedLeader = null; // Force la sélection sur l'option par défaut
                }
                else if (!string.IsNullOrEmpty(selectedLeaderName))
                {
                    SelectedLeader = Personnages.FirstOrDefault(p => p.CharacterName == selectedLeaderName);
                }
            }

            // Récupération du nom du personnage
            private string ExtractCharacterName(string windowName)
            {
                // Extraire le texte avant le premier espace
                var parts = windowName.Split(' ');
                return parts.Length > 0 ? parts[0] : windowName;
            }

            // Déplacement vers le haut du personnage dans la liste
            public void MoveUp(Personnage personnage)
            {
                int index = Personnages.IndexOf(personnage);
                if (index > 0)
                {
                    Personnages.Move(index, index - 1);
                    UpdateOrder();
                }
            }

            // Déplacement vers le bas du personnage dans la liste
            public void MoveDown(Personnage personnage)
            {
                int index = Personnages.IndexOf(personnage);
                if (index < Personnages.Count - 1)
                {
                    Personnages.Move(index, index + 1);
                    UpdateOrder();
                }
            }

            // Mise à jour de l'ordre des personnages dans la liste
            private void UpdateOrder()
            {
                int order = 1;
                foreach (var personnage in Personnages)
                {
                    personnage.Order = order++;
                }
            }

            // Gestion du Leader
            private ObservableCollection<LeaderOption> _leaderOptions = new ObservableCollection<LeaderOption>();
            public ObservableCollection<LeaderOption> LeaderOptions
            {
                get => _leaderOptions;
                set
                {
                    _leaderOptions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeaderOptions)));
                }
            }

            // Sélection du Leader
            private Personnage _selectedLeader;
            public Personnage SelectedLeader
            {
                get => _selectedLeader;
                set
                {
                    _selectedLeader = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeaderOptions)); // Force la mise à jour de la ComboBox
                }
            }

            // Mise à jour des options du Leader
            private void UpdateLeaderOptions()
            {
                var newOptions = new ObservableCollection<LeaderOption> { _defaultLeaderOption };

                foreach (var personnage in Personnages)
                {
                    newOptions.Add(new LeaderOption
                    {
                        DisplayName = personnage.CharacterName,
                        Value = personnage
                    });
                }

                LeaderOptions = newOptions;
            }
        }

        // BOUTON DE DEPLACEMENT VERS LE HAUT : DEPLACER VERS LE HAUT
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Personnage personnage)
            {
                // Récupérer le ViewModel
                if (this.DataContext is Interface.CharactersViewModel viewModel)
                {
                    viewModel.MoveUp(personnage);
                }
            }
        }

        // BOUTON DE DEPLACEMENT VERS LE BAS : DEPLACER VERS LE BAS
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Personnage personnage)
            {
                // Récupérer le ViewModel
                if (this.DataContext is Interface.CharactersViewModel viewModel)
                {
                    viewModel.MoveDown(personnage);
                }
            }
        }

        // BOUTON RAFRAICHIR : RAFRAICHIR LA LISTE
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer le ViewModel actuel
                if (this.DataContext is Interface.CharactersViewModel viewModel)
                {
                    // Rafraîchir les données sans recréer complètement la collection
                    viewModel.LoadWindows();

                    // Collecter les données
                    //var rawData = CollectData();

                    // Appeler la méthode de génération de rapport avec les données nécessaires
                    /*ReportGenerator.GenerateDataReport(
                        rawData["Personnages"] as ObservableCollection<Personnage>,
                        (bool)rawData["MC_GlobalStatus"],
                        rawData["MC_Shortcut"].ToString(),
                        (bool)rawData["MC_Delays"],
                        rawData["MC_MinDelay"].ToString(),
                        rawData["MC_MaxDelay"].ToString(),
                        rawData["MC_Layout"],
                        (bool)rawData["HC_GlobalStatus"],
                        rawData["HC_Shortcut"].ToString(),
                        (bool)rawData["HC_Delays"],
                        rawData["HC_MinDelay"].ToString(),
                        rawData["HC_MaxDelay"].ToString(),
                        (bool)rawData["WS_GlobalStatus"],
                        rawData["WS_Shortcut"].ToString(),
                        (bool)rawData["ET_GlobalStatus"],
                        rawData["ET_Leader"],
                        rawData["ET_TchatPos"].ToString()
                    );*/
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du rafraîchissement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // TEXTBOX : PERTE DU FOCUS DES ELEMENTS INTERACTIFS
        private void ClearFocus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Transfère le focus vers le conteneur parent
            Keyboard.ClearFocus();
        }

        // BOUTONS DE RACCOURCIS : VERIFICATION DE LA DISPONIBILITE DES RACCOURCIS DETECTES
        private bool IsShortcutAlreadyUsed(System.Windows.Controls.Button currentButton, string shortcut)
        {
            return (Btn_MC_Shortcut != currentButton && Btn_MC_Shortcut.Content.ToString() == shortcut) ||
                   (Btn_HC_Shortcut != currentButton && Btn_HC_Shortcut.Content.ToString() == shortcut) ||
                   (Btn_WS_Shortcut != currentButton && Btn_WS_Shortcut.Content.ToString() == shortcut);
        }

        // BOUTONS DE RACCOURCIS : DETECTION DES TOUCHES DE CONTROLE
        private bool IsModifierKey(System.Windows.Input.Key key)
        {
            return key == System.Windows.Input.Key.LeftCtrl ||
                   key == System.Windows.Input.Key.RightCtrl ||
                   key == System.Windows.Input.Key.LeftShift ||
                   key == System.Windows.Input.Key.RightShift ||
                   key == System.Windows.Input.Key.LeftAlt ||
                   key == System.Windows.Input.Key.RightAlt;
        }

        // BOUTONS DE RACCOURCIS : CONVERSION DES BOUTONS DE SOURIS
        private string TranslateMouseButton(System.Windows.Input.MouseButton button)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left: return "Bouton gauche";
                case System.Windows.Input.MouseButton.Right: return "Bouton droit";
                case System.Windows.Input.MouseButton.Middle: return "Bouton du milieu";
                case System.Windows.Input.MouseButton.XButton1: return "Bouton latéral 1";
                case System.Windows.Input.MouseButton.XButton2: return "Bouton latéral 2";
                default: return "Bouton inconnu";
            }
        }

        // BOUTONS DE RACCOURCIS : GESTION DES COMBINAISONS DE TOUCHES
        private string BuildShortcutString(System.Windows.Input.Key mainKey)
        {
            var modifiers = new List<string>();

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)) modifiers.Add("Ctrl");
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift)) modifiers.Add("Shift");
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt)) modifiers.Add("Alt");

            return string.Join("+", modifiers.Concat(new[] { mainKey.ToString() }));
        }

        // BOUTONS DE RACCOURCIS : DEFINITION DU RACCOURCI POUR TOUS LES BOUTONS
        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            button.Content = "Définir un raccourci...";
            _waitingButtons[button] = true;

            this.PreviewKeyUp += Window_PreviewKeyUp;
            this.PreviewMouseUp += Window_PreviewMouseUp;
        }

        // BOUTONS DE RACCOURCIS : DÉTECTION DES TOUCHES
        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            foreach (var entry in _waitingButtons.Where(b => b.Value).ToList())
            {
                var button = entry.Key;

                if (IsModifierKey(e.Key)) return;

                var shortcut = BuildShortcutString(e.Key);

                // Si le bouton est en état d'erreur
                if (_currentErrorButton == button)
                {
                    _errorTimer.Stop();
                    _currentErrorButton = null;
                }

                // Si le raccourci est déjà utilisé
                if (IsShortcutAlreadyUsed(button, shortcut))
                {
                    button.Content = "Raccourci déjà utilisé !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                button.Content = shortcut;
                CleanupForButton(button);
                e.Handled = true;
            }
        }

        // BOUTONS DE RACCOURCIS : DÉTECTION DES BOUTONS DE SOURIS
        private void Window_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach (var entry in _waitingButtons.Where(b => b.Value).ToList())
            {
                var button = entry.Key;
                var mouseText = TranslateMouseButton(e.ChangedButton);

                // Si le bouton est en état d'erreur
                if (_currentErrorButton == button)
                {
                    _errorTimer.Stop();
                    _currentErrorButton = null;
                }

                // Si le raccourci est déjà utilisé
                if (IsShortcutAlreadyUsed(button, mouseText))
                {
                    button.Content = "Raccourci déjà utilisé !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                button.Content = mouseText;
                CleanupForButton(button);
                e.Handled = true;
            }
        }

        // BOUTONS DE RACCOURCIS : MISE A JOUR DU CONTENU
        private void CleanupForButton(System.Windows.Controls.Button button)
        {
            _waitingButtons[button] = false;

            // Annuler le timer si le bouton était en erreur
            if (_currentErrorButton == button)
            {
                _errorTimer.Stop();
                _currentErrorButton = null;
            }

            if (!_waitingButtons.Any(b => b.Value))
            {
                this.PreviewKeyUp -= Window_PreviewKeyUp;
                this.PreviewMouseUp -= Window_PreviewMouseUp;
            }
        }

        // TEXTBOX : VERIFICATION DES CARACTERES NUMERIQUES
        private void NumberValidation_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Autorise uniquement les chiffres et le backspace
            e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);
        }

        // TEXTBOX : CONFIRMATION DES CARACTERES NUMERIQUES
        private void NumberValidation_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // TEXTBOX : EFFACEMENT DU CONTENU AU CLIC
        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (System.Windows.Controls.TextBox)sender;
            textBox.Focus();

            if (textBox.Text.EndsWith("ms"))
            {
                textBox.Text = "";
            }
            else
            {
                // Sélectionner tout le texte existant
                textBox.SelectAll();
            }

            e.Handled = true;
        }

        // TEXTBOX : VALIDATION PAR PERTE DU FOCUS
        private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = (System.Windows.Controls.TextBox)sender;
            var defaultValues = new Dictionary<System.Windows.Controls.TextBox, string>
            {
                { TxtBox_MC_MinDelay, "50" },
                { TxtBox_MC_MaxDelay, "125" },
                { TxtBox_HC_MinDelay, "50" },
                { TxtBox_HC_MaxDelay, "125" }
            };

            // Validation de base
            int value;
            if (!int.TryParse(textBox.Text, out value))
            {
                value = int.Parse(defaultValues[textBox]);
            }
            value = Math.Clamp(value, 0, 9999);
            textBox.Text = $"{value}ms";

            // Gestion Min/Max
            if (_minMaxPairs.TryGetValue(textBox, out var pairedTextBox))
            {
                bool isMin = textBox == TxtBox_MC_MinDelay || textBox == TxtBox_HC_MinDelay;
                int pairedValue = int.Parse(pairedTextBox.Text.Replace("ms", ""));

                if (isMin)
                {
                    // Si Min > Max, forcer Max = Min
                    if (value > pairedValue)
                    {
                        pairedValue = value;
                        pairedTextBox.Text = $"{Math.Clamp(pairedValue, 0, 9999)}ms";
                    }
                }
                else
                {
                    // Si Max < Min, forcer Min = Max
                    if (value < pairedValue)
                    {
                        pairedValue = value;
                        pairedTextBox.Text = $"{Math.Clamp(pairedValue, 0, 9999)}ms";
                    }
                }
            }
        }

        // TEXTBOX : VALIDATION PAR LA TOUCHE ENTREE
        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
            }
        }

        //COMBOBOX LEADER : DEFINI L'ETAT DU BOUTON DE POSITION DU TCHAT SELON LA SELECTION
        private void CboBox_ET_Leader_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboBox_ET_Leader.SelectedItem is LeaderOption selected)
            {
                // Activer le bouton uniquement si ce n'est pas l'option par défaut
                Btn_ET_TchatPos.IsEnabled = selected.Value != null;
            }
            else
            {
                Btn_ET_TchatPos.IsEnabled = false;
            }
        }


// === PARTIE GESTION DES SCRIPTS === //

        // Récupération des données
        private Dictionary<string, object> CollectData()
        {
            // Récupérer le ViewModel actuel
            if (this.DataContext is CharactersViewModel viewModel)
            {
                // Déterminer les valeurs de DelayMin et DelayMax
                int mcMinDelay = ChkBox_MC_Delays.IsChecked == true
                    ? int.Parse(TxtBox_MC_MinDelay.Text.Replace("ms", ""))
                    : 0;

                int mcMaxDelay = ChkBox_MC_Delays.IsChecked == true
                    ? int.Parse(TxtBox_MC_MaxDelay.Text.Replace("ms", ""))
                    : 0;

                return new Dictionary<string, object>
        {
            { "Personnages", viewModel.Personnages },
            { "MC_GlobalStatus", TglBtn_MC_GlobalStatus.IsChecked },
            { "MC_Shortcut", Btn_MC_Shortcut.Content.ToString() },
            { "MC_Delays", ChkBox_MC_Delays.IsChecked },
            { "MC_MinDelay", mcMinDelay }, // Valeur conditionnelle
            { "MC_MaxDelay", mcMaxDelay }, // Valeur conditionnelle
            { "MC_Layout", CboBox_MC_Layout.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content.ToString() : null },
            { "ActiveWindows", viewModel.Personnages != null && viewModel.Personnages.Any() ? viewModel.Personnages.Where(p => p.MouseClone).Select(p => p.WindowName).ToHashSet() : new HashSet<string>() },
            { "HC_GlobalStatus", TglBtn_HC_GlobalStatus.IsChecked },
            { "HC_Shortcut", Btn_HC_Shortcut.Content.ToString() },
            { "HC_Delays", ChkBox_HC_Delays.IsChecked },
            { "HC_MinDelay", TxtBox_HC_MinDelay.Text.Replace("ms", "") },
            { "HC_MaxDelay", TxtBox_HC_MaxDelay.Text.Replace("ms", "") },
            { "WS_GlobalStatus", TglBtn_WS_GlobalStatus.IsChecked },
            { "WS_Shortcut", Btn_WS_Shortcut.Content.ToString() },
            { "ET_GlobalStatus", TglBtn_ET_GlobalStatus.IsChecked },
            { "ET_Leader", CboBox_ET_Leader.SelectedItem?.ToString() },
            { "ET_TchatPos", Btn_ET_TchatPos.Content.ToString() }
        };
            }
            else
            {
                throw new InvalidOperationException("DataContext n'est pas une instance de CharactersViewModel.");
            }
        }

        private Process _mcProcess;
        private Process _hcProcess;
        private Process _wsProcess;
        private Process _etProcess;

        private void TglBtn_MC_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            ScriptGenerator.Generate_MC_Script();
            StartScript("MC", ref _mcProcess);
        }

        private void TglBtn_MC_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _mcProcess);
        }

        private void TglBtn_HC_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            ScriptGenerator.Generate_HC_Script();
            StartScript("HC", ref _hcProcess);
        }

        private void TglBtn_HC_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _hcProcess);
        }

        private void TglBtn_WS_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            ScriptGenerator.Generate_WS_Script();
            StartScript("WS", ref _wsProcess);
        }

        private void TglBtn_WS_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _wsProcess);
        }

        private void TglBtn_ET_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            ScriptGenerator.Generate_ET_Script();
            StartScript("ET", ref _etProcess);
        }

        private void TglBtn_ET_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _etProcess);
        }

        private void StartScript(string scriptName, ref Process process)
        {
            // Arrêter le script actuel s'il est déjà en cours d'exécution
            StopScript(ref process);

            // Chemin du fichier script
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{scriptName}-Temp.ahk");

            // Vérifier si le fichier script existe
            if (!File.Exists(scriptPath))
            {
                System.Windows.MessageBox.Show($"Le fichier {scriptPath} n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Chemin relatif vers AutoHotkey64.exe
            string ahkExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "AutoHotkey64.exe");

            // Vérifier si AutoHotkey64.exe existe
            if (!File.Exists(ahkExePath))
            {
                System.Windows.MessageBox.Show($"AutoHotkey64.exe n'a pas été trouvé à l'emplacement : {ahkExePath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Exécuter le script AutoHotkey
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ahkExePath,
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = Process.Start(startInfo);
                Console.WriteLine($"{scriptName} activé !");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'exécution d'AutoHotkey : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopScript(ref Process process)
        {
            // Arrêter le processus AHK
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.Dispose();
                process = null;
                Console.WriteLine("Script désactivé !");
            }
        }
    }
}