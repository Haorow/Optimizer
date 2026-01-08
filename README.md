# Optimizer

Optimizer est une application WPF conçue pour le jeu Dofus Unity dont l'objectif est d'améliorer et rendre plus confortable le jeu en multicompte.
Elle permet de compenser/remplacer temporairement les fonctionnalités existantes en jeu qui provoquent régulièrement des bugs/crashs.
L'application comporte 4 fonctionnalités : Mouse Clone, Hotkey Clone, Window Switcher et Easy Team.

![alt tag](https://private-user-images.githubusercontent.com/44991366/425853065-327fd5d2-d85c-442f-8121-a77b19390a24.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NDMxOTEyNzIsIm5iZiI6MTc0MzE5MDk3MiwicGF0aCI6Ii80NDk5MTM2Ni80MjU4NTMwNjUtMzI3ZmQ1ZDItZDg1Yy00NDJmLTgxMjEtYTc3YjE5MzkwYTI0LnBuZz9YLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFWQ09EWUxTQTUzUFFLNFpBJTJGMjAyNTAzMjglMkZ1cy1lYXN0LTElMkZzMyUyRmF3czRfcmVxdWVzdCZYLUFtei1EYXRlPTIwMjUwMzI4VDE5NDI1MlomWC1BbXotRXhwaXJlcz0zMDAmWC1BbXotU2lnbmF0dXJlPTNhMDljYTU1YTczYzI4MTZhOWYwYjIzMjQyNTIwOWYwODA4YmE2NWRhZjI3ZWZkM2U5YmUxOGNhYzQ0ZWU3MDkmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0In0.NQ7U2L3TGhPB9G0A82_IVAqZ1aFbrlRh8wvB7PRMrJY)

## Présentation des fonctionnalités

Mouse Clone :
Permet de répliquer les clics dans toutes les fenêtres spécifiées, qu'elles soient au premier plan ou non, grâce à un script AutoHotkey ainsi qu'un raccourci défini par l'utilisateur.

Hotkey Clone :
Permet de répliquer les touches dans toutes les fenêtres spécifiées, qu'elles soient au premier plan ou non, grâce à un script AutoHotkey ainsi qu'un raccourci défini par l'utilisateur.

Window Switcher :
Permet de basculer d'une fenêtre du jeu à une autre fenêtre du jeu (équivalent au raccourci Alt+Echap), en suivant l'ordre défini des fenêtres grâce à un raccourci défini par l'utilisateur.

Easy Team :
Permet d'inviter en groupe les personnages correspondants aux fenêtres spécifiées depuis la fenêtre du personnage défini en temps que chef (équivalent à /invite NomDuPersonnage dans le chat du jeu).

## Fonctionnalités à venir

- Amélioration du menu contextuel (possibilité d'activer/désactiver des fonctionnalités)
- Ajout d'un fichier settings.ini pour sauvegarder l'état de l'application à la fermeture et le rétablir à l'ouverture.

## Prérequis

- [Visual Studio Community 2022](https://visualstudio.microsoft.com/fr/vs/community/) (ou une version ultérieure).
- [AutoHotkey v2](https://www.autohotkey.com/) (à inclure dans le projet).
- Le jeu Dofus Unity (DirectX 11) installé.
