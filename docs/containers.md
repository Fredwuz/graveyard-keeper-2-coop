# Chests

A chest, barrel or any other world container has one content for everyone.

## How it works

- **Joiners send what changed, not what is there.** A prefix on `Inventory.AddItemToInventory`,
  `AddItemsToInventory`, `RemoveItemById` and `Clear` records the contents; the postfix sends
  the difference (`ContainerDelta`, `itemId:+n;itemId:-m`). Whole contents were
  last-writer-wins and lost one of two edits made at once.
- **The host's contents are the truth.** The host applies a delta on top of its current
  contents and sends the result to everyone (`ContainerState`, `itemId x count` per stack, with
  a revision number per container). Joiners ignore an older revision. A delta the host cannot
  apply (items no longer there) is answered with the host's contents.
- **Rich containers** hold items with their own state: a body with organs, a worn tool, nested
  items. "id x count" cannot carry those, so the whole contents travel in the game's own
  serialized form (`ContainerRich`, at most 512 KB), and the last change wins. Containers with
  more than 40 stacks or a list longer than 480 characters are also sent whole. Graves and
  autopsy tables are therefore [one player at a time](work-lock.md).
- **Naming a container.** By the unique id of the world object that owns it, the same on every
  machine. An inventory does not know its owner, so the mod keeps a map from inventory id to
  owner id, rebuilt when an unknown inventory changes. An inventory found to belong to nobody in
  the world (a backpack, a tool belt) is remembered as such; a change to the joiner's own
  backpack goes to [player profiles](player-profiles.md) instead.
- **Join.** The host's [join snapshot](joining.md) sends every container changed this session.

## Files

[CoopContainerSync.cs](../src/GK2Coop/CoopContainerSync.cs).

## Messages

| Name | Direction |
| --- | --- |
| `GK2Coop.ContainerDelta.v3` | joiner → host |
| `GK2Coop.ContainerState.v3` | host → all |
| `GK2Coop.ContainerRich.v1` | any → host → others |

## Settings

`[Coop] ShareContainers` (true).

## Limits

- Two players who take the last stack of a chest at the same moment both keep it: the host
  rejects the second delta and corrects the chest, but the items are already in the second
  player's bag.
- Rich containers: the last change wins.

## Tests

Regression runs `surveys`, `mix`, `chaos`; `tests/vm/Verify-VmCoop.ps1` (chest edits both ways
across a real network).
