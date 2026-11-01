# Player bodies

Other players' characters stand and move in your world. The game's own multiplayer code was
unfinished; these parts make its remote bodies behave.

## How it works

- **Movement** travels as the game's own commands. The host forwards a joiner's commands to the
  other joiners ([CoopHostRelay.cs](../src/GK2Coop/CoopHostRelay.cs), see [session](session.md)).
- **Repairs to the game's remote-player setup.** [CoopPatches.cs](../src/GK2Coop/CoopPatches.cs):
  remote bodies stay kinematic so replicated moves are accepted (`[Fixes] RemoteBodyKinematic`),
  a new remote body is put into the local player's scene (`MatchRemoteBodyScene`), and it spawns
  at the local player's position instead of the origin until its first move arrives
  (`SeedRemoteSpawnPosition`, in `Plugin.SeedRemoteSpawnPosition`). With `DetailedLogs`, it also
  counts the game's commands.
- **Finding bodies cheaply.** [CoopBodies.cs](../src/GK2Coop/CoopBodies.cs) keeps the list of bodies
  as the game prepares them. A search through every object in memory cost 8 to 11 ms; per-frame
  and per-second code must use `CoopBodies.All()` instead.
- **Another player's death** plays only its animation on your screen; it no longer opens "you're
  dead" or freezes your controls ([CoopRemoteBodyGuard.cs](../src/GK2Coop/CoopRemoteBodyGuard.cs)).
- **A departed player's body** no longer breaks the players who stay: its leftover event
  handlers unsubscribe themselves ([CoopStaleBodyFix.cs](../src/GK2Coop/CoopStaleBodyFix.cs)).
  Before, the host could no longer sleep after a joiner had come and gone.
- **The wisp** (the story's companion) belongs to the local player only; other players' bodies
  get theirs switched off ([CoopWispFollow.cs](../src/GK2Coop/CoopWispFollow.cs)).
- **Acting as another player.** [CoopPlayerContext.cs](../src/GK2Coop/CoopPlayerContext.cs) points
  `MainGame.PlayerData` and `MainGame.PlayerController` (their backing fields) at a given player
  for one synchronous call, so the game's own code credits a remote player's action to them. A
  reference captured inside the scope and used later escapes it; `VerifyNotLeaked` runs every
  frame and logs a leaked scope.
- **Records and doubles.** Each player is a record (`GameSave.hostPlayer` / `clientPlayers`) plus a
  body. [CoopWatchdog.cs](../src/GK2Coop/CoopWatchdog.cs) removes ghost records and second bodies;
  [CoopLifecycle.cs](../src/GK2Coop/CoopLifecycle.cs) removes them at session end.
- **Other scenes** hide a player: see [story scenes](scenes.md#players-in-other-scenes).

## Settings

`[Fixes] RemoteBodyKinematic` (true), `SeedRemoteSpawnPosition` (true), `MatchRemoteBodyScene`
(true), `RemoteBodyAppearance` (false).

## Tests

Regression runs `rehost`, `tagtrace`, and every run that moves players.
