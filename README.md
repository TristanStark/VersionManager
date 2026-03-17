# VersionManager

Application desktop **WPF / C#** de gestion de versions, conçue sans bibliothèque externe, avec architecture **MVVM**.  
L’outil permet de consulter l’état courant d’une API de versioning, visualiser l’historique des versions, inspecter le contenu d’archives ZIP et préparer la création d’une nouvelle version à partir d’un package d’update.

---

## Objectif du projet

Cette application a pour but de fournir une interface simple et robuste pour un workflow de publication basé sur des archives ZIP :

- récupérer le **dernier numéro de version** via API
- consulter l’**historique des versions**
- sélectionner un **ZIP local d’update**
- visualiser l’**arborescence interne du ZIP**
- inspecter les détails d’un fichier contenu dans l’archive
- préparer un workflow de **création d’une nouvelle version**
- à terme :
  - télécharger la version courante depuis l’API
  - fusionner le contenu avec le ZIP d’update
  - demander un nouveau numéro de version
  - recréer une archive finale
  - envoyer cette archive à l’API

---

## Fonctionnalités actuelles

### Interface principale
- affichage du dernier numéro de version
- affichage du statut de connexion API
- historique des versions au centre via `DataGrid`
- panneau latéral avec informations sur le ZIP sélectionné
- visualisation hiérarchique du contenu ZIP via `TreeView`
- panneau de détails du fichier sélectionné
- barre de logs et progression en bas de fenêtre

### ZIP
- sélection d’un fichier `.zip`
- lecture des entrées de l’archive
- reconstruction d’une hiérarchie de dossiers/fichiers
- calcul du SHA-256 d’un fichier sélectionné
- affichage :
  - nom
  - chemin interne
  - type
  - taille
  - date de modification

### MVVM
- séparation claire entre :
  - **Views**
  - **ViewModels**
  - **Models**
  - **Services**
- commandes WPF
- état observable avec `INotifyPropertyChanged`

---

## Fonctionnalités prévues

- appel API réel avec `HttpClient`
- téléchargement du ZIP de la version courante
- extraction temporaire des archives
- fusion du ZIP courant et du ZIP d’update
- détection des fichiers :
  - ajoutés
  - modifiés
  - supprimés
- dialogue de saisie du nouveau numéro de version
- re-création du ZIP final
- envoi à l’API
- filtre/recherche dans l’historique
- filtre/recherche dans l’arborescence ZIP
- coloration visuelle des différences
- comparaison entre deux versions
- gestion d’erreurs plus détaillée
- annulation des opérations longues

---

## Stack technique

- **.NET / C#**
- **WPF**
- **MVVM**
- **System.IO.Compression.ZipArchive**
- **SHA256** via `System.Security.Cryptography`
- **OpenFileDialog** natif Windows

Aucune bibliothèque externe n’est nécessaire.

---

## Structure du projet

```text
VersionManager/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Models/
│   ├── VersionInfo.cs
│   ├── WorkflowStep.cs
│   ├── ZipEntryInfo.cs
│   ├── FileDetailInfo.cs
│   └── ApiStatusInfo.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IApiService.cs
│   │   └── IZipService.cs
│   ├── ApiService.cs
│   └── ZipService.cs
│
├── ViewModels/
│   ├── Base/
│   │   ├── ViewModelBase.cs
│   │   ├── RelayCommand.cs
│   │   └── AsyncRelayCommand.cs
│   ├── MainViewModel.cs
│   └── ZipTreeNodeViewModel.cs
│
└── Helpers/
    └── FileSizeHelper.cs
```

---

## Architecture

### 1. Models
Les modèles représentent les données métiers brutes.

#### `VersionInfo`
Décrit une version publiée :
- numéro de version
- date de création
- auteur
- description
- statut
- taille du ZIP

#### `ZipEntryInfo`
Décrit une entrée d’archive :
- nom
- chemin complet
- taille
- date de modification
- dossier ou fichier

#### `FileDetailInfo`
Décrit les informations détaillées d’un fichier sélectionné :
- nom
- chemin
- taille
- hash SHA-256
- type
- date

#### `WorkflowStep`
Représente une étape métier du workflow de publication.

#### `ApiStatusInfo`
Regroupe les informations de connexion à l’API.

### 2. ViewModels

#### `MainViewModel`
Point central de l’application.  
Il orchestre :
- chargement des données API
- historique des versions
- sélection d’un ZIP
- construction de l’arborescence
- gestion de la sélection dans le `TreeView`
- mise à jour des détails du fichier
- logs
- progression

#### `ZipTreeNodeViewModel`
Représente un nœud hiérarchique dans l’arbre ZIP :
- nom
- chemin complet
- type fichier/dossier
- taille
- date
- enfants
- état de sélection
- état d’expansion

### 3. Services

#### `IApiService` / `ApiService`
Abstraction des appels API.  
Actuellement, l’implémentation est simulée.

Rôle visé :
- récupérer le statut de l’API
- récupérer la dernière version
- récupérer l’historique
- télécharger une archive
- envoyer une nouvelle archive

