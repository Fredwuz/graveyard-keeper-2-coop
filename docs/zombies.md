# Zombies

Zombies are the host's: one set of workers, run in one place. Joiners see the host's zombies
move and work.

## How it works

- Zombie work moves things (porters carry items between chests, gardeners tend beds, crafters
  run stations), so two copies of one zombie working would do everything twice.
- **Placing and picking up are shared.** A placement
  (`ZombieSystemData.PutZombieFromStoreToGameScene`) sends the zombie whole (game serializer,
  `Zombie.Presence`); the others place it through the same game call. A pickup takes it off
  everyone's scene.
- **Only the host runs them.** `ZombieSystem` and `ZombiePorterSystem` update only on the host.
- **Motion.** A few times a second the host sends each zombie's position, facing and animation
  (`Zombie.Motion`); joiners apply them to their copies.
- The host keeps a journal of the session's zombie changes, cleared when its session ends.
- Conveyors work the same way: see [crafting](crafting.md#conveyors).

## Files

[CoopZombieSync.cs](../src/GK2Coop/CoopZombieSync.cs) (`Tick` runs every frame from `Plugin.Update`).

## Messages

`GK2Coop.Zombie.Presence.v1`, `GK2Coop.Zombie.Motion.v1`.

## Settings

`[Coop] ShareZombies` (true).

## Tests

Regression runs `zombies`, `zombie-work`, `zombie-out`, `zombie-grab` (Full; one zombie
experiment per run), `testtools`.
