# Fights

One player fights at a time in a world. The others get notices about the fight and its result.
The progress of each fight level is shared.

## How it works

- **A fight runs on the fighter's machine.** Its enemies exist only there, on a fighting level
  (an island the game teleports the fighter to), with the world clock stopped.
- **Fight lock.** [CoopFightLock.cs](../src/GK2Coop/CoopFightLock.cs). Two fights at once would each
  use their own copy of the military base's army, and the second to end would decide the level's
  stage for everyone. While one player is in a pre-fight or a fight, the others cannot start
  one; they are told who is fighting. The host decides (`FightLock`): a player's fight state
  leaving "disabled" claims the fight, returning to it lets go, and the host frees the claim of
  a player who left. Of two players starting at the same moment, the later one's pre-fight is
  cancelled. Fights a story script starts are never blocked.
- **Notices.** [CoopFightWatch.cs](../src/GK2Coop/CoopFightWatch.cs): when a fight becomes active
  and when it ends (won, lost or left), the others get a notice (`FightWatch`). Watching the
  fight itself was tried (0.61 to 0.62) and dropped.
- **Shared level progress.** [CoopFightSync.cs](../src/GK2Coop/CoopFightSync.cs): each stage change
  (`FightingLevelData.OnFightingLevelDataChanged`) is sent (`FightLevel`); a receiver sets the
  same stage unless it is fighting in that level itself.
- **Stuck on a fighting level.** [CoopArenaRescue.cs](../src/GK2Coop/CoopArenaRescue.cs): a player
  standing in a fighting level without a fight of their own is taken to that level's return
  point and told why.

## Files

[CoopFightLock.cs](../src/GK2Coop/CoopFightLock.cs), [CoopFightWatch.cs](../src/GK2Coop/CoopFightWatch.cs),
[CoopFightSync.cs](../src/GK2Coop/CoopFightSync.cs), [CoopArenaRescue.cs](../src/GK2Coop/CoopArenaRescue.cs).
[CoopRemoteBodyGuard.cs](../src/GK2Coop/CoopRemoteBodyGuard.cs) stops another player's death
animation from freezing your controls ([player bodies](player-bodies.md)).

## Messages

`GK2Coop.FightLock.v1`, `GK2Coop.FightWatch.v1`, `GK2Coop.FightLevel.v1`.

## Limits

Players cannot fight together, and cannot watch another player's fight.

## Tests

Regression runs `fightlock`, `fightwatch`, `fightbuild`, `world` (fight level sync).