#### `IZipService` / `ZipService`
Responsable de toute la logique ZIP :
- lecture des entrées
- construction d’un arbre hiérarchique
- extraction des détails d’un fichier
- calcul de hash

### 4. Views

#### `MainWindow.xaml`
Fenêtre principale de l’application.

Elle contient :
- un bandeau supérieur avec actions
- un panneau gauche d’informations
- un panneau central pour l’historique
- un panneau droit pour l’exploration du ZIP
- une zone inférieure de logs/progression

---

## Interface utilisateur

### Bandeau supérieur
- titre de l’application
- dernière version connue
- bouton de rafraîchissement
- bouton de sélection de ZIP
- bouton de création de nouvelle version

### Colonne gauche
- état API
- informations sur le ZIP choisi
- étapes du workflow

### Zone centrale
- historique des versions
- future zone de recherche/filtre

### Colonne droite
- arborescence du ZIP via `TreeView`
- détail du nœud sélectionné

### Bas de fenêtre
- progression
- logs d’exécution

---

## Workflow cible

Le workflow complet visé est le suivant :

1. récupérer la dernière version depuis l’API
2. sélectionner un ZIP local contenant les updates
3. télécharger le ZIP de la version actuelle
4. extraire les deux archives dans des répertoires temporaires
5. appliquer les modifications du ZIP d’update sur la version courante
6. demander à l’utilisateur le nouveau numéro de version
7. recréer une archive ZIP finale
8. envoyer l’archive à l’API
9. rafraîchir l’historique

---

## Exemple de comportement actuel

### Chargement
Au lancement :
- l’application interroge le service API simulé
- charge la dernière version
- charge l’historique
- initialise le workflow

### Sélection d’un ZIP
Lorsqu’un utilisateur clique sur **Parcourir ZIP** :
- une boîte de dialogue s’ouvre
- le ZIP est lu
- les entrées sont analysées
- l’arborescence est reconstruite
- le nombre de fichiers et la taille sont affichés
- le panneau de droite montre le contenu de l’archive

### Sélection d’un fichier
Lorsqu’un fichier est sélectionné dans l’arbre :
- les détails sont mis à jour
- le hash SHA-256 est calculé
- les métadonnées sont affichées

---

## Choix techniques

### Pourquoi WPF ?
WPF est un bon compromis pour une application métier Windows :
- interface riche
- `DataGrid`
- `TreeView`
- binding puissant
- support naturel du pattern MVVM

### Pourquoi MVVM ?
MVVM permet de :
- séparer l’UI de la logique
- rendre le code plus maintenable
- faciliter les évolutions futures
- isoler les services techniques

### Pourquoi `ZipArchive` natif ?
L’objectif du projet est de rester sans dépendances externes.  
`System.IO.Compression.ZipArchive` suffit pour :
- lire
- parcourir
- inspecter
- recréer des ZIP

---

## Lancement du projet

### Prérequis
- Windows
- .NET SDK compatible avec le projet
- Visual Studio 2022 recommandé avec workload WPF

### Exécution
1. ouvrir la solution dans Visual Studio
2. définir `VersionManager` comme projet de démarrage
3. lancer l’application

---

## Améliorations recommandées

### Court terme
- ajouter un vrai `Dialog` pour le nouveau numéro de version
- remplacer le faux `ApiService` par un client HTTP réel
- enrichir les logs
- améliorer la gestion des erreurs

### Moyen terme
- comparaison entre ZIP source et ZIP final
- coloration des différences dans le `TreeView`
- recherche dans l’historique
- recherche dans le ZIP
- support des opérations longues avec annulation

### Long terme
- gestion multi-environnements
- publication signée
- historique détaillé des changements par fichier
- système d’authentification API
- export de rapports

---

## Limites actuelles

- l’API est simulée
- le workflow de création de version n’est pas encore branché de bout en bout
- la recherche dans l’historique n’est pas encore implémentée
- il n’y a pas encore de comparaison visuelle entre versions
- pas encore de mécanisme de rollback
- l’application est actuellement pensée pour Windows uniquement

---

## Idées d’évolution UX

- indicateurs visuels des fichiers :
  - ajoutés
  - modifiés
  - supprimés
- badges de statut dans l’historique
- preview texte pour les fichiers `.txt`, `.json`, `.xml`, `.config`
- drag & drop de ZIP directement dans la fenêtre
- panneau de différences avant envoi

---

## Convention de développement

- éviter de mettre de la logique métier dans le code-behind
- réserver le code-behind aux interactions purement UI
- centraliser les opérations ZIP dans `ZipService`
- centraliser les appels réseau dans `ApiService`
- garder `MainViewModel` comme orchestrateur, pas comme service technique

---

## Exemple de prochaine étape de refactor

La suite logique est d’introduire :

- `IVersionBuildService`
- `IDialogService`

### `IVersionBuildService`
Responsable du workflow complet :
- téléchargement version courante
- extraction
- fusion
- zippage final

### `IDialogService`
Responsable des fenêtres modales :
- saisie du nouveau numéro de version
- messages de confirmation
- erreurs

---

## Auteur

Projet de base WPF/MVVM pour gestion de versions par archives ZIP.
