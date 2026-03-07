using Hardcodet.Wpf.TaskbarNotification;
using Optimizer.Models;
using Optimizer.Services;
using Optimizer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
// Alias pour éviter les conflits
using WpfApplication = System.Windows.Application;
using WpfButton = System.Windows.Controls.Button;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Optimizer.Views
{
    public partial class MainWindow : Window
    {
        // DÉCLARATIONS
        private Dictionary<WpfButton, bool> _waitingButtons = new Dictionary<WpfButton, bool>();
        private DispatcherTimer _errorTimer;
        private TaskbarIcon? taskbarIcon;
        private MenuItem? _quickLaunchMenuItem;
        private readonly Dictionary<WpfButton, string> _previousShortcuts = new Dictionary<WpfButton, string>();
        private readonly MainViewModel _viewModel;
        private WpfButton? _currentErrorButton;
        private readonly Dictionary<WpfTextBox, string> _previousDelayValues = new Dictionary<WpfTextBox, string>();
        private bool _isInitializing = true;
        private const int SnapDistance = 25;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            this.Opacity = 0;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.Loaded += (s, e) =>
            {
                if (_viewModel.WindowLeft.HasValue && _viewModel.WindowTop.HasValue)
                {
                    this.Left = _viewModel.WindowLeft.Value;
                    this.Top = _viewModel.WindowTop.Value;
                }
                this.Opacity = 1;
            };

            InitializeTaskbarIcon();
            InitializeContextMenu();

            // S'abonner aux changements du ViewModel pour mettre à jour le menu contextuel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _errorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _errorTimer.Tick += (s, e) =>
            {
                if (_currentErrorButton != null && _previousShortcuts.ContainsKey(_currentErrorButton))
                    _currentErrorButton.Content = _previousShortcuts[_currentErrorButton];
                _errorTimer.Stop();
            };

            this.Loaded += (s, e) => _isInitializing = false;
        }

        #region Méthodes - Initialisation

        private void InitializeTaskbarIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "optimizer_icon.ico");

                if (File.Exists(iconPath))
                {
                    taskbarIcon = new TaskbarIcon
                    {
                        Icon = new System.Drawing.Icon(iconPath),
                        ToolTipText = "Optimizer",
                        Visibility = Visibility.Collapsed
                    };
                }
                else
                {
                    var uri = new Uri("pack://application:,,,/Resources/Icons/optimizer_icon.ico");
                    var streamInfo = WpfApplication.GetResourceStream(uri);

                    taskbarIcon = new TaskbarIcon
                    {
                        Icon = streamInfo != null
                            ? new System.Drawing.Icon(streamInfo.Stream)
                            : System.Drawing.SystemIcons.Application,
                        ToolTipText = "Optimizer",
                        Visibility = Visibility.Collapsed
                    };
                }

                if (taskbarIcon != null)
                    taskbarIcon.TrayMouseDoubleClick += (s, e) => RestoreWindow();
            }
            catch (Exception)
            {
                taskbarIcon = new TaskbarIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    ToolTipText = "Optimizer",
                    Visibility = Visibility.Collapsed
                };
            }
        }

        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenu();
            contextMenu.Style = (Style)this.FindResource(typeof(ContextMenu));

            // --- Afficher ---
            var showMenuItem = new MenuItem { Header = "Afficher" };
            showMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            showMenuItem.Click += (s, e) => RestoreWindow();
            contextMenu.Items.Add(showMenuItem);

            // --- Séparateur ---
            contextMenu.Items.Add(new MenuItem { Style = (Style)this.FindResource("MenuSeparatorStyle") });

            // --- Lancement rapide / Tout arrêter ---
            // Le Header et l'icône sont mis à jour dynamiquement via ViewModel_PropertyChanged
            _quickLaunchMenuItem = new MenuItem();
            _quickLaunchMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            _quickLaunchMenuItem.Command = _viewModel.QuickLaunchCommand;
            _quickLaunchMenuItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanUseQuickLaunch") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            UpdateQuickLaunchMenuHeader();
            contextMenu.Items.Add(_quickLaunchMenuItem);

            // --- Toggle Mouse Clone ---
            var mcToggleMenuItem = new MenuItem { Header = "Mouse Clone", StaysOpenOnClick = true };
            mcToggleMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            var mcToggleButton = new ToggleButton { Width = 25, Height = 16, Margin = new Thickness(0), Style = (Style)this.FindResource("SwitchToggleButton") };
            mcToggleButton.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsMouseCloneEnabled") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            mcToggleButton.SetBinding(ToggleButton.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableMouseClone") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            mcToggleMenuItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableMouseClone") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            mcToggleMenuItem.Icon = mcToggleButton;
            contextMenu.Items.Add(mcToggleMenuItem);

            // --- Toggle Hotkey Clone ---
            var hcToggleMenuItem = new MenuItem { Header = "Hotkey Clone", StaysOpenOnClick = true };
            hcToggleMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            var hcToggleButton = new ToggleButton { Width = 25, Height = 16, Margin = new Thickness(0), Style = (Style)this.FindResource("SwitchToggleButton") };
            hcToggleButton.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsHotkeyCloneEnabled") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            hcToggleButton.SetBinding(ToggleButton.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableHotkeyClone") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            hcToggleMenuItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableHotkeyClone") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            hcToggleMenuItem.Icon = hcToggleButton;
            contextMenu.Items.Add(hcToggleMenuItem);

            // --- Toggle Window Switcher ---
            var wsToggleMenuItem = new MenuItem { Header = "Window Switcher", StaysOpenOnClick = true };
            wsToggleMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            var wsToggleButton = new ToggleButton { Width = 25, Height = 16, Margin = new Thickness(0), Style = (Style)this.FindResource("SwitchToggleButton") };
            wsToggleButton.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsWindowSwitcherEnabled") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            wsToggleButton.SetBinding(ToggleButton.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableWindowSwitcher") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            wsToggleMenuItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanEnableWindowSwitcher") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            wsToggleMenuItem.Icon = wsToggleButton;
            contextMenu.Items.Add(wsToggleMenuItem);

            // --- Easy Team ---
            var easyTeamMenuItem = new MenuItem { Header = "Easy Team" };
            easyTeamMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            easyTeamMenuItem.Click += (s, e) => _viewModel.ExecuteEasyTeam();
            easyTeamMenuItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("IsEasyTeamButtonEnabled") { Source = _viewModel, Mode = System.Windows.Data.BindingMode.OneWay });
            contextMenu.Items.Add(easyTeamMenuItem);

            // --- Séparateur ---
            contextMenu.Items.Add(new MenuItem { Style = (Style)this.FindResource("MenuSeparatorStyle") });

            // --- Fermer ---
            var exitMenuItem = new MenuItem { Header = "Fermer" };
            exitMenuItem.Style = (Style)this.FindResource(typeof(MenuItem));
            exitMenuItem.Click += (s, e) => GlobalShutdown();
            contextMenu.Items.Add(exitMenuItem);

            if (taskbarIcon != null)
                taskbarIcon.ContextMenu = contextMenu;
        }

        #endregion

        #region Méthodes - Fenêtre

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            if (taskbarIcon != null)
                taskbarIcon.Visibility = Visibility.Collapsed;
        }

        private void GlobalShutdown()
        {
            _viewModel.WindowLeft = this.Left;
            _viewModel.WindowTop = this.Top;
            _viewModel?.Shutdown();
            WpfApplication.Current.Shutdown();
        }

        /// <summary>
        /// Point d'entrée public pour App.xaml.cs — délègue au ViewModel sans exposer _viewModel.
        /// </summary>
        public void Shutdown()
        {
            _viewModel?.Shutdown();
        }

        /// <summary>
        /// Met à jour le Header et l'icône du menu contextuel Lancement rapide / Tout arrêter
        /// en fonction de IsQuickLaunchMode exposé par le ViewModel.
        /// </summary>
        private void UpdateQuickLaunchMenuHeader()
        {
            if (_quickLaunchMenuItem == null) return;

            if (_viewModel.IsQuickLaunchMode)
            {
                _quickLaunchMenuItem.Header = "Lancement rapide";
                _quickLaunchMenuItem.Icon = new TextBlock
                {
                    Text = "▶",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    Width = 25,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else
            {
                _quickLaunchMenuItem.Header = "Tout arrêter";
                _quickLaunchMenuItem.Icon = new TextBlock
                {
                    Text = "■",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    Width = 25,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsQuickLaunchMode))
                UpdateQuickLaunchMenuHeader();
        }

        /// <summary>
        /// Désabonnement propre de tous les événements lors de la fermeture de la fenêtre.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _errorTimer.Stop();
            taskbarIcon?.Dispose();

            base.OnClosed(e);
        }

        private void SnapToScreenEdges()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;

            double left = this.Left;
            double top = this.Top;
            double right = this.Left + this.ActualWidth;
            double bottom = this.Top + this.ActualHeight;

            if (left < workingArea.Left)
                this.Left = workingArea.Left;
            else if (Math.Abs(left - workingArea.Left) < SnapDistance)
                this.Left = workingArea.Left;

            if (right > workingArea.Right)
                this.Left = workingArea.Right - this.ActualWidth;
            else if (Math.Abs(right - workingArea.Right) < SnapDistance)
                this.Left = workingArea.Right - this.ActualWidth;

            if (top < workingArea.Top)
                this.Top = workingArea.Top;
            else if (Math.Abs(top - workingArea.Top) < SnapDistance)
                this.Top = workingArea.Top;

            if (bottom > workingArea.Bottom)
                this.Top = workingArea.Bottom - this.ActualHeight;
            else if (Math.Abs(bottom - workingArea.Bottom) < SnapDistance)
                this.Top = workingArea.Bottom - this.ActualHeight;
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
                SnapToScreenEdges();
            }
        }

        private void Btn_Hide_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            if (taskbarIcon != null)
                taskbarIcon.Visibility = Visibility.Visible;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalShutdown();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
            {
                this.Height = 780;
                return;
            }

            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;

            double expansionAmount = 350;
            double halfExpansion = expansionAmount / 2;
            double idealTop = this.Top - halfExpansion;
            double idealBottom = this.Top + 390 + halfExpansion;

            double finalTopOffset = -halfExpansion;
            if (idealTop < workingArea.Top)
                finalTopOffset = -halfExpansion + (workingArea.Top - idealTop);
            else if (idealBottom > workingArea.Bottom)
                finalTopOffset = -halfExpansion - (idealBottom - workingArea.Bottom);

            var heightAnimation = new DoubleAnimation
            {
                From = 390,
                To = 780,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            if (finalTopOffset != 0)
            {
                var topAnimation = new DoubleAnimation
                {
                    From = this.Top,
                    To = this.Top + finalTopOffset,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                topAnimation.Completed += (s, args) => this.BeginAnimation(Window.TopProperty, null);
                this.BeginAnimation(Window.TopProperty, topAnimation);
            }

            this.BeginAnimation(Window.HeightProperty, heightAnimation);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
            {
                this.Height = 390;
                return;
            }

            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;

            double contractionAmount = 350;
            double halfContraction = contractionAmount / 2;
            double currentBottom = this.Top + 780;

            double finalTopOffset = halfContraction;
            if (this.Top <= workingArea.Top + 5)
                finalTopOffset = 0;
            else if (currentBottom >= workingArea.Bottom - 5)
                finalTopOffset = workingArea.Bottom - (this.Top + 390);

            var heightAnimation = new DoubleAnimation
            {
                From = 780,
                To = 390,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            if (finalTopOffset != 0)
            {
                var topAnimation = new DoubleAnimation
                {
                    From = this.Top,
                    To = this.Top + finalTopOffset,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                topAnimation.Completed += (s, args) => this.BeginAnimation(Window.TopProperty, null);
                this.BeginAnimation(Window.TopProperty, topAnimation);
            }

            this.BeginAnimation(Window.HeightProperty, heightAnimation);
        }

        #endregion

        #region Méthodes - Toggle Buttons (Handlers pour compatibilité avec le XAML existant)

        private void TglBtn_MC_WindowStatus_Checked(object sender, RoutedEventArgs e) { }
        private void TglBtn_MC_WindowStatus_Unchecked(object sender, RoutedEventArgs e) { }
        private void TglBtn_HC_WindowStatus_Checked(object sender, RoutedEventArgs e) { }
        private void TglBtn_HC_WindowStatus_Unchecked(object sender, RoutedEventArgs e) { }
        private void TglBtn_WS_WindowStatus_Checked(object sender, RoutedEventArgs e) { }
        private void TglBtn_WS_WindowStatus_Unchecked(object sender, RoutedEventArgs e) { }
        private void TglBtn_ET_WindowStatus_Checked(object sender, RoutedEventArgs e) { }
        private void TglBtn_ET_WindowStatus_Unchecked(object sender, RoutedEventArgs e) { }

        #endregion

        #region Méthodes - Raccourcis

        private bool IsShortcutAlreadyUsed(WpfButton currentButton, string shortcut)
        {
            return (Btn_MC_Shortcut != currentButton && Btn_MC_Shortcut.Content.ToString() == shortcut) ||
                   (Btn_HC_Shortcut != currentButton && Btn_HC_Shortcut.Content.ToString() == shortcut) ||
                   (Btn_WS_Shortcut != currentButton && Btn_WS_Shortcut.Content.ToString() == shortcut) ||
                   (Btn_AF_Shortcut != currentButton && Btn_AF_Shortcut.Content.ToString() == shortcut);
        }

        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (WpfButton)sender;

            if (!_previousShortcuts.ContainsKey(button))
                _previousShortcuts[button] = button.Content?.ToString() ?? string.Empty;

            button.Content = "Définir un raccourci...";
            Overlay.Visibility = Visibility.Visible;
            _waitingButtons[button] = true;

            this.PreviewKeyUp += Window_PreviewKeyUp;
            this.PreviewMouseUp += Window_PreviewMouseUp;
        }

        private void Window_PreviewKeyUp(object sender, WpfKeyEventArgs e)
        {
            foreach (var entry in _waitingButtons.Where(b => b.Value).ToList())
            {
                var button = entry.Key;

                if (e.Key == Key.Escape)
                {
                    button.Content = _previousShortcuts.ContainsKey(button)
                        ? _previousShortcuts[button]
                        : "Raccourci non défini";
                    _previousShortcuts.Remove(button);
                    CleanupForButton(button);
                    e.Handled = true;
                    return;
                }

                if (ShortcutValidationService.IsModifierKey(e.Key))
                    return;

                Key actualKey = e.Key == Key.System ? e.SystemKey : e.Key;

                if (!ShortcutMappingService.IsKeyAllowed(actualKey))
                {
                    button.Content = "Raccourci indisponible !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                var shortcutInfo = ShortcutMappingService.GetShortcutInfo(actualKey);
                if (shortcutInfo == null)
                {
                    button.Content = "Erreur interne !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                string displayName = shortcutInfo.DisplayName;

                if (_currentErrorButton == button)
                {
                    _errorTimer.Stop();
                    _currentErrorButton = null;
                }

                if (IsShortcutAlreadyUsed(button, displayName))
                {
                    button.Content = "Raccourci déjà utilisé !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                button.Content = displayName;

                if (_previousShortcuts.ContainsKey(button))
                    _previousShortcuts[button] = displayName;
                else
                    _previousShortcuts.Add(button, displayName);

                UpdateViewModelShortcut(button, displayName);
                CleanupForButton(button);
                e.Handled = true;

                if (actualKey == Key.Space)
                {
                    Keyboard.ClearFocus();
                    this.Focus();
                }
            }
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            foreach (var entry in _waitingButtons.Where(b => b.Value).ToList())
            {
                var button = entry.Key;

                if (e.ChangedButton == MouseButton.Left)
                {
                    button.Content = "Raccourci indisponible !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                if (!ShortcutMappingService.IsMouseButtonAllowed(e.ChangedButton))
                {
                    button.Content = "Raccourci indisponible !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                var shortcutInfo = ShortcutMappingService.GetShortcutInfo(e.ChangedButton);
                if (shortcutInfo == null)
                {
                    button.Content = "Erreur interne !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                string displayName = shortcutInfo.DisplayName;

                if (_currentErrorButton == button)
                {
                    _errorTimer.Stop();
                    _currentErrorButton = null;
                }

                if (IsShortcutAlreadyUsed(button, displayName))
                {
                    button.Content = "Raccourci déjà utilisé !";
                    _currentErrorButton = button;
                    _errorTimer.Start();
                    return;
                }

                button.Content = displayName;

                if (_previousShortcuts.ContainsKey(button))
                    _previousShortcuts[button] = displayName;
                else
                    _previousShortcuts.Add(button, displayName);

                UpdateViewModelShortcut(button, displayName);
                CleanupForButton(button);
                e.Handled = true;
            }
        }

        private void UpdateViewModelShortcut(WpfButton button, string shortcut)
        {
            if (button == Btn_MC_Shortcut)
                _viewModel.MouseCloneShortcut = shortcut;
            else if (button == Btn_HC_Shortcut)
                _viewModel.HotkeyCloneShortcut = shortcut;
            else if (button == Btn_WS_Shortcut)
                _viewModel.WindowSwitcherShortcut = shortcut;
            else if (button == Btn_AF_Shortcut)
                _viewModel.AutoFollowShortcut = shortcut;
        }

        private void CleanupForButton(WpfButton button)
        {
            _waitingButtons[button] = false;
            Overlay.Visibility = Visibility.Collapsed;

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

        #endregion

        #region Méthodes - TextBox

        private void ClearFocus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, UIMainWindow);
        }

        private void NumberValidation_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);
        }

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

        private void TextBox_Delay_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (WpfTextBox)sender;
            if (!string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text != "ms")
                _previousDelayValues[textBox] = textBox.Text;
            if (textBox.Text.EndsWith("ms"))
                textBox.Text = "";
        }

        private void TextBox_Delay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (WpfTextBox)sender;
            if (!string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text != "ms")
                _previousDelayValues[textBox] = textBox.Text;
            textBox.Focus();
            if (textBox.Text.EndsWith("ms"))
                textBox.Text = "";
            else
                textBox.SelectAll();
            e.Handled = true;
        }

        private void TextBox_Delay_PreviewKeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var textBox = (WpfTextBox)sender;
                if (!string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text != "ms")
                    _previousDelayValues[textBox] = textBox.Text;

                var elementWithFocus = Keyboard.FocusedElement as UIElement;
                if (elementWithFocus != null)
                {
                    elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                    var nextTextBox = Keyboard.FocusedElement as WpfTextBox;
                    if (nextTextBox != null && nextTextBox.Name.Contains("Delay"))
                    {
                        if (!string.IsNullOrWhiteSpace(nextTextBox.Text) && nextTextBox.Text != "ms")
                            _previousDelayValues[nextTextBox] = nextTextBox.Text;
                        if (nextTextBox.Text.EndsWith("ms"))
                            nextTextBox.Text = "";
                    }

                    e.Handled = true;
                }
            }
        }

        private void TextBox_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (WpfTextBox)sender;
                var binding = textBox.GetBindingExpression(WpfTextBox.TextProperty);

                if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == "ms")
                {
                    if (_previousDelayValues.TryGetValue(textBox, out string? previousValue))
                        textBox.Text = previousValue;
                    binding?.UpdateTarget();
                }
                else
                {
                    binding?.UpdateSource();
                }

                _previousDelayValues.Remove(textBox);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void TextBox_Delay_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (WpfTextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == "ms")
            {
                if (_previousDelayValues.TryGetValue(textBox, out string? previousValue))
                {
                    textBox.Text = previousValue;
                    textBox.GetBindingExpression(WpfTextBox.TextProperty)?.UpdateTarget();
                }
            }
            _previousDelayValues.Remove(textBox);
        }

        private void ComboBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var comboBox = border?.TemplatedParent as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                e.Handled = true;
            }
        }

        #endregion
    }
}