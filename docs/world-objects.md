# World objects

A tree one player fells, a rock one player mines, a bush one player harvests is gone for
everyone. An object that turns into another (a crop into a ready crop, a tree into a stump, a
broken thing into a repaired one) has the same id on every machine, so later syncs still find it.

## How it works

- **Deaths.** [CoopWorldSync.cs](../src/GK2Coop/CoopWorldSync.cs): when a world object dies on one
  machine, that machine sends its id (`WgoDeath`); the host passes it on. The receiver kills its
  copy through the game's own HP path, so the death animation, sound and cleanup run. Scene
  objects carry the same unique id on every machine, so the id names the object.
- **Drops of a mirrored death.** With [drop sharing](drops.md) on (default), only the machine that
  caused the death makes the drops. `[Coop] RemoteDeathDrops` (off) instead lets every machine
  make its own drops; it is ignored while `ShareDrops` is on.
- **Death logic on joiners.** [CoopDeathLogicFix.cs](../src/GK2Coop/CoopDeathLogicFix.cs): the game
  skips every after-death effect on a co-op client (drops, quest triggers, inventory spill). The
  mod lets the joiner run it as in single player, by clearing `IsCoopGame` for that one call.
- **Stable replacement ids.** [CoopStableIds.cs](../src/GK2Coop/CoopStableIds.cs):
  `WorldData.ReplaceWgoData` gives a replacement a random id. While a session runs, the mod
  derives it from the replaced object's id and the new object type instead, so every machine
  gets the same id. Types that register a navigation hole by id in their constructor keep the
  random id.
- **Tool use (experiment, off).** [CoopToolSync.cs](../src/GK2Coop/CoopToolSync.cs) replays each
  tool tick on the other machine inside [CoopPlayerContext](player-bodies.md). Off by default:
  with deaths replicated it is not needed.

## Files

[CoopWorldSync.cs](../src/GK2Coop/CoopWorldSync.cs), [CoopDeathLogicFix.cs](../src/GK2Coop/CoopDeathLogicFix.cs),
[CoopStableIds.cs](../src/GK2Coop/CoopStableIds.cs), [CoopToolSync.cs](../src/GK2Coop/CoopToolSync.cs).

## Messages

`GK2Coop.WgoDeath.v1` (any → host → others), `GK2Coop.ToolUse.v1` (experiment).

## Settings

`[Coop] ReplicateWorldDeaths` (true), `RemoteDeathDrops` (false), `StableReplacementIds` (true),
`ReplicateToolUse` (false); `[Fixes] ClientRunsDeathLogic` (true).

## Tests

Regression runs `surveys`, `rejoin`, and the `mix` / `chaos` runs, which count every berry.
