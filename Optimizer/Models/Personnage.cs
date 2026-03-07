using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Optimizer.Models
{
    public class Personnage : INotifyPropertyChanged
    {
        #region Champs privés

        private string _windowName = string.Empty;
        private string _characterName = string.Empty;
        private bool _mouseClone;
        private bool _hotkeyClone;
        private bool _windowSwitcher;
        private bool _easyTeam;
        private int _order;
        private IntPtr _handle;
        private bool _isSelected;

        #endregion

        #region Propriétés publiques

        public string WindowName
        {
            get => _windowName;
            set
            {
                if (_windowName == value) return;
                _windowName = value;
                OnPropertyChanged();
            }
        }

        public string CharacterName
        {
            get => _characterName;
            set
            {
                if (_characterName == value) return;
                _characterName = value;
                OnPropertyChanged();
            }
        }

        public bool MouseClone
        {
            get => _mouseClone;
            set
            {
                if (_mouseClone == value) return;
                _mouseClone = value;
                OnPropertyChanged();
            }
        }

        public bool HotkeyClone
        {
            get => _hotkeyClone;
            set
            {
                if (_hotkeyClone == value) return;
                _hotkeyClone = value;
                OnPropertyChanged();
            }
        }

        public bool WindowSwitcher
        {
            get => _windowSwitcher;
            set
            {
                if (_windowSwitcher == value) return;
                _windowSwitcher = value;
                OnPropertyChanged();
            }
        }

        public bool EasyTeam
        {
            get => _easyTeam;
            set
            {
                if (_easyTeam == value) return;
                _easyTeam = value;
                OnPropertyChanged();
            }
        }

        public int Order
        {
            get => _order;
            set
            {
                if (_order == value) return;
                _order = value;
                OnPropertyChanged();
            }
        }

        public IntPtr Handle
        {
            get => _handle;
            set
            {
                if (_handle == value) return;
                _handle = value;
                // Pas de OnPropertyChanged — Handle n'est pas bindé dans l'UI
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
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