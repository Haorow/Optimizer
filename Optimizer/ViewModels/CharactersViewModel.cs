using Optimizer.Models;
using Optimizer.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Optimizer.ViewModels
{
    public class CharactersViewModel : INotifyPropertyChanged
    {
        #region Services

        private readonly WindowDiscoveryService _windowDiscoveryService;

        /// <summary>
        /// Callback injecté par MainViewModel pour récupérer le leader actuel.
        /// Évite tout couplage avec la View (Application.Current.MainWindow).
        /// </summary>
        private readonly Func<Personnage?> _getCurrentLeader;

        #endregion

        #region Propriétés

        public ObservableCollection<Personnage> Personnages { get; set; }

        // Gestion du toggle button d'en-tête de colonne pour MC
        private bool _isMouseCloneGlobalChecked;
        public bool IsMouseCloneGlobalChecked
        {
            get => _isMouseCloneGlobalChecked;
            set
            {
                _isMouseCloneGlobalChecked = value;
                OnPropertyChanged();
                UpdateMouseCloneForAll(value);
            }
        }

        // Gestion du toggle button d'en-tête de colonne pour HC
        private bool _isHotkeyCloneGlobalChecked;
        public bool IsHotkeyCloneGlobalChecked
        {
            get => _isHotkeyCloneGlobalChecked;
            set
            {
                _isHotkeyCloneGlobalChecked = value;
                OnPropertyChanged();
                UpdateHotkeyCloneForAll(value);
            }
        }

        // Gestion du toggle button d'en-tête de colonne pour WS
        private bool _isWindowSwitcherGlobalChecked;
        public bool IsWindowSwitcherGlobalChecked
        {
            get => _isWindowSwitcherGlobalChecked;
            set
            {
                _isWindowSwitcherGlobalChecked = value;
                OnPropertyChanged();
                UpdateWindowSwitcherForAll(value);
            }
        }

        // Gestion du toggle button d'en-tête de colonne pour ET
        private bool _isUpdatingEasyTeamGlobal = false;
        private bool _isEasyTeamGlobalChecked;
        public bool IsEasyTeamGlobalChecked
        {
            get => _isEasyTeamGlobalChecked;
            set
            {
                if (_isUpdatingEasyTeamGlobal)
                {
                    _isEasyTeamGlobalChecked = value;
                    OnPropertyChanged();
                    return;
                }

                _isEasyTeamGlobalChecked = value;
                OnPropertyChanged();
                UpdateEasyTeamForAll(value);
            }
        }

        public void BeginBatchUpdate() => _isUpdatingEasyTeamGlobal = true;
        public void EndBatchUpdate() => _isUpdatingEasyTeamGlobal = false;

        #endregion

        #region Commandes

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur avec injection du service de découverte et du callback leader.
        /// </summary>
        public CharactersViewModel(WindowDiscoveryService windowDiscoveryService, Func<Personnage?> getCurrentLeader)
        {
            _windowDiscoveryService = windowDiscoveryService;
            _getCurrentLeader = getCurrentLeader;

            Personnages = new ObservableCollection<Personnage>();

            MoveUpCommand = new RelayCommand<Personnage>(MoveUp);
            MoveDownCommand = new RelayCommand<Personnage>(MoveDown);

            LoadWindows();
        }

        #endregion

        #region Méthodes - Mise à jour globale

        private void UpdateMouseCloneForAll(bool isChecked)
        {
            foreach (var personnage in Personnages)
                personnage.MouseClone = isChecked;
        }

        private void UpdateHotkeyCloneForAll(bool isChecked)
        {
            foreach (var personnage in Personnages)
                personnage.HotkeyClone = isChecked;
        }

        private void UpdateWindowSwitcherForAll(bool isChecked)
        {
            foreach (var personnage in Personnages)
                personnage.WindowSwitcher = isChecked;
        }

        private void UpdateEasyTeamForAll(bool isChecked)
        {
            // Récupère le leader via le callback injecté — aucun accès à la View
            var currentLeader = _getCurrentLeader();

            foreach (var personnage in Personnages)
            {
                // Le leader reste toujours coché
                personnage.EasyTeam = (currentLeader != null && personnage == currentLeader) || isChecked;
            }
        }

        #endregion

        #region Méthodes - Chargement et gestion

        /// <summary>
        /// Recharge les fenêtres Dofus via WindowDiscoveryService.
        /// Met à jour les personnages existants et supprime ceux qui ont disparu.
        /// </summary>
        public void LoadWindows()
        {
            var dofusWindows = _windowDiscoveryService.GetDofusWindows();

            foreach (var window in dofusWindows)
            {
                var existing = Personnages.FirstOrDefault(p => p.CharacterName == window.CharacterName);

                if (existing == null)
                {
                    Personnages.Add(new Personnage
                    {
                        Handle = window.Handle,
                        WindowName = window.WindowTitle,
                        CharacterName = window.CharacterName,
                        MouseClone = false,
                        HotkeyClone = false,
                        WindowSwitcher = false,
                        EasyTeam = false
                    });
                }
                else
                {
                    existing.WindowName = window.WindowTitle;
                    existing.Handle = window.Handle;
                }
            }

            // Supprimer les personnages qui ne sont plus dans les fenêtres actuelles
            var activeNames = dofusWindows.Select(w => w.CharacterName).ToHashSet();
            for (int i = Personnages.Count - 1; i >= 0; i--)
            {
                if (!activeNames.Contains(Personnages[i].CharacterName))
                    Personnages.RemoveAt(i);
            }

            UpdateOrder();
        }

        public void MoveUp(Personnage personnage)
        {
            int index = Personnages.IndexOf(personnage);
            if (index > 0)
            {
                Personnages.Move(index, index - 1);
                UpdateOrder();
                OnPropertyChanged(nameof(Personnages));
            }
        }

        public void MoveDown(Personnage personnage)
        {
            int index = Personnages.IndexOf(personnage);
            if (index < Personnages.Count - 1)
            {
                Personnages.Move(index, index + 1);
                UpdateOrder();
                OnPropertyChanged(nameof(Personnages));
            }
        }

        private void UpdateOrder()
        {
            int order = 1;
            foreach (var personnage in Personnages)
                personnage.Order = order++;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}