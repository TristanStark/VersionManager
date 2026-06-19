# Configuration Artifactory

Le workflow ajouté sur la branche `feature/artifactory-semver-api` utilise l'organisation suivante :

```text
<URL Artifactory>/<repository>/<prefix optionnel>/<semver>/<zip>
```

Exemple :

```text
https://artifactory.example.com/artifactory/libs-release-local/my-app/1.2.3/package_1.2.3.zip
```

## Paramètres

Les paramètres sont sauvegardés localement dans `settings.json` à côté de l'exécutable.

```json
{
  "ApiUrl": "https://artifactory.example.com/artifactory",
  "RepositoryKey": "libs-release-local",
  "PathPrefix": "my-app",
  "PackageFileName": "package_{version}.zip",
  "Username": "user",
  "AccessToken": "token",
  "AuthMode": "Basic",
  "TimeoutSeconds": 300
}
```

`PackageFileName` peut contenir `{version}`. Par exemple `package_{version}.zip` devient `package_1.2.3.zip`.

## Authentification

Modes supportés :

- `Basic` : envoie `Authorization: Basic base64(username:token)` ; c'est l'équivalent de `curl -u user:token`.
- `Bearer` : envoie `Authorization: Bearer token`.
- `ApiKey` : envoie `X-JFrog-Art-Api: token`.
- `None` : aucune authentification.

Les variables d'environnement suivantes surchargent `settings.json` :

```text
VERSIONMANAGER_ARTIFACTORY_URL
VERSIONMANAGER_ARTIFACTORY_REPOSITORY
VERSIONMANAGER_ARTIFACTORY_PATH_PREFIX
VERSIONMANAGER_ARTIFACTORY_PACKAGE_FILE_NAME
VERSIONMANAGER_ARTIFACTORY_USERNAME
VERSIONMANAGER_ARTIFACTORY_TOKEN
VERSIONMANAGER_ARTIFACTORY_AUTH_MODE
```

## Méthodes principales

`IArtifactoryService` expose :

```csharp
Task<string> DownloadLatestVersionZipAsync(string destinationFolder, CancellationToken cancellationToken = default);
Task<string> DownloadVersionZipAsync(string versionNumber, string destinationFolder, CancellationToken cancellationToken = default);
Task UploadVersionZipAsync(string zipPath, string versionNumber, CancellationToken cancellationToken = default);
Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default);
```

`IVersionBuildService` expose le hook de merge :

```csharp
Task<string> BuildUpdatedZipAsync(
    string currentVersionZipPath,
    string patchZipPath,
    string outputZipPath,
    IProgress<string>? logProgress = null,
    IProgress<double>? valueProgress = null,
    CancellationToken cancellationToken = default);
```

Le corps actuel fait une fusion simple : le ZIP de patch écrase les fichiers du ZIP courant et ajoute les nouveaux fichiers. Remplace ce bloc dans `VersionBuildService.BuildUpdatedZipAsync` par ta logique métier définitive.
