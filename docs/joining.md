# Joining: the world copy

A joiner plays in a copy of the host's world. While still on the main menu, the joiner
downloads the host's world, stores it as a separate save slot, and opens it with the game's own
Continue. The joiner's own saves are never changed.

## How it works

1. **Menu-stage connection.** "Join" in the Co-op menu (or a Steam invite) calls
   `CoopMenu.CopyAndJoin`: it sets `StartupMode = Connect` and starts
   `CoopSaveBootstrap.Begin`, which connects while the joiner is still on the main menu. The
   normal handshake is held back (`CoopSession.SuppressHello`) for this connection.
2. **Request.** The joiner sends `BootstrapRequest` with its protocol number. The host refuses a
   different protocol, a world without a save slot, or a second transfer while one runs
   (`BootstrapError`, shown in the joiner's language by `Explain`).
3. **The live world, not the last save.** The host serializes its loaded world in memory with
   the game's own save serializer (gardens grown and repairs made since the last save come
   along). The session's player records are taken out for that one call. If that fails, it
   sends the saved file instead.
4. **Transfer.** Deflate-compressed, in 1 KB chunks, 16 in flight, each confirmed by the joiner
   (`BootstrapChunk` / `BootstrapAck`), resent from the first unconfirmed chunk after 1.5 s
   without progress. Limits: 32 MB compressed, 128 MB unpacked. The joiner checks a SHA-256 of
   the result.
5. **An isolated slot.** The joiner writes the world as `GK2Coop_<16 hex>.dat/.info` (through
   `.part` files), dated 1 January 2000 so it is never the newest save.
6. **Open it.** The joiner disconnects, waits until Netcode has shut down, then presses the
   game's Continue for the player. `ActiveSlotPostfix` points Continue at the copy only during
   that press. Windows opened over the main menu meanwhile are closed.
7. **Connect in the world.** `StartupMode = Connect` connects again, `AttachClientToExistingWorld`
   makes the joiner's and the host's player records, and the [handshake](session.md) runs.
8. **Join snapshot.** [CoopJoinSnapshot.cs](../src/GK2Coop/CoopJoinSnapshot.cs): the host replays
   what changed since its session started (deaths, chest contents, drops, quest transitions)
   with the same messages it uses live. The joiner first removes the drops of its copy
   (`CoopDropSync.ClearForJoinSnapshot`), so the host's drops are the whole truth.

## Saves

- **A joiner never saves while joined** (`JoinerSavePrefix`), and never into a world copy
  afterwards. The host keeps the world, [player profiles](player-profiles.md) keep the joiner's
  inventory.
- **The host's saves stay single-player saves.** [CoopSaveHygiene.cs](../src/GK2Coop/CoopSaveHygiene.cs)
  takes the session's player records out of `GameSave` for each `SaveSystem.Save` call and puts
  them back afterwards.
- **Old copies.** At game start, `TidyWorldCopies` moves world copies older than 5 minutes to
  `<save folder>\GK2Coop\world-copies` and keeps only the newest 20 files there. Test slots go
  to `GK2Coop\test-slots`.

## Files

[CoopSaveBootstrap.cs](../src/GK2Coop/CoopSaveBootstrap.cs), [CoopJoinSnapshot.cs](../src/GK2Coop/CoopJoinSnapshot.cs),
[CoopSaveHygiene.cs](../src/GK2Coop/CoopSaveHygiene.cs), [CoopMenu.cs](../src/GK2Coop/CoopMenu.cs) (`CopyAndJoin`),
[CoopProgressWindow.cs](../src/GK2Coop/CoopProgressWindow.cs) (the join window).

## Messages

| Name | Direction | Content |
| --- | --- | --- |
| `GK2Coop.BootstrapRequest.v1` | joiner → host | protocol |
| `GK2Coop.BootstrapMeta.v1` | host → joiner | transfer id, sizes, SHA-256, slot `.info` |
| `GK2Coop.BootstrapChunk.v1` | host → joiner | transfer id, index, bytes |
| `GK2Coop.BootstrapAck.v1` | joiner → host | transfer id, index |
| `GK2Coop.BootstrapError.v1` | host → joiner | reason (English) |

## Settings

`[Coop] SendJoinSnapshot` (true).

## Limits

- Friends can join only after the host's first save (the first sleep in a new game).
- The host sends one world at a time; a second joiner gets "already sending" and retries.
- Connect timeout 20 s with one retry; a transfer that stalls for 60 s fails.

## Tests

Regression runs `menu`, `worldcopy`, `lobby`, `nav`, `rehost`.
