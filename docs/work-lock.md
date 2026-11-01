# Work lock

Only one player at a time can work at a grave or an autopsy table.

## Why

Their contents travel whole and the last change wins ([rich containers](containers.md)), and the
work runs on the machine of the player doing it. Two players at one table could each take the
same organ out of the same body; one burial could be dug twice.

## How it works

- The player who opens a grave or table holds it until their window is closed and nothing is in
  progress there on their machine.
- Anyone else who tries meanwhile is told who is working there.
- The host decides who holds what (`WorkLock`) and frees whatever a player who left was holding.
- [Fights](fights.md) use the same pattern.

## Files

[CoopWorkLock.cs](../src/GK2Coop/CoopWorkLock.cs) (`Update` runs every frame from `Plugin.Update`).

## Messages

`GK2Coop.WorkLock.v1`.

## Limits

Burials and autopsies in progress are not shared; only one player works at each.

## Tests

Regression runs `worklock`, `testtools`.
