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

## Settings

`[Coop] ShareDrops` (true; forces `RemoteDeathDrops` off); `[Diagnostics] TraceDrops` (with
`DetailedLogs`).

## Tests

Regression runs `surveys`, `zombie-out`, `zombie-grab`, `mix`, `chaos`.
