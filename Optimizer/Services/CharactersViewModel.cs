using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using static Optimizer.Interface;
using System.Runtime.CompilerServices;

namespace Optimizer.Services
{
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
}