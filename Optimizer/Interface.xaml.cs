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
using System.Windows.Interop;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Optimizer.Services;

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
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseHook;
        private IntPtr _hookID = IntPtr.Zero;
        private bool _isWaitingForClick = false;

        // Ajoute cette classe imbriquée
        public class LeaderOption
        {
            public string DisplayName { get; set; } // Texte affiché dans la ComboBox
            public Personnage Value { get; set; }   // Objet Personnage associé

            public override string ToString()
            {
                return DisplayName; // Afficher le nom dans la ComboBox
            }
        }

        // METHODE : CHARGEMENT DE LA FENETRE ET DE LA LISTE
        public Interface()
        {
            InitializeComponent();
            InitializeGlobalStatusButtons();
            this.DataContext = new Optimizer.Services.CharactersViewModel();

            // Centrer la fenêtre au démarrage
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Ajuster la position après le chargement de la fenêtre
            this.Loaded += (s, e) =>
            {
                // Décalage vers le haut pour centrer l'application lorsque le menu Paramètres est déplié
                double offsetY = 130;
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
            StopScript(ref _mcProcess);
            StopScript(ref _hcProcess);
            StopScript(ref _wsProcess);
            StopScript(ref _etProcess);
            System.Windows.Application.Current.Shutdown();
        }

        // BOUTON PARAMETRES : DEPLOIEMENT DU PANNEAU DE CONFIGURATION
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Agrandir la fenêtre à 650 quand le bouton est coché
            this.Height = 650;
        }

        // BOUTON PARAMETRES : REPLI DU PANNEAU DE CONFIGURATION
        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // Réduire la fenêtre à 390 quand le bouton est décoché
            this.Height = 390;
        }

        // BOUTON DE DEPLACEMENT VERS LE HAUT : DEPLACER VERS LE HAUT
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Personnage personnage)
            {
                // Récupérer le ViewModel
                if (this.DataContext is Optimizer.Services.CharactersViewModel viewModel)
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
                if (this.DataContext is Optimizer.Services.CharactersViewModel viewModel)
                {
                    viewModel.MoveDown(personnage);
                }
            }
        }

        // TOGGLE BUTTONS _WINDOWSTATUS : VERIFIER LE NOMBRE DE FENETRES ACTIVES POUR UNE FONCTIONNALITE
        private int CountActiveWindowsForFeature(Func<Personnage, bool> featureSelector)
        {
            if (this.DataContext is CharactersViewModel viewModel && viewModel.Personnages != null)
            {
                return viewModel.Personnages.Count(p => featureSelector(p));
            }
            return 0;
        }

        // TOGGLE BUTTONS _GLOBALSTATUS : ACTIVATION/DESACTIVATION SELON LE NOMBRE DE TOGGLE BUTTONS _WINDOWSTATUS ACTIFS POUR UNE FONCTIONNALITE
        private void UpdateGlobalStatusButtons()
        {
            // Mouse Clone
            int activeMCWindows = CountActiveWindowsForFeature(p => p.MouseClone);
            TglBtn_MC_GlobalStatus.IsEnabled = activeMCWindows >= 2;
            if (activeMCWindows < 2 && TglBtn_MC_GlobalStatus.IsChecked == true)
            {
                TglBtn_MC_GlobalStatus.IsChecked = false; // Réinitialiser à Unchecked
            }

            // Hotkey Clone
            int activeHCWindows = CountActiveWindowsForFeature(p => p.HotkeyClone);
            TglBtn_HC_GlobalStatus.IsEnabled = activeHCWindows >= 2;
            if (activeHCWindows < 2 && TglBtn_HC_GlobalStatus.IsChecked == true)
            {
                TglBtn_HC_GlobalStatus.IsChecked = false; // Réinitialiser à Unchecked
            }

            // Window Switcher
            int activeWSWindows = CountActiveWindowsForFeature(p => p.WindowSwitcher);
            TglBtn_WS_GlobalStatus.IsEnabled = activeWSWindows >= 2;
            if (activeWSWindows < 2 && TglBtn_WS_GlobalStatus.IsChecked == true)
            {
                TglBtn_WS_GlobalStatus.IsChecked = false; // Réinitialiser à Unchecked
            }

            // Easy Team
            if (this.DataContext is CharactersViewModel viewModel)
            {
                // Compter le nombre de fenêtres actives (EasyTeam activé)
                int activeWindowsCount = viewModel.Personnages.Count(p => p.EasyTeam);

                // Activer CboBox_ET_Leader si au moins deux fenêtres sont actives
                CboBox_ET_Leader.IsEnabled = activeWindowsCount >= 2;

                // Activer TglBtn_ET_GlobalStatus uniquement si :
                // - Au moins deux fenêtres sont actives
                // - Une option valide est sélectionnée dans CboBox_ET_Leader
                TglBtn_ET_GlobalStatus.IsEnabled = activeWindowsCount >= 2 &&
                                                   CboBox_ET_Leader.SelectedItem is LeaderOption selectedOption &&
                                                   selectedOption.Value != null;

                // Désactiver TglBtn_ET_GlobalStatus si les conditions ne sont pas remplies
                if (activeWindowsCount < 2 ||
                    CboBox_ET_Leader.SelectedItem is LeaderOption selected && selected.Value == null)
                {
                    TglBtn_ET_GlobalStatus.IsChecked = false; // Réinitialiser à Unchecked
                }
            }
        }

        // TOGGLE BUTTONS _GLOBALSTATUS : INITIALISER L'ETAT DES TOGGLE BUTTONS _GLOBALSTATUS
        private void InitializeGlobalStatusButtons()
        {
            Btn_EasyTeam.IsEnabled = false;
            UpdateGlobalStatusButtons();
        }

        // TOGGLE BUTTON MC_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A L'ACTIVATION
        private void TglBtn_MC_WindowStatus_Checked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "MouseClone",
                ref _mcProcess,
                () => TglBtn_MC_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_MC_Script(ahkData);
                });
        }

        // TOGGLE BUTTON MC_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A LA DESACTIVATION
        private void TglBtn_MC_WindowStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "MouseClone",
                ref _mcProcess,
                () => TglBtn_MC_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_MC_Script(ahkData);
                });
        }

        // TOGGLE BUTTON HC_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A L'ACTIVATION
        private void TglBtn_HC_WindowStatus_Checked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "HotkeyClone",
                ref _hcProcess,
                () => TglBtn_HC_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_HC_Script(ahkData);
                });
        }

        // TOGGLE BUTTON HC_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A LA DESACTIVATION
        private void TglBtn_HC_WindowStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "HotkeyClone",
                ref _hcProcess,
                () => TglBtn_HC_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_HC_Script(ahkData);
                });
        }

        // TOGGLE BUTTON WS_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A L'ACTIVATION
        private void TglBtn_WS_WindowStatus_Checked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "WindowSwitcher",
                ref _wsProcess,
                () => TglBtn_WS_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_WS_Script(ahkData);
                });
        }

        // TOGGLE BUTTON WS_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A LA DESACTIVATION
        private void TglBtn_WS_WindowStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "WindowSwitcher",
                ref _wsProcess,
                () => TglBtn_WS_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_WS_Script(ahkData);
                });
        }

        // TOGGLE BUTTON ET_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A L'ACTIVATION
        private void TglBtn_ET_WindowStatus_Checked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();

            // Mettre à jour le script Easy Team si nécessaire (sans relancer le script)
            if (TglBtn_ET_GlobalStatus.IsChecked == true)
            {
                var rawData = CollectData();
                var ahkData = AhkConverter.ConvertToAhkData(rawData);
                ScriptGenerator.Generate_ET_Script(ahkData);
            }
        }

        // TOGGLE BUTTON ET_WINDOWSTATUS : MET A JOUR LA LISTE DES FENETRES ACTIVES POUR UNE FONCTIONNALITE A LA DESACTIVATION
        private void TglBtn_ET_WindowStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateGlobalStatusButtons();

            // Mettre à jour le script Easy Team si nécessaire (sans relancer le script)
            if (TglBtn_ET_GlobalStatus.IsChecked == true)
            {
                var rawData = CollectData();
                var ahkData = AhkConverter.ConvertToAhkData(rawData);
                ScriptGenerator.Generate_ET_Script(ahkData);
            }
        }

        // BOUTON RAFRAICHIR : RAFRAICHIR LA LISTE
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer le ViewModel actuel
                if (this.DataContext is Optimizer.Services.CharactersViewModel viewModel)
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

        // BOUTON EASY TEAM : EXECUTION DU SCRIPT
        private void BtnEasyTeam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Démarrer le script Easy Team
                StartScript("EasyTeam", ref _etProcess);

                // Informer l'utilisateur que le script a été exécuté
                System.Windows.MessageBox.Show("Le script Easy Team a été exécuté avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'exécution du script Easy Team : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Mettre à jour le script correspondant
                UpdateScriptIfNeededForButton(button);
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

                // Mettre à jour le script correspondant
                UpdateScriptIfNeededForButton(button);
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

        // CHECKBOX DELAIS : MISE A JOUR DU SCRIPT SI LA FONCTIONNALITE EST ACTIVE
        private void ChkBox_Delays_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            UpdateScriptIfNeededForDelaysCheckbox(checkBox);
        }

        // CHECKBOX DELAIS : MISE A JOUR DU SCRIPT SI LA FONCTIONNALITE EST ACTIVE
        private void ChkBox_Delays_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            UpdateScriptIfNeededForDelaysCheckbox(checkBox);
        }

        // TEXTBOX : PERTE DU FOCUS DES ELEMENTS INTERACTIFS
        private void ClearFocus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Transfère le focus vers le conteneur parent
            FocusManager.SetFocusedElement(this, UIInterface);
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
            value = Math.Clamp(value, 0, 999);
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
                        pairedTextBox.Text = $"{Math.Clamp(pairedValue, 0, 999)}ms";
                    }
                }
                else
                {
                    // Si Max < Min, forcer Min = Max
                    if (value < pairedValue)
                    {
                        pairedValue = value;
                        pairedTextBox.Text = $"{Math.Clamp(pairedValue, 0, 999)}ms";
                    }
                }
            }

            // Mettre à jour le script correspondant
            UpdateScriptIfNeededForTextBox(textBox);

            // Forcer la perte de focus
            FocusManager.SetFocusedElement(this, UIInterface);
        }

        // TEXTBOX : VALIDATION PAR LA TOUCHE ENTREE
        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (System.Windows.Controls.TextBox)sender;

                // Mettre à jour le script correspondant
                UpdateScriptIfNeededForTextBox(textBox);

                // Forcer la perte de focus pour déclencher la validation
                FocusManager.SetFocusedElement(this, UIInterface);
            }
        }

        // COMBOBOX LAYOUT : MISE A JOUR DU SCRIPT SI LA FONCTIONNALITE EST ACTIVE
        private void CboBox_MC_Layout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "MouseClone",
                ref _mcProcess,
                () => TglBtn_MC_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_MC_Script(ahkData);
                });
        }

        // COMBOBOX LEADER : DEFINI L'ETAT DU BOUTON DE POSITION DU TCHAT SELON LA SELECTION
        private void CboBox_ET_Leader_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboBox_ET_Leader.SelectedItem is LeaderOption selected)
            {
                // Désactiver TglBtn_ET_GlobalStatus si la valeur par défaut est sélectionnée
                if (selected.Value == null && TglBtn_ET_GlobalStatus.IsChecked == true)
                {
                    TglBtn_ET_GlobalStatus.IsChecked = false;
                }
            }
            else
            {
                // Désactiver TglBtn_ET_GlobalStatus si aucune sélection valide n'est faite
                if (TglBtn_ET_GlobalStatus.IsChecked == true)
                {
                    TglBtn_ET_GlobalStatus.IsChecked = false;
                }
            }
            // Mettre à jour l'état des boutons
            UpdateGlobalStatusButtons();

            // Mettre à jour le script Mouse Clone si nécessaire
            UpdateScriptIfNeeded(
                "EasyTeam",
                ref _etProcess,
                () => TglBtn_ET_GlobalStatus.IsChecked == true,
                () =>
                {
                    var rawData = CollectData();
                    var ahkData = AhkConverter.ConvertToAhkData(rawData);
                    ScriptGenerator.Generate_ET_Script(ahkData);
                });
        }



        // === PARTIE GESTION DES SCRIPTS === //

        // Récupération des données
        private Dictionary<string, object> CollectData()
        {
            if (this.DataContext is Optimizer.Services.CharactersViewModel viewModel)
            {
                var data = new Dictionary<string, object>
        {
            { "Personnages", viewModel.Personnages },
            // Mouse Clone
            { "MC_GlobalStatus", TglBtn_MC_GlobalStatus.IsChecked },
            { "MC_Shortcut", Btn_MC_Shortcut.Content.ToString() },
            { "MC_Delays", ChkBox_MC_Delays.IsChecked },
            { "MC_MinDelay", TxtBox_MC_MinDelay.Text.Replace("ms", "") },
            { "MC_MaxDelay", TxtBox_MC_MaxDelay.Text.Replace("ms", "") },
            { "MC_Layout", CboBox_MC_Layout.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content.ToString() : null },
            { "ActiveWindows_MC", viewModel.Personnages.Where(p => p.MouseClone).Select(p => p.WindowName).ToHashSet() },
            // Hotkey Clone
            { "HC_GlobalStatus", TglBtn_HC_GlobalStatus.IsChecked },
            { "HC_Shortcut", Btn_HC_Shortcut.Content.ToString() },
            { "HC_Delays", ChkBox_HC_Delays.IsChecked },
            { "HC_MinDelay", TxtBox_HC_MinDelay.Text.Replace("ms", "") },
            { "HC_MaxDelay", TxtBox_HC_MaxDelay.Text.Replace("ms", "") },
            { "ActiveWindows_HC", viewModel.Personnages.Where(p => p.HotkeyClone).Select(p => p.WindowName).ToHashSet() },
            // Window Switcher
            { "WS_GlobalStatus", TglBtn_WS_GlobalStatus.IsChecked },
            { "WS_Shortcut", Btn_WS_Shortcut.Content.ToString() },
            { "ActiveWindows_WS", viewModel.Personnages.Where(p => p.WindowSwitcher).Select(p => p.WindowName).ToHashSet() },
            // Easy Team
            { "ET_GlobalStatus", TglBtn_ET_GlobalStatus.IsChecked },
            { "ET_Leader", CboBox_ET_Leader.SelectedItem is LeaderOption selectedOption && selectedOption.Value != null ? selectedOption.Value.WindowName : null },
            { "ActiveWindows_ET", viewModel.Personnages.Where(p => p.EasyTeam).Select(p => p.WindowName).ToHashSet() }
        };
                string leaderWindowName = data["ET_Leader"] as string;
                System.Diagnostics.Debug.WriteLine($"Titre de la fenêtre Easy Team dans CollectData : {leaderWindowName}");

                return data;
            }
            else
            {
                throw new InvalidOperationException("DataContext n'est pas une instance de CharactersViewModel.");
            }
        }

        // Déclaration des process pour chaque fonctionnalité
        private Process _mcProcess;
        private Process _hcProcess;
        private Process _wsProcess;
        private Process _etProcess;

        // LANCEMENT DE LA FONCTIONNALITE (SCRIPT) MOUSE CLONE
        private void TglBtn_MC_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer les données brutes
                var rawData = CollectData();

                // Convertir les données pour AHK v2
                var ahkData = AhkConverter.ConvertToAhkData(rawData);

                // Générer le script AHK pour Mouse Clone
                ScriptGenerator.Generate_MC_Script(ahkData);

                // Démarrer le script
                StartScript("MouseClone", ref _mcProcess);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération ou de l'exécution du script Mouse Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FERMETURE DE LA FONCTIONNALITE (SCRIPT) MOUSE CLONE
        private void TglBtn_MC_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _mcProcess);
        }

        // LANCEMENT DE LA FONCTIONNALITE (SCRIPT) HOTKEY CLONE
        private void TglBtn_HC_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer les données brutes
                var rawData = CollectData();

                // Convertir les données pour AHK v2
                var ahkData = AhkConverter.ConvertToAhkData(rawData);

                // Générer le script AHK pour Hotkey Clone
                ScriptGenerator.Generate_HC_Script(ahkData);

                // Démarrer le script
                StartScript("HotkeyClone", ref _hcProcess);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération ou de l'exécution du script Hotkey Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FERMETURE DE LA FONCTIONNALITE (SCRIPT) HOTKEY CLONE
        private void TglBtn_HC_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _hcProcess);
        }

        // LANCEMENT DE LA FONCTIONNALITE (SCRIPT) WINDOW SWITCHER
        private void TglBtn_WS_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer les données brutes
                var rawData = CollectData();

                // Convertir les données pour AHK v2
                var ahkData = AhkConverter.ConvertToAhkData(rawData);

                // Générer le script AHK pour Hotkey Clone
                ScriptGenerator.Generate_WS_Script(ahkData);

                // Démarrer le script
                StartScript("WindowSwitcher", ref _wsProcess);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération ou de l'exécution du script Window Switcher : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FERMETURE DE LA FONCTIONNALITE (SCRIPT) WINDOW SWITCHER
        private void TglBtn_WS_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _wsProcess);
        }

        // LANCEMENT DE LA FONCTIONNALITE (SCRIPT) EASY TEAM
        private void TglBtn_ET_GlobalStatus_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier si un chef d'équipe valide est sélectionné
                if (CboBox_ET_Leader.SelectedItem is LeaderOption selectedOption && selectedOption.Value == null)
                {
                    System.Windows.MessageBox.Show("Veuillez sélectionner un chef d'équipe valide avant d'activer cette fonctionnalité.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Désactiver immédiatement le ToggleButton
                    TglBtn_ET_GlobalStatus.IsChecked = false;
                    return;
                }

                // Récupérer les données brutes
                var rawData = CollectData();

                // Convertir les données pour AHK v2
                var ahkData = AhkConverter.ConvertToAhkData(rawData);

                // Générer le script AHK pour Easy Team
                ScriptGenerator.Generate_ET_Script(ahkData);

                // Activer le bouton Btn_EasyTeam
                Btn_EasyTeam.IsEnabled = true;

                // Informer l'utilisateur que le script a été généré
                System.Windows.MessageBox.Show("Le script Easy Team a été généré avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la génération du script Easy Team : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FERMETURE DE LA FONCTIONNALITE (SCRIPT) EASY TEAM
        private void TglBtn_ET_GlobalStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            StopScript(ref _etProcess);
            Btn_EasyTeam.IsEnabled = false;
        }

        // METHODE DE LANCEMENT D'UN SCRIPT
        private void StartScript(string scriptName, ref Process process)
        {
            // Arrêter le script actuel s'il est déjà en cours d'exécution
            StopScript(ref process);

            // Chemin du fichier script
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{scriptName}.ahk");

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

        // METHODE DE FERMETURE D'UN SCRIPT
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

        // METHODE DE MISE A JOUR D'UN SCRIPT ACTIF
        private void UpdateScriptIfNeeded(string scriptName, ref Process process, Func<bool> isGlobalStatusChecked, Action generateScriptAction)
        {
            // Vérifier si une TextBox a encore le focus
            if (Keyboard.FocusedElement is System.Windows.Controls.TextBox)
            {
                return; // Ignorer la mise à jour si une TextBox est active
            }

            // Vérifier si le ToggleButton _GlobalStatus est activé
            if (isGlobalStatusChecked())
            {
                // Arrêter le script en cours
                StopScript(ref process);

                // Regénérer le script
                generateScriptAction();

                // Relancer le script SAUF pour Easy Team
                if (scriptName != "EasyTeam")
                {
                    StartScript(scriptName, ref process);
                }
            }
        }

        // METHODE DE MISE A JOUR D'UN SCRIPT ACTIF POUR LES BOUTONS DE RACCOURCIS
        private void UpdateScriptIfNeededForButton(System.Windows.Controls.Button button)
        {
            if (button == Btn_MC_Shortcut)
            {
                UpdateScriptIfNeeded(
                    "MouseClone",
                    ref _mcProcess,
                    () => TglBtn_MC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_MC_Script(ahkData);
                    });
            }
            else if (button == Btn_HC_Shortcut)
            {
                UpdateScriptIfNeeded(
                    "HotkeyClone",
                    ref _hcProcess,
                    () => TglBtn_HC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_HC_Script(ahkData);
                    });
            }
            else if (button == Btn_WS_Shortcut)
            {
                UpdateScriptIfNeeded(
                    "WindowSwitcher",
                    ref _wsProcess,
                    () => TglBtn_WS_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_WS_Script(ahkData);
                    });
            }
        }

        // METHODE DE MISE A JOUR D'UN SCRIPT ACTIF POUR LES CHECKBOX DE DELAIS
        private void UpdateScriptIfNeededForDelaysCheckbox(System.Windows.Controls.CheckBox checkBox)
        {
            if (checkBox == ChkBox_MC_Delays)
            {
                UpdateScriptIfNeeded(
                    "MouseClone",
                    ref _mcProcess,
                    () => TglBtn_MC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_MC_Script(ahkData);
                    });
            }
            else if (checkBox == ChkBox_HC_Delays)
            {
                UpdateScriptIfNeeded(
                    "HotkeyClone",
                    ref _hcProcess,
                    () => TglBtn_HC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_HC_Script(ahkData);
                    });
            }
        }

        // METHODE DE MISE A JOUR D'UN SCRIPT ACTIF POUR LES TEXTBOX DES DELAIS
        private void UpdateScriptIfNeededForTextBox(System.Windows.Controls.TextBox textBox)
        {
            if (textBox == TxtBox_MC_MinDelay || textBox == TxtBox_MC_MaxDelay)
            {
                UpdateScriptIfNeeded(
                    "MouseClone",
                    ref _mcProcess,
                    () => TglBtn_MC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_MC_Script(ahkData);
                    });
            }
            else if (textBox == TxtBox_HC_MinDelay || textBox == TxtBox_HC_MaxDelay)
            {
                UpdateScriptIfNeeded(
                    "HotkeyClone",
                    ref _hcProcess,
                    () => TglBtn_HC_GlobalStatus.IsChecked == true,
                    () =>
                    {
                        var rawData = CollectData();
                        var ahkData = AhkConverter.ConvertToAhkData(rawData);
                        ScriptGenerator.Generate_HC_Script(ahkData);
                    });
            }
        }
    }
}