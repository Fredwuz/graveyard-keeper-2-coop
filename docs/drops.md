# Drops

Items on the ground exist once in the shared world. Each is made on one machine and shown on
all; the host decides who picks it up, so an item is never credited twice and never lost.

## How it works

- **Spawn.** The machine whose action made a drop (`DropSystem.DropItemIntoWorld`) sends
  `DropSpawn`: the drop's id, item id, count, scene, position, and the world object it came
  from. The host passes it on; receivers make the same drop under the same id.
- **Two players finish the same object** at almost the same moment: the host keeps the first
  harvest's drop and takes the second back from everyone (`SecondHarvest`).
- **Pickup is a request.** `PlayerData.CollectDrop` and `CollectResDrop` on a joiner become a
  `DropPickupRequest`. The host checks the drop still exists and that the player's bag has
  room (against its [shadow copy](player-profiles.md) of that player), credits the player, and
  sends `DropPickupResult` to everyone. An item that does not fit goes back on the ground.
- **Carrying.** Lifting a log or a body onto the head (`BigDropInteractionHandler.Interact`) is a
  request too: a joiner asks (`DropCarryRequest`), the host takes the drop off its ground and
  tells everyone who carries it (`DropCarryResult`); only then does the log go onto the joiner's
  head. The host's own lift is told the same way. A carried log put down comes back under the
  same id and is shared as a new drop; one put into a log holder is gone from every ground.
  A drop the host does not have (it lies in a scene the host has not loaded) may be carried
  unless someone carries it already.
- **Where they lie.** Each machine throws a new drop out with its own random offset and pushes
  drops with its own physics, so a log lay somewhere else on every screen. The machine whose
  player moved a drop last has the say over it: the one that made it or put it down, or whose
  player walked into it (`DropView.DoKick`). Once a second every machine sends the drops it has
  the say over that came to rest somewhere other than it last said (`DropPosition`); a joiner
  sends to the host, which puts them there and passes them on. The host also sends the drops no
  player moved, and those another player moved more than 10 s ago. After a join, the host sends
  all drops again 5 s later.
- **Merges.** Drops that touch merge. Only the host merges, and it reports a merge only when
  items moved (`DropMerge`); joiners apply the counts.
- **Join.** The host's [join snapshot](joining.md) sends every drop it has; a joiner first clears
  the drops of its world copy.

## Files

[CoopDropSync.cs](../src/GK2Coop/CoopDropSync.cs); [CoopDropTrace.cs](../src/GK2Coop/CoopDropTrace.cs)
logs where a harvested item went (diagnostics only, with `DetailedLogs`).

## Messages

| Name | Direction |
| --- | --- |
| `GK2Coop.DropSpawn.v1` | any → host → others |
| `GK2Coop.DropPickupRequest.v2` | joiner → host |
| `GK2Coop.DropPickupResult.v2` | host → all |
| `GK2Coop.DropMerge.v1` | host → joiners |
| `GK2Coop.DropPosition.v1` | any → host → others |
| `GK2Coop.DropCarryRequest.v1` | joiner → host |
| `GK2Coop.DropCarryResult.v1` | host → joiners |

## Settings

`[Coop] ShareDrops` (true; forces `RemoteDeathDrops` off); `[Diagnostics] TraceDrops` (with
`DetailedLogs`).

## Tests

Regression runs `surveys`, `zombie-out`, `zombie-grab`, `mix`, `chaos`.
