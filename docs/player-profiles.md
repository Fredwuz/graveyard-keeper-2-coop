# Player profiles

Each joiner keeps their own inventory, tool belt, money, energy, health, talents, look and
position from one session to the next. The host stores them; the game's save files are not
changed.

## Why

A joiner loads a copy of the host's save, and a save holds one player: the host. Without
profiles every joiner started as a copy of the host, with the host's inventory and money.

## How it works

- **Identity** is `[Session] PlayerKey`: 32 hex digits, made once per installation. Not the
  Netcode client id, which changes with every connection. If two connected joiners send the
  same key (a game folder copied to a friend), the host answers `KeyInUse` and the second makes
  a new key.
- **Hello.** Once in the world, a joiner sends its key and whether it plays in a world copy
  (slot name starts with `GK2Coop_`). The host answers with one of:
  - `KeepOwn`: the joiner plays its own save, keeps what it has, and still uploads;
  - `Stored`: the stored profile, which the joiner restores;
  - `NewPlayer`: no profile yet; the joiner gets the game's new-player start.
- **Restore** copies values into the live objects (the game's UI holds references to them):
  inventory, tool belt, money, energy, insanity (plus the action gems when the host does not
  [pool them](knowledge.md)), health, tutorial flags, talents, look. A returning player goes back
  to their last position when it is in the scene the copy starts in.
- **Upload.** Every 30 s, about 1 s after the joiner's own inventory changes, and when the game
  quits. Only after a successful restore: a failed restore leaves the host's copy in the
  joiner's bag, and uploading that would overwrite the real profile.
- **Storage.** The host checks that an upload deserializes before it replaces the file
  (`.tmp`, then `File.Replace`):
  `BepInEx\config\GK2Coop\players\<host save slot>\<key>.dat`, plus `<key>.txt` with the
  player's name and the time.
- **Host's shadow copy.** The host also copies each joiner's inventory into its own record of
  that joiner, so [drop pickups](drops.md) are checked against the space the joiner really has.

## Format

Gzip of the game's Odin save serializer ([CoopGameSerializer.cs](../src/GK2Coop/CoopGameSerializer.cs)),
an `object[]` of the `PlayerData` and the talent data. Profiles from before 0.36.0
(`LazySerializer`, player only) still load. At most 4 MB packed, 64 MB unpacked
([Gzip.cs](../src/GK2Coop/Gzip.cs)), sent in 1 KB chunks.

## Files

[CoopPlayerProfiles.cs](../src/GK2Coop/CoopPlayerProfiles.cs).

## Messages

| Name | Direction | Content |
| --- | --- | --- |
| `GK2Coop.Profile.Hello.v1` | joiner → host | key, plays in a copy |
| `GK2Coop.Profile.Data.v1` | both | mode, transfer id, chunk index and count, bytes |

## Settings

`[Coop] KeepPlayerProgress` (true), `[Session] PlayerKey` (made automatically).

## Tests

Regression runs `menu`, `rejoin`; `mix3` (three players, test copies sharing one key).
