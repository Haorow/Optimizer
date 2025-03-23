# Optimizer

Optimizer est une application WPF conçue pour le jeu Dofus Unity dont l'objectif est d'améliorer et rendre plus confortable le jeu en multicompte.
Elle permet de compenser/remplacer temporairement les fonctionnalités existantes en jeu qui provoquent régulièrement des bugs/crashs.
L'application comporte 4 fonctionnalités : Mouse Clone, Hotkey Clone, Window Switcher et Easy Team.

## Présentation des fonctionnalités

Mouse Clone :
Permet de répliquer les clics dans toutes les fenêtres spécifiées, qu'elles soient au premier plan ou non, grâce à un script AutoHotkey ainsi qu'un raccourci défini par l'utilisateur.

Hotkey Clone :
Permet de répliquer les touches dans toutes les fenêtres spécifiées, qu'elles soient au premier plan ou non, grâce à un script AutoHotkey ainsi qu'un raccourci défini par l'utilisateur.

Window Switcher :
Permet de basculer d'une fenêtre du jeu à une autre fenêtre du jeu (équivalent au raccourci Alt+Echap), en suivant l'ordre défini des fenêtres grâce à un raccourci défini par l'utilisateur.

Easy Team :
Permet d'inviter en groupe les personnages correspondants aux fenêtres spécifiées depuis la fenêtre du personnage défini en temps que chef (équivalent à /invite NomDuPersonnage dans le chat du jeu).


## Scripts individuels fonctionnant indépendament de l'application

### Ces scripts sont à modifier et intégrer dans le code selon le code UI et le code behind existant

- Fonctionnalité Mouse Clone
- Fonctionnalité Hotkey Clone
- Fonctionnalité Window Switcher
- Fonctionnalité Easy Team

## Fonctionnalités à venir

- Ajout de la fonctionnalité du bouton Masquer dans la barre des tâches et d'un menu contextuel lors du clic sur l'icone
- Ajout d'un fichier settings.ini pour sauvegarder l'état de l'application à la fermeture

## Prérequis

- [Visual Studio Community 2022](https://visualstudio.microsoft.com/fr/vs/community/) (ou une version ultérieure).
- [AutoHotkey v2](https://www.autohotkey.com/) (à inclure dans le projet).
- Le jeu Dofus Unity (DirectX 11) installé.
