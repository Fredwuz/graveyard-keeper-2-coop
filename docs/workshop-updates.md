# Workshop updates

A player who subscribed to the mod on the Steam Workshop gets each new version installed when
the game starts; it is active from the next launch.

## Why

The game's own mod system loads only language and voice packs. Steam downloads a Workshop item
to `steamapps\workshop\content\4358690\<id>`, and nothing runs it.

## How it works

[CoopWorkshopUpdater.cs](../src/GK2Coop/CoopWorkshopUpdater.cs), run from `Plugin.Awake`:

1. Only when the plugin lives in its own `BepInEx\plugins\GK2Coop` folder.
2. Removes `.old` files left by the last update.
3. Source: `<library>\steamapps\workshop\content\4358690\<item>\BepInEx\plugins\GK2Coop`, next to
   the game's own library folder. The item is `PublishedItemId` (3811794693), pinned in code:
   anyone can publish an item with a `GK2Coop.dll`, so other items are never trusted.
4. Compares the DLL versions (`AssemblyName.GetAssemblyName`, the csproj `<Version>`). Only a
   newer one is copied.
5. Copies every file into the plugin folder. Each existing file is first renamed to `.old`
   (Mono locks the running DLL but allows a rename). Each copy is checked by SHA-256. On any
   failure, everything is put back.
6. The main menu says "installed, restart" or why it failed.

Only the plugin's own folder is written, never BepInEx or the player's settings.

## Settings

`[Updates] AutoUpdateFromWorkshop` (true), `WorkshopItemId` (0 = the pinned item; another id
replaces this mod's code with that item's files, so only for a trusted item);
`[Testing] WorkshopFolderOverride` (tests: read updates from this folder).

## Tests

`tests/GameplayProbe/Verify-WorkshopUpdate.ps1` (a Steam library layout on a test copy).
