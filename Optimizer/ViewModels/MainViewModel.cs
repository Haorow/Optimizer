using Optimizer.Converters;
using Optimizer.Helpers;
using Optimizer.Models;
using Optimizer.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Optimizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Services

        private readonly UpdateService _updateService = new();
        private readonly WindowDiscoveryService _windowDiscoveryService;
        private readonly ScriptExecutionService _scriptExecutionService;
        private readonly SettingsService _settingsService;

        private bool _isLoadingSettings = false;
        private bool _isInitializing = true;

        // Debounce pour éviter les écritures disque en rafale
        private readonly System.Windows.Threading.DispatcherTimer _saveDebounceTimer;

        #endregion

        #region Propriétés - Mise à jour de l'application

        private bool _isUpdating;
        public bool IsUpdating
        {
            get => _isUpdating;
            set { _isUpdating = value; OnPropertyChanged(nameof(IsUpdating)); }
        }

        private double _updateProgress;
        public double UpdateProgress
        {
            get => _updateProgress;
            set { _updateProgress = value; OnPropertyChanged(nameof(UpdateProgress)); }
        }

        private string _updateStatusText = "Mise à jour en cours...";
        public string UpdateStatusText
        {
            get => _updateStatusText;
            set { _updateStatusText = value; OnPropertyChanged(nameof(UpdateStatusText)); }
        }

        #endregion

        #region Propriétés - ViewModels enfants

        public CharactersViewModel CharactersViewModel { get; }

        #endregion

        #region Propriété -Setup

        // Setup — Vitesse d'exécution
        private int _executionSpeedIndex = 1;
        public int ExecutionSpeedIndex
        {
            get => _executionSpeedIndex;
            set
            {
                if (_executionSpeedIndex != value)
                {
                    _executionSpeedIndex = Math.Clamp(value, 0, 2);
                    OnPropertyChanged();
                    UpdateAllActiveScripts();
                    SaveSettingsNow();
                }
            }
        }

        /// <summary>
        /// Valeur SpeedDelay passée aux scripts AHK selon l'état courant.
        /// 0 = Lent → 200ms | 1 = Normal → 100ms | - = Rapide → 0 (désactivé)
        /// </summary>
        private int SpeedDelay => ExecutionSpeedIndex switch
        {
            0 => 200,  // Lent
            1 => 100,  // Normal
            _ => 0     // Rapide
        };

        #endregion

        #region Propriétés - Mouse Clone

        private bool _isMouseCloneEnabled;
        public bool IsMouseCloneEnabled
        {
            get => _isMouseCloneEnabled;
            set
            {
                if (_isMouseCloneEnabled != value)
                {
                    _isMouseCloneEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanEnableAutoFollow));

                    if (value)
                        StartMouseClone();
                    else
                        StopMouseClone();
                }
            }
        }

        private string _mouseCloneShortcut = "F1";
        public string MouseCloneShortcut
        {
            get => _mouseCloneShortcut;
            set
            {
                if (_mouseCloneShortcut != value)
                {
                    _mouseCloneShortcut = value;
                    OnPropertyChanged();
                    UpdateMouseCloneAutoFollowShortcut();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private bool _mouseCloneDelaysEnabled = true;
        public bool MouseCloneDelaysEnabled
        {
            get => _mouseCloneDelaysEnabled;
            set
            {
                if (_mouseCloneDelaysEnabled != value)
                {
                    _mouseCloneDelaysEnabled = value;
                    OnPropertyChanged();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private int _mouseCloneMinDelay = 50;
        public int MouseCloneMinDelay
        {
            get => _mouseCloneMinDelay;
            set
            {
                if (_mouseCloneMinDelay != value)
                {
                    _mouseCloneMinDelay = Math.Clamp(value, 0, 999);

                    if (_mouseCloneMinDelay > _mouseCloneMaxDelay)
                        MouseCloneMaxDelay = _mouseCloneMinDelay;

                    OnPropertyChanged();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private int _mouseCloneMaxDelay = 125;
        public int MouseCloneMaxDelay
        {
            get => _mouseCloneMaxDelay;
            set
            {
                if (_mouseCloneMaxDelay != value)
                {
                    _mouseCloneMaxDelay = Math.Clamp(value, 0, 999);

                    if (_mouseCloneMaxDelay < _mouseCloneMinDelay)
                        MouseCloneMinDelay = _mouseCloneMaxDelay;

                    OnPropertyChanged();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private int _mouseCloneLayoutIndex = 1;
        public int MouseCloneLayoutIndex
        {
            get => _mouseCloneLayoutIndex;
            set
            {
                if (_mouseCloneLayoutIndex != value)
                {
                    _mouseCloneLayoutIndex = value;
                    OnPropertyChanged();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        #endregion

        #region Propriétés - Hotkey Clone

        private bool _isHotkeyCloneEnabled;
        public bool IsHotkeyCloneEnabled
        {
            get => _isHotkeyCloneEnabled;
            set
            {
                if (_isHotkeyCloneEnabled != value)
                {
                    _isHotkeyCloneEnabled = value;
                    OnPropertyChanged();

                    if (value)
                        StartHotkeyClone();
                    else
                        StopHotkeyClone();
                }
            }
        }

        private string _hotkeyCloneShortcut = "F2";
        public string HotkeyCloneShortcut
        {
            get => _hotkeyCloneShortcut;
            set
            {
                if (_hotkeyCloneShortcut != value)
                {
                    _hotkeyCloneShortcut = value;
                    OnPropertyChanged();
                    UpdateHotkeyCloneAutoFollowShortcut();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private bool _hotkeyCloneDelaysEnabled = true;
        public bool HotkeyCloneDelaysEnabled
        {
            get => _hotkeyCloneDelaysEnabled;
            set
            {
                if (_hotkeyCloneDelaysEnabled != value)
                {
                    _hotkeyCloneDelaysEnabled = value;
                    OnPropertyChanged();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private int _hotkeyCloneMinDelay = 50;
        public int HotkeyCloneMinDelay
        {
            get => _hotkeyCloneMinDelay;
            set
            {
                if (_hotkeyCloneMinDelay != value)
                {
                    _hotkeyCloneMinDelay = Math.Clamp(value, 0, 999);

                    if (_hotkeyCloneMinDelay > _hotkeyCloneMaxDelay)
                        HotkeyCloneMaxDelay = _hotkeyCloneMinDelay;

                    OnPropertyChanged();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private int _hotkeyCloneMaxDelay = 125;
        public int HotkeyCloneMaxDelay
        {
            get => _hotkeyCloneMaxDelay;
            set
            {
                if (_hotkeyCloneMaxDelay != value)
                {
                    _hotkeyCloneMaxDelay = Math.Clamp(value, 0, 999);

                    if (_hotkeyCloneMaxDelay < _hotkeyCloneMinDelay)
                        HotkeyCloneMinDelay = _hotkeyCloneMaxDelay;

                    OnPropertyChanged();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        #endregion

        #region Propriétés - Window Switcher

        private bool _isWindowSwitcherEnabled;
        public bool IsWindowSwitcherEnabled
        {
            get => _isWindowSwitcherEnabled;
            set
            {
                if (_isWindowSwitcherEnabled != value)
                {
                    _isWindowSwitcherEnabled = value;
                    OnPropertyChanged();

                    if (value)
                        StartWindowSwitcher();
                    else
                        StopWindowSwitcher();
                }
            }
        }

        private string _windowSwitcherShortcut = "F3";
        public string WindowSwitcherShortcut
        {
            get => _windowSwitcherShortcut;
            set
            {
                if (_windowSwitcherShortcut != value)
                {
                    _windowSwitcherShortcut = value;
                    OnPropertyChanged();
                    UpdateWindowSwitcherScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        #endregion

        #region Propriétés - Easy Team

        private ObservableCollection<LeaderOption> _leaderOptions = new();
        public ObservableCollection<LeaderOption> LeaderOptions
        {
            get => _leaderOptions;
            set
            {
                _leaderOptions = value;
                OnPropertyChanged();
            }
        }

        private LeaderOption? _selectedLeader;
        public LeaderOption? SelectedLeader
        {
            get => _selectedLeader;
            set
            {
                if (_selectedLeader != value)
                {
                    _selectedLeader = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanEnableAutoFollow));
                    OnLeaderChanged();
                    UpdateMouseCloneScriptIfActive();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private bool _isEasyTeamButtonEnabled;
        public bool IsEasyTeamButtonEnabled
        {
            get => _isEasyTeamButtonEnabled;
            set
            {
                _isEasyTeamButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Propriétés - Interface

        private double? _windowLeft = null;
        public double? WindowLeft
        {
            get => _windowLeft;
            set
            {
                _windowLeft = value;
                OnPropertyChanged();
            }
        }

        private double? _windowTop = null;
        public double? WindowTop
        {
            get => _windowTop;
            set
            {
                _windowTop = value;
                OnPropertyChanged();
            }
        }

        private bool _isSettingsPanelExpanded;
        public bool IsSettingsPanelExpanded
        {
            get => _isSettingsPanelExpanded;
            set
            {
                _isSettingsPanelExpanded = value;
                OnPropertyChanged();
                SaveSettingsNow();
            }
        }

        private bool _isQuickLaunchMode = true;
        public bool IsQuickLaunchMode
        {
            get => _isQuickLaunchMode;
            set
            {
                if (_isQuickLaunchMode != value)
                {
                    _isQuickLaunchMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canUseQuickLaunch;
        public bool CanUseQuickLaunch
        {
            get => _canUseQuickLaunch;
            set
            {
                if (_canUseQuickLaunch != value)
                {
                    _canUseQuickLaunch = value;
                    OnPropertyChanged();
                }
            }
        }

        // AutoFollow MC
        private bool _isMouseCloneAutoFollowEnabled;
        public bool IsMouseCloneAutoFollowEnabled
        {
            get => _isMouseCloneAutoFollowEnabled;
            set
            {
                if (_isMouseCloneAutoFollowEnabled != value)
                {
                    _isMouseCloneAutoFollowEnabled = value;
                    OnPropertyChanged();
                    UpdateMouseCloneAutoFollowShortcut();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private string _mouseCloneAutoFollowShortcut = "Alt+[Raccourci MC]";
        public string MouseCloneAutoFollowShortcut
        {
            get => _mouseCloneAutoFollowShortcut;
            set
            {
                if (_mouseCloneAutoFollowShortcut != value)
                {
                    _mouseCloneAutoFollowShortcut = value;
                    OnPropertyChanged();
                }
            }
        }

        // AutoFollow HC
        private bool _isHotkeyCloneAutoFollowEnabled;
        public bool IsHotkeyCloneAutoFollowEnabled
        {
            get => _isHotkeyCloneAutoFollowEnabled;
            set
            {
                if (_isHotkeyCloneAutoFollowEnabled != value)
                {
                    _isHotkeyCloneAutoFollowEnabled = value;
                    OnPropertyChanged();
                    UpdateHotkeyCloneAutoFollowShortcut();
                    UpdateHotkeyCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        private string _hotkeyCloneAutoFollowShortcut = "Alt+[Raccourci HC]";
        public string HotkeyCloneAutoFollowShortcut
        {
            get => _hotkeyCloneAutoFollowShortcut;
            set
            {
                if (_hotkeyCloneAutoFollowShortcut != value)
                {
                    _hotkeyCloneAutoFollowShortcut = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _autoFollowShortcut = "F5";
        public string AutoFollowShortcut
        {
            get => _autoFollowShortcut;
            set
            {
                if (_autoFollowShortcut != value)
                {
                    _autoFollowShortcut = value;
                    OnPropertyChanged();
                    UpdateMouseCloneScriptIfActive();
                    SaveSettingsNow();
                }
            }
        }

        #endregion

        #region Commandes

        public ICommand RefreshCommand { get; }
        public ICommand ExecuteEasyTeamCommand { get; }
        public ICommand ToggleSettingsPanelCommand { get; }
        public ICommand QuickLaunchCommand { get; }

        #endregion

        #region Propriétés - Activation des toggles globaux

        private bool _canEnableMouseClone;
        public bool CanEnableMouseClone
        {
            get => _canEnableMouseClone;
            set
            {
                if (_canEnableMouseClone != value)
                {
                    _canEnableMouseClone = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canEnableHotkeyClone;
        public bool CanEnableHotkeyClone
        {
            get => _canEnableHotkeyClone;
            set
            {
                if (_canEnableHotkeyClone != value)
                {
                    _canEnableHotkeyClone = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canEnableWindowSwitcher;
        public bool CanEnableWindowSwitcher
        {
            get => _canEnableWindowSwitcher;
            set
            {
                if (_canEnableWindowSwitcher != value)
                {
                    _canEnableWindowSwitcher = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanEnableAutoFollow
            => IsMouseCloneEnabled && SelectedLeader != null && SelectedLeader.Value != null;

        #endregion

        #region Constructeur

        public MainViewModel()
        {
            // Initialiser les services
            _windowDiscoveryService = new WindowDiscoveryService();
            _scriptExecutionService = new ScriptExecutionService();

            // Initialiser SettingsService avec le chemin du fichier
            string settingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "settings.json"
            );
            _settingsService = new SettingsService(settingsPath);

            // Initialiser le timer de debounce pour la sauvegarde (500ms)
            _saveDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                _settingsService.SaveSettings(BuildAppSettings());
            };

            // Vérification précoce : AutoHotkey64.exe présent ?
            var ahkError = _scriptExecutionService.ValidateAutoHotkeyExists();
            if (ahkError != null)
            {
                Logger.Log($"[AVERTISSEMENT] {ahkError}");
                System.Windows.MessageBox.Show(
                    ahkError,
                    "AutoHotkey introuvable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // Initialiser le ViewModel des personnages en injectant le callback leader
            CharactersViewModel = new CharactersViewModel(
                _windowDiscoveryService,
                () => SelectedLeader?.Value
            );

            // S'abonner aux changements
            CharactersViewModel.Personnages.CollectionChanged += Personnages_CollectionChanged;
            CharactersViewModel.PropertyChanged += CharactersViewModel_PropertyChanged;

            // Initialiser les commandes
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ExecuteEasyTeamCommand = new RelayCommand(ExecuteEasyTeam, CanExecuteEasyTeam);
            ToggleSettingsPanelCommand = new RelayCommand(ExecuteToggleSettingsPanel);
            QuickLaunchCommand = new RelayCommand(ExecuteQuickLaunch, CanExecuteQuickLaunch);

            // Initialiser les options de leader
            InitializeLeaderOptions();

            // Charger les paramètres sauvegardés
            LoadSettings();

            // Charger les fenêtres au démarrage
            ExecuteRefresh();

            // Notifier l'état initial de CanEnableAutoFollow
            OnPropertyChanged(nameof(CanEnableAutoFollow));

            // Initialiser le texte des raccourcis AutoFollow
            UpdateMouseCloneAutoFollowShortcut();
            UpdateHotkeyCloneAutoFollowShortcut();

            // Initialiser la vérification de mise à jour de l'application
            _ = CheckForUpdateAsync();

            _isInitializing = false;
        }

        private async Task CheckForUpdateAsync()
        {
            _updateService.AlreadyUpToDate += () =>
            {
            };

            _updateService.ProgressChanged += progress =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    IsUpdating = true;
                    UpdateProgress = progress;
                    UpdateStatusText = progress < 1.0
                        ? $"Mise à jour en cours... {(int)(progress * 100)}%"
                        : "Finalisation...";
                });
            };

            _updateService.UpdateReady += () =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    UpdateStatusText = "Redémarrage...";
                    Task.Delay(500).ContinueWith(_ =>
                        App.Current.Dispatcher.Invoke(() => App.Current.Shutdown()));
                });
            };

            _updateService.UpdateError += message =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    IsUpdating = false;
                    Logger.Log($"❌ Erreur de mise à jour : {message}");
                    System.Windows.Forms.MessageBox.Show($"Erreur MAJ : {message}");
                });
            };

            await _updateService.CheckAndUpdateAsync();
        }

        private void Personnage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Personnage.MouseClone) ||
                e.PropertyName == nameof(Personnage.HotkeyClone) ||
                e.PropertyName == nameof(Personnage.WindowSwitcher) ||
                e.PropertyName == nameof(Personnage.EasyTeam))
            {
                if (e.PropertyName == nameof(Personnage.EasyTeam) &&
                    sender is Personnage perso &&
                    !perso.EasyTeam &&
                    SelectedLeader?.Value == perso)
                {
                    SelectedLeader = LeaderOptions[0];
                }

                UpdateGlobalStates();
                UpdateAllActiveScripts();
            }
        }

        private void Personnages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateGlobalStates();

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
                UpdateAllActiveScripts();
                UpdateLeaderOptions();
            }

            if (e.NewItems != null)
            {
                foreach (Personnage personnage in e.NewItems)
                    personnage.PropertyChanged += Personnage_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (Personnage personnage in e.OldItems)
                    personnage.PropertyChanged -= Personnage_PropertyChanged;
            }
        }

        private void CharactersViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CharactersViewModel.IsMouseCloneGlobalChecked) ||
                e.PropertyName == nameof(CharactersViewModel.IsHotkeyCloneGlobalChecked) ||
                e.PropertyName == nameof(CharactersViewModel.IsWindowSwitcherGlobalChecked) ||
                e.PropertyName == nameof(CharactersViewModel.IsEasyTeamGlobalChecked))
            {
                UpdateGlobalStates();
            }
        }

        #endregion

        #region Méthodes - Refresh

        private void ExecuteRefresh()
        {
            CharactersViewModel.LoadWindows();

            foreach (var personnage in CharactersViewModel.Personnages)
            {
                personnage.PropertyChanged -= Personnage_PropertyChanged;
                personnage.PropertyChanged += Personnage_PropertyChanged;
            }

            UpdateLeaderOptions();
            UpdateGlobalStates();
            UpdateAllActiveScripts();
        }

        #endregion

        #region Méthodes - Lancement rapide

        private bool CanExecuteQuickLaunch() => CanUseQuickLaunch;

        /// <summary>
        /// Bascule entre "Lancement rapide" (tout activer) et "Tout arrêter" (tout désactiver).
        /// </summary>
        private void ExecuteQuickLaunch()
        {
            if (CharactersViewModel.Personnages.Count < 2)
                return;

            if (IsInStopMode())
                ExecuteStopAll();
            else
                ExecuteStartAll();
        }

        /// <summary>
        /// Retourne true si toutes les conditions sont remplies pour afficher "Tout arrêter".
        /// </summary>
        private bool IsInStopMode()
        {
            bool mcConditionMet = CharactersViewModel.Personnages.Count(p => p.MouseClone) >= 2 && IsMouseCloneEnabled;
            bool hcConditionMet = CharactersViewModel.Personnages.Count(p => p.HotkeyClone) >= 2 && IsHotkeyCloneEnabled;
            bool wsConditionMet = CharactersViewModel.Personnages.Count(p => p.WindowSwitcher) >= 2 && IsWindowSwitcherEnabled;

            return mcConditionMet && hcConditionMet && wsConditionMet;
        }

        /// <summary>
        /// Désactive tous les toggles individuels et globaux, conserve le leader Easy Team.
        /// </summary>
        private void ExecuteStopAll()
        {
            var currentLeader = SelectedLeader;

            CharactersViewModel.BeginBatchUpdate();

            foreach (var personnage in CharactersViewModel.Personnages)
            {
                personnage.MouseClone = false;
                personnage.HotkeyClone = false;
                personnage.WindowSwitcher = false;

                if (currentLeader?.Value != personnage)
                    personnage.EasyTeam = false;
            }

            IsMouseCloneEnabled = false;
            IsHotkeyCloneEnabled = false;
            IsWindowSwitcherEnabled = false;

            SelectedLeader = currentLeader;

            CharactersViewModel.IsMouseCloneGlobalChecked = false;
            CharactersViewModel.IsHotkeyCloneGlobalChecked = false;
            CharactersViewModel.IsWindowSwitcherGlobalChecked = false;
            CharactersViewModel.IsEasyTeamGlobalChecked = CharactersViewModel.Personnages.Any(p => p.EasyTeam);

            CharactersViewModel.EndBatchUpdate();
        }

        /// <summary>
        /// Active tous les toggles individuels et globaux.
        /// </summary>
        private void ExecuteStartAll()
        {
            foreach (var personnage in CharactersViewModel.Personnages)
            {
                personnage.MouseClone = true;
                personnage.HotkeyClone = true;
                personnage.WindowSwitcher = true;
                personnage.EasyTeam = true;
            }

            IsMouseCloneEnabled = true;
            IsHotkeyCloneEnabled = true;
            IsWindowSwitcherEnabled = true;

            CharactersViewModel.IsMouseCloneGlobalChecked = true;
            CharactersViewModel.IsHotkeyCloneGlobalChecked = true;
            CharactersViewModel.IsWindowSwitcherGlobalChecked = true;
            CharactersViewModel.IsEasyTeamGlobalChecked = true;
        }

        #endregion

        #region Méthodes - Easy Team

        private void InitializeLeaderOptions()
        {
            LeaderOptions = new ObservableCollection<LeaderOption>
            {
                new LeaderOption { DisplayName = "Définir le chef d'équipe", Value = null }
            };

            SelectedLeader = LeaderOptions[0];
        }

        private void UpdateLeaderOptions()
        {
            var currentLeader = SelectedLeader;

            LeaderOptions.Clear();
            LeaderOptions.Add(new LeaderOption { DisplayName = "Définir le chef d'équipe", Value = null });

            foreach (var personnage in CharactersViewModel.Personnages)
                LeaderOptions.Add(new LeaderOption { DisplayName = personnage.CharacterName, Value = personnage });

            if (currentLeader?.Value != null)
            {
                var matchingLeader = LeaderOptions.FirstOrDefault(l => l.Value?.CharacterName == currentLeader.Value.CharacterName);
                SelectedLeader = matchingLeader ?? LeaderOptions[0];
            }
            else
            {
                SelectedLeader = LeaderOptions[0];
            }
        }

        private void OnLeaderChanged()
        {
            if (SelectedLeader?.Value != null && !SelectedLeader.Value.EasyTeam)
                SelectedLeader.Value.EasyTeam = true;

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateEasyTeamButtonState();
                UpdateEasyTeamScript();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateEasyTeamButtonState()
        {
            int activeCount = CharactersViewModel.Personnages.Count(p => p.EasyTeam);
            bool hasValidLeader = SelectedLeader?.Value != null;

            IsEasyTeamButtonEnabled = activeCount >= 2 && hasValidLeader;

            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private bool CanExecuteEasyTeam() => IsEasyTeamButtonEnabled;

        public void ExecuteEasyTeam()
        {
            if (SelectedLeader?.Value == null)
            {
                System.Windows.MessageBox.Show("Veuillez sélectionner un chef d'équipe valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _scriptExecutionService.ExecuteEasyTeam(BuildAhkData());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'exécution du script Easy Team : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEasyTeamScript()
        {
            if (IsEasyTeamButtonEnabled)
                _scriptExecutionService.UpdateEasyTeamScript(BuildAhkData());
        }

        #endregion

        #region Méthodes - États globaux

        private void UpdateGlobalStates()
        {
            int activeMC = CharactersViewModel.Personnages.Count(p => p.MouseClone);
            CanEnableMouseClone = activeMC >= 2;
            if (!CanEnableMouseClone && IsMouseCloneEnabled)
                IsMouseCloneEnabled = false;

            int activeHC = CharactersViewModel.Personnages.Count(p => p.HotkeyClone);
            CanEnableHotkeyClone = activeHC >= 2;
            if (!CanEnableHotkeyClone && IsHotkeyCloneEnabled)
                IsHotkeyCloneEnabled = false;

            int activeWS = CharactersViewModel.Personnages.Count(p => p.WindowSwitcher);
            CanEnableWindowSwitcher = activeWS >= 2;
            if (!CanEnableWindowSwitcher && IsWindowSwitcherEnabled)
                IsWindowSwitcherEnabled = false;

            UpdateEasyTeamButtonState();

            CanUseQuickLaunch = CharactersViewModel.Personnages.Count >= 2;
            IsQuickLaunchMode = !IsInStopMode();

            OnPropertyChanged(nameof(CanEnableAutoFollow));
        }

        private void UpdateMouseCloneAutoFollowShortcut()
        {
            MouseCloneAutoFollowShortcut = MouseCloneShortcut == "Raccourci non défini"
                ? "Alt+[Raccourci MC]"
                : $"Alt+{MouseCloneShortcut}";
        }

        private void UpdateHotkeyCloneAutoFollowShortcut()
        {
            HotkeyCloneAutoFollowShortcut = HotkeyCloneShortcut == "Raccourci non défini"
                ? "Alt+[Raccourci HC]"
                : $"Alt+{HotkeyCloneShortcut}";
        }

        #endregion

        #region Méthodes - Scripts Mouse Clone

        private void StartMouseClone()
        {
            try
            {
                _scriptExecutionService.StartMouseClone(BuildAhkData());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du démarrage de Mouse Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                IsMouseCloneEnabled = false;
            }
        }

        private void StopMouseClone() => _scriptExecutionService.StopMouseClone();

        private void UpdateMouseCloneScriptIfActive()
        {
            if (IsMouseCloneEnabled)
                _scriptExecutionService.StartMouseClone(BuildAhkData());
        }

        #endregion

        #region Méthodes - Scripts Hotkey Clone

        private void StartHotkeyClone()
        {
            try
            {
                _scriptExecutionService.StartHotkeyClone(BuildAhkData());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du démarrage de Hotkey Clone : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                IsHotkeyCloneEnabled = false;
            }
        }

        private void StopHotkeyClone() => _scriptExecutionService.StopHotkeyClone();

        private void UpdateHotkeyCloneScriptIfActive()
        {
            if (IsHotkeyCloneEnabled)
                _scriptExecutionService.StartHotkeyClone(BuildAhkData());
        }

        #endregion

        #region Méthodes - Scripts Window Switcher

        private void StartWindowSwitcher()
        {
            try
            {
                _scriptExecutionService.StartWindowSwitcher(BuildAhkData());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du démarrage de Window Switcher : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                IsWindowSwitcherEnabled = false;
            }
        }

        private void StopWindowSwitcher() => _scriptExecutionService.StopWindowSwitcher();

        private void UpdateWindowSwitcherScriptIfActive()
        {
            if (IsWindowSwitcherEnabled)
                _scriptExecutionService.StartWindowSwitcher(BuildAhkData());
        }

        #endregion

        #region Méthodes - Mise à jour des scripts

        private void UpdateAllActiveScripts()
        {
            UpdateMouseCloneScriptIfActive();
            UpdateHotkeyCloneScriptIfActive();
            UpdateWindowSwitcherScriptIfActive();
            UpdateEasyTeamScript();
        }

        #endregion

        #region Méthodes - Construction des données AHK

        /// <summary>
        /// Construit AhkData directement depuis l'état du ViewModel.
        /// Plus de dictionnaire intermédiaire — typage fort, zéro boxing/unboxing.
        /// </summary>
        private AhkData BuildAhkData()
        {
            var personnages = CharactersViewModel.Personnages;

            return new AhkData
            {
                WindowTitles = personnages.Select(p => p.WindowName).ToArray(),
                IsOptimizerVisible = System.Windows.Application.Current.MainWindow?.IsVisible ?? true,

                // Setup
                SpeedDelay = SpeedDelay,

                // Mouse Clone
                MouseCloneEnabled = IsMouseCloneEnabled,
                MouseCloneShortcut = AhkConverter.FormatShortcut(MouseCloneShortcut),
                MouseCloneDelays = MouseCloneDelaysEnabled,
                MouseCloneMinDelay = MouseCloneMinDelay,
                MouseCloneMaxDelay = MouseCloneMaxDelay,
                MouseCloneLayout = AhkConverter.ConvertLayout(MouseCloneLayoutIndex == 0 ? "Fenêtre unique" : "Fenêtres individuelles"),
                ActiveWindows_MC = personnages.Where(p => p.MouseClone).Select(p => p.WindowName).ToHashSet(),

                // Mouse Clone + AutoFollow
                MouseCloneAutoFollowEnabled = IsMouseCloneAutoFollowEnabled && CanEnableAutoFollow,
                MouseCloneAutoFollowShortcut = MouseCloneAutoFollowShortcut,

                // Hotkey Clone
                HotkeyCloneEnabled = IsHotkeyCloneEnabled,
                HotkeyCloneShortcut = AhkConverter.FormatShortcut(HotkeyCloneShortcut),
                HotkeyCloneDelays = HotkeyCloneDelaysEnabled,
                HotkeyCloneMinDelay = HotkeyCloneMinDelay,
                HotkeyCloneMaxDelay = HotkeyCloneMaxDelay,
                ActiveWindows_HC = personnages.Where(p => p.HotkeyClone).Select(p => p.WindowName).ToHashSet(),

                // Hotkey Clone + AutoFollow
                HotkeyCloneAutoFollowEnabled = IsHotkeyCloneAutoFollowEnabled && CanEnableAutoFollow,
                HotkeyCloneAutoFollowShortcut = HotkeyCloneAutoFollowShortcut,

                // Window Switcher
                WindowSwitcherEnabled = IsWindowSwitcherEnabled,
                WindowSwitcherShortcut = AhkConverter.FormatShortcut(WindowSwitcherShortcut),
                ActiveWindows_WS = personnages.Where(p => p.WindowSwitcher).Select(p => p.WindowName).ToHashSet(),

                // Easy Team
                EasyTeamLeaderWindow = SelectedLeader?.Value?.WindowName,
                ActiveWindows_ET = personnages.Where(p => p.EasyTeam).Select(p => p.WindowName).ToHashSet(),

                // AutoFollow
                AutoFollowShortcut = AutoFollowShortcut
            };
        }

        #endregion

        #region Méthodes - Interface

        private void ExecuteToggleSettingsPanel()
        {
            IsSettingsPanelExpanded = !IsSettingsPanelExpanded;
        }

        #endregion

        #region Méthodes - Paramètres

        private void LoadSettings()
        {
            _isLoadingSettings = true;

            try
            {
                var settings = _settingsService.LoadSettings();

                ExecutionSpeedIndex = settings.ExecutionSpeedIndex;

                MouseCloneShortcut = settings.MouseCloneShortcut;
                MouseCloneDelaysEnabled = settings.MouseCloneDelaysEnabled;
                MouseCloneMinDelay = settings.MouseCloneMinDelay;
                MouseCloneMaxDelay = settings.MouseCloneMaxDelay;
                MouseCloneLayoutIndex = settings.MouseCloneLayoutIndex;

                HotkeyCloneShortcut = settings.HotkeyCloneShortcut;
                HotkeyCloneDelaysEnabled = settings.HotkeyCloneDelaysEnabled;
                HotkeyCloneMinDelay = settings.HotkeyCloneMinDelay;
                HotkeyCloneMaxDelay = settings.HotkeyCloneMaxDelay;

                WindowSwitcherShortcut = settings.WindowSwitcherShortcut;

                if (!string.IsNullOrEmpty(settings.SelectedLeaderName))
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var leader = LeaderOptions.FirstOrDefault(l => l.Value?.CharacterName == settings.SelectedLeaderName);
                        if (leader != null)
                            SelectedLeader = leader;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                IsMouseCloneAutoFollowEnabled = settings.IsMouseCloneAutoFollowEnabled;
                IsHotkeyCloneAutoFollowEnabled = settings.IsHotkeyCloneAutoFollowEnabled;
                AutoFollowShortcut = settings.AutoFollowShortcut;

                IsSettingsPanelExpanded = settings.IsSettingsPanelExpanded;
                WindowLeft = settings.WindowLeft;
                WindowTop = settings.WindowTop;
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        #endregion

        #region Sauvegarde des paramètres

        /// <summary>
        /// Construit l'objet AppSettings à partir de l'état courant du ViewModel.
        /// </summary>
        private AppSettings BuildAppSettings() => new AppSettings
        {
            ExecutionSpeedIndex = ExecutionSpeedIndex,

            MouseCloneShortcut = MouseCloneShortcut,
            MouseCloneDelaysEnabled = MouseCloneDelaysEnabled,
            MouseCloneMinDelay = MouseCloneMinDelay,
            MouseCloneMaxDelay = MouseCloneMaxDelay,
            MouseCloneLayoutIndex = MouseCloneLayoutIndex,

            HotkeyCloneShortcut = HotkeyCloneShortcut,
            HotkeyCloneDelaysEnabled = HotkeyCloneDelaysEnabled,
            HotkeyCloneMinDelay = HotkeyCloneMinDelay,
            HotkeyCloneMaxDelay = HotkeyCloneMaxDelay,

            WindowSwitcherShortcut = WindowSwitcherShortcut,

            SelectedLeaderName = SelectedLeader?.Value?.CharacterName,

            IsMouseCloneAutoFollowEnabled = IsMouseCloneAutoFollowEnabled,
            IsHotkeyCloneAutoFollowEnabled = IsHotkeyCloneAutoFollowEnabled,
            AutoFollowShortcut = AutoFollowShortcut,

            IsSettingsPanelExpanded = IsSettingsPanelExpanded,
            WindowLeft = System.Windows.Application.Current.MainWindow?.Left,
            WindowTop = System.Windows.Application.Current.MainWindow?.Top
        };

        /// <summary>
        /// Déclenche une sauvegarde différée de 500ms (debounce).
        /// </summary>
        private void SaveSettingsNow()
        {
            if (_isLoadingSettings || _isInitializing)
                return;

            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        #endregion

        #region Méthodes - Fermeture

        private bool _isShuttingDown = false;

        public void Shutdown()
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;

            _saveDebounceTimer.Stop();
            _scriptExecutionService?.StopAllScripts();

            var settings = BuildAppSettings();
            settings.WindowLeft = WindowLeft;
            settings.WindowTop = WindowTop;

            _settingsService.SaveSettings(settings);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}