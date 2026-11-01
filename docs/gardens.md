# Gardens

A seed one player plants grows in the same bed for everyone. The seed is spent only by the
player who planted it.

## How it works

- Beds grow on each machine. A finished bed becomes `garden_*_ready` under the same id
  ([stable ids](world-objects.md)), so the machines agree once both have grown.
- When a player plants, that machine sends the bed, the crop and the seed (`GardenPlant`); the
  host passes it on. Every other machine plants the same crop in the same bed with the
  parameters the game derives from that seed, but with no requirements: taking the seed again
  would come out of that machine's own player or a shared chest.
- Harvesting a ready bed is a [death or a finished craft](world-objects.md); its drops are
  [shared drops](drops.md).

## Files

[CoopGardenSync.cs](../src/GK2Coop/CoopGardenSync.cs).

## Messages

`GK2Coop.GardenPlant.v2` (any → host → others).

## Settings

`[Coop] SharePlanting` (true).

## Tests

Regression run `garden` (Full profile).
