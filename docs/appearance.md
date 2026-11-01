# Appearance

Each player is drawn with their own look on everyone's screen. F11 opens the game's character
customization during a session; the others see the new look.

## How it works

- The game draws a player from `PlayerData.customization` through helpers that know only the
  local player, so every remote player looked like whoever was watching.
- Each machine sends its own player's customization (`Appearance`, the game's `LazySerializer`)
  when it changes and whenever the set of players changes; the host passes joiners' looks on.
- Every machine draws each remote body with that player's preset and colour palette and redraws
  a body that respawns.
- A returning joiner's look is part of their [profile](player-profiles.md).

## Files

[CoopAppearanceSync.cs](../src/GK2Coop/CoopAppearanceSync.cs).

## Messages

`GK2Coop.Appearance.v1`.

## Settings

`[Coop] ShareAppearance` (true), `[UI] ChangeLookKey` (F11). `[Fixes] RemoteBodyAppearance`
(false) is an older fix that re-applies the standard look to remote bodies.

## Tests

Regression run `rejoin` (Full).
