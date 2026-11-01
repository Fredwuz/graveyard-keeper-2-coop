# Build and release

## Build

Needs Windows, the .NET SDK, and Graveyard Keeper 2 with BepInEx installed.

```powershell
dotnet build src\GK2Coop\GK2Coop.csproj -c Release
```

**Where the game is.** [Directory.Build.props](../Directory.Build.props), which every project
here reads, sets `GamePath` to Steam's default library. A game elsewhere goes into `dev.props`
next to it (git ignores it; `Directory.Build.props` shows its content). The scripts' game-folder
parameters default to the same value (`dotnet msbuild … -getProperty:GamePath`); `-p:GamePath=…`
on a build, or the parameter on a script, still overrides it.

The project targets .NET Framework 4.7.2 and references the game's own DLLs from
`<GamePath>\GraveyardKeeper2_Data\Managed` (the demo's data folder also works), and BepInEx from
`artifacts\bepinex-5.4.23.5` when that is there, else from the game folder.
`scripts\Install-Mod.ps1` builds and installs into a game folder.

## Version numbers

| Number | Where | Used for |
| --- | --- | --- |
| Mod version | `Plugin.Version` in [Plugin.cs](../src/GK2Coop/Plugin.cs) **and** `<Version>` in [GK2Coop.csproj](../src/GK2Coop/GK2Coop.csproj) | Handshake message, lobby, menu (`Plugin.Version`); DLL version, Workshop updater (csproj) |
| Protocol | `CoopSession.ProtocolVersion` | Players with different protocols refuse each other |

The two mod versions must be equal; the build target `CheckPluginVersion` fails the build
otherwise. Bump the protocol whenever any `GK2Coop.*` message changes what it carries.

## Release

1. `scripts\Build-Package.ps1` needs BepInEx 5.4.23.5 x64 extracted into
   `artifacts\bepinex-5.4.23.5` (not in git; it goes into the zip). It builds Release and writes `artifacts\GK2Coop-0.1.0-dev.zip` (a
   fixed file name; the version is in the DLL). Contents: BepInEx and the loader, the plugin,
   the licenses in the plugin folder, `INSTALL-GK2COOP.txt` and `GK2Coop-Guides\`. No settings
   file: BepInEx writes the defaults (they match `package\de.fredwuz.gk2coop.cfg`), and an update
   keeps the player's settings. Zip paths use `/`.
2. `scripts\Build-WorkshopItem.ps1` builds the zip, then lays out `artifacts\workshop-item` for
   the game's Workshop uploader: no config folder, the Workshop guide, and the preview picture
   from `media\workshop-preview.jpg`.
3. Player guides: edit [tests/guides.py](../tests/guides.py), then `python tests/Generate-Guides.py`
   writes `package\INSTALL.txt`, `package\WORKSHOP-INSTALL.txt` and `package\guides\*`. Button
   names come from the translation tables.
4. README: update "This version is for Graveyard Keeper 2 version …" when the supported game
   version changes.

Commit messages: `<version>: <what changed for players>`, then a bullet list (see `git log`).
