# Crafting

Workbenches, anvils, ovens and kilns are shared stations run by the host. Crafts that change a
world object (repairs, cleared blockages, opened doors, grown crops) finish for everyone.
Conveyors run on the host.

## Crafting stations

[CoopCraftSync.cs](../src/GK2Coop/CoopCraftSync.cs) covers the stations that open the game's craft
window (`WGODef.InteractionType.Craft`). A craft takes ingredients from the station, the chests
in the zone and the player, so two machines running one craft would each consume. Therefore:

- **Only the host runs these stations.** A joiner's add, remove and cancel become requests to
  the host (`CraftRequest`), sent after a fresh upload of the joiner's inventory
  (`CraftInventory`).
- The host queues the craft and remembers who ordered it. When it starts, the ingredients are
  taken as that player, and the joiner is told exactly what to remove (`CraftConsume`).
- Taking a finished product out of a station is a request too (`CraftInventoryTake` /
  `CraftInventoryTakeResult`).
- Once a second the host sends every station whose state changed (`CraftState`, the game's
  `LazySerializer`), and joiners load it into their copy. A joiner does not advance these
  stations itself. On joining, the host sends all of them.
- Not covered: instant crafts (`skipQueue`), gardens, graves, town repairs and other
  interaction types.
- Installed only when `[Coop] ShareCrafting` is on at start.

## Crafts that change the world

[CoopCraftEndSync.cs](../src/GK2Coop/CoopCraftEndSync.cs): a craft whose recipe replaces or
removes its object (`CraftDef.replaceWgoId`) ends in `WgoData.OnCraftEnd`. The machine where it
ends sends the object, what it was, and the recipe (`CraftEnd`); the host passes it on. A
receiver ends the same recipe on its copy (same replacement, same end scripts), unless its copy
has already changed. [Stable ids](world-objects.md) make the replacement the same object
everywhere. Host-run stations are left to `CoopCraftSync`.

## Conveyors

[CoopConveyorSync.cs](../src/GK2Coop/CoopConveyorSync.cs): `ConveyorSystem.CustomUpdate` runs only
on the host. Joiners skip their own conveyor ticks and see the results through the station and
chest syncs.

## Messages

`GK2Coop.CraftRequest.v1`, `CraftState.v1`, `CraftInventory.v1`, `CraftInventoryTake.v1`,
`CraftInventoryTakeResult.v1`, `CraftConsume.v1`, `CraftEnd.v1`.

## Settings

`[Coop] ShareCrafting` (true), `ShareFinishedWorldCrafts` (true).

## Tests

Regression runs `world` (crafting), `craftend`, `conveyor`.
