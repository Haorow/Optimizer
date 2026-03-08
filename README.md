# Optimizer

Optimizer est une application WPF conçue pour le jeu Dofus Unity dont l'objectif est d'améliorer et rendre plus confortable le jeu en multicompte.
Elle complète les fonctionnalités existantes en jeu pour offrir une expérience multicompte plus fluide.

## Fonctionnalités

**Mouse Clone :**
Réplique un clic dans toutes les fenêtres actives, qu'elles soient au premier plan ou non, via un raccourci défini par l'utilisateur.

**Hotkey Clone :**
Enregistre et réplique une saisie dans toutes les fenêtres actives, qu'elles soient au premier plan ou non, via un raccourci défini par l'utilisateur.

**Window Switcher :**
Bascule vers la fenêtre personnage suivant dans la liste via un raccourci défini par l'utilisateur, en suivant l'ordre défini.

**Easy Team :**
Invite automatiquement tous les personnages actifs depuis la fenêtre du chef d'équipe. Supporte également Mouse Clone + AutoFollow et Hotkey Clone + AutoFollow pour regrouper l'équipe après téléportation.

## Installation

Télécharge la dernière release depuis l'onglet [Releases](https://github.com/Haorow/Optimizer/releases), extrais le contenu du zip et lance `Optimizer.exe`.
L'application vérifie automatiquement les mises à jour au démarrage.

## Contribution

Ce projet utilise un fichier `Secrets.cs` non versionné pour stocker le token GitHub utilisé par le système de mise à jour.
Si tu souhaites contribuer, crée un fichier `Secrets.cs` à la racine du projet avec le contenu suivant :

    namespace Optimizer
    {
        internal static class Secrets
        {
            public const string GitHubToken = "ton_token_github";
        }
    }

Un token GitHub avec la permission `public_repo` est suffisant.

## Licence

Ce projet est sous licence [GPL v3](LICENSE) — utilisation et modification libres, redistribution obligatoirement open source.
