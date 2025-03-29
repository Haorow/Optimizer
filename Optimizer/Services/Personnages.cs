using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Optimizer
{
	public class Personnage : INotifyPropertyChanged
	{
		private string _windowName;
		private string _characterName;
		private bool _mouseClone;
		private bool _hotkeyClone;
		private bool _windowSwitcher;
		private bool _easyTeam;
		private int _order;
		private IntPtr _handle;
		private bool _isSelected;

		public string WindowName
		{
			get => _windowName;
			set { _windowName = value; OnPropertyChanged(); }
		}

		public string CharacterName
		{
			get => _characterName;
			set { _characterName = value; OnPropertyChanged(); }
		}

		public bool MouseClone
		{
			get => _mouseClone;
			set { _mouseClone = value; OnPropertyChanged(); }
		}

		public bool HotkeyClone
		{
			get => _hotkeyClone;
			set { _hotkeyClone = value; OnPropertyChanged(); }
		}

		public bool WindowSwitcher
		{
			get => _windowSwitcher;
			set { _windowSwitcher = value; OnPropertyChanged(); }
		}

		public bool EasyTeam
		{
			get => _easyTeam;
			set { _easyTeam = value; OnPropertyChanged(); }
		}

		public int Order
		{
			get => _order;
			set { _order = value; OnPropertyChanged(); }
		}

		public IntPtr Handle
		{
			get => _handle;
			set { _handle = value; OnPropertyChanged(); }
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}