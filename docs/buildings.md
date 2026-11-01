# Buildings

What one player builds or removes in build mode appears or disappears for everyone.

## How it works

- Placing a building (`WgoBuildPointer.TryDoBuildAction`) creates a world object on the
  builder's machine only. The builder sends the new object whole (`Build`, the game's save
  serializer), after the building's own after-build expressions ran, including any workbench
  it extends.
- The receiver adds it through `WorldData.AddWgoData`, which spawns its view when the scene is
  loaded. The object keeps its id everywhere, so later crafts, repairs and deaths on it line up.
- Removing in build mode (`RemovePointer.TryDoBuildAction` → `WgoData.DoBuildRemove`) is not a
  death, so world sync never saw it: the remover sends the object's id.
- Materials are taken from the builder's own inventory; dropped contents are
  [shared drops](drops.md). The host passes every change on and keeps a journal of the session's
  changes, cleared when its session ends.
- [Test tools](diagnostics.md) place their test yard through this path.

## Files

[CoopBuildSync.cs](../src/GK2Coop/CoopBuildSync.cs).

## Messages

`GK2Coop.Build.v1` (any → host → others).

## Settings

`[Coop] ShareBuilding` (true).

## Tests

Regression runs `build` (Full), `fightbuild`, `testtools`.
