# How the co-op mod works

These pages explain each feature of the mod: what it does, which files hold it, which network
messages it sends, which settings change it, what it cannot do yet, and which test run checks it.
Read [architecture](#architecture) first.

## Features

| Page | What players see |
| --- | --- |
| [Session and handshake](session.md) | Joining, leaving, version check, names, the shared clock |
| [Joining: the world copy](joining.md) | A joiner plays in a copy of the host's world |
| [Steam](steam.md) | Hosting and joining over Steam, invites, the lobby |
| [Player profiles](player-profiles.md) | A joiner keeps their own inventory between sessions |
| [World objects](world-objects.md) | Cut trees, mined rocks and replaced objects are the same for everyone |
| [Drops](drops.md) | Items on the ground exist once; only one player picks each up |
| [Chests](containers.md) | One content per chest for everyone |
| [Crafting](crafting.md) | Workbenches, ovens, repairs, cleared paths, conveyors |
| [Gardens](gardens.md) | A planted seed grows for everyone |
| [Quests](quests.md) | Quest progress is shared |
| [Tech tree, recipes and gems](knowledge.md) | One tech tree; one pool of action gems |
| [World values](world-values.md) | Reputation, congregation, sermon readiness |
| [Vendors](vendors.md) | One stock per vendor |
| [Buildings](buildings.md) | Build mode changes reach everyone |
| [Zombies](zombies.md) | The host runs the zombies; joiners see them work |
| [Sleep, time, pause and weather](time-and-weather.md) | The night passes when all sleep; one sky |
| [Story scenes and conversations](scenes.md) | Watch another player's scene; see their conversation |
| [Fights](fights.md) | One fight at a time; shared fight progress |
| [Work lock](work-lock.md) | One player at a time at a grave or autopsy table |
| [Chat](chat.md) | Text chat |
| [Appearance](appearance.md) | Each player is drawn with their own look |
| [Player bodies](player-bodies.md) | Other players' characters in your world |
| [Menus and HUD](ui.md) | The Co-op menu, status, name tags, controller support, translations |
| [Workshop updates](workshop-updates.md) | The mod updates itself from the Steam Workshop |
| [Diagnostics](diagnostics.md) | Logs, the profiler, the F8 window, test tools |
| [Testing](testing.md) | Offline checks and the live regression |
| [Build and release](build-and-release.md) | Build, version numbers, zip, Workshop folder |

## Architecture

**A BepInEx plugin inside the game.** [Plugin.cs](../src/GK2Coop/Plugin.cs) is the entry point.
`Awake` binds every setting, calls `Init(Logger)` on each `Coop*` class, sets its `Enabled` from
the settings and installs its Harmony patches. `Update` drives the per-frame parts;
`RunSecondTicks` runs the once-a-second `Tick` of each part, each at its own moment in the
second so they do not add up to one long frame.

**The game's own network code, finished by the mod.** Graveyard Keeper 2 ships an unfinished
co-op layer (`LazyNetwork`, Unity Netcode for GameObjects). The mod starts it, repairs it
([CoopPatches.cs](../src/GK2Coop/CoopPatches.cs), [CoopHostRelay.cs](../src/GK2Coop/CoopHostRelay.cs))
and moves player movement through it. Everything else the mod shares travels as Netcode
*named messages* called `GK2Coop.<Name>.v<N>`.

**A star around the host.** Joiners send only to the host. The host applies a joiner's message
and passes it on to the other joiners. A message handler therefore starts by checking its role
(`IsHost`) and, on a joiner, that the sender is the host (`NetworkManager.ServerClientId`).

**Who decides what:**

| State | Decided by |
| --- | --- |
| Clock, weather, the shared night | Host |
| Picking up and merging drops | Host |
| Chest contents | Host (joiners send what changed) |
| Crafting stations, zombies, conveyors | Host runs them |
| Work lock, fight lock | Host |
| Action gem pool | Host (adds up everyone's changes) |
| Deaths, quests, tech tree, world values, buildings, planting, finished crafts, vendors, looks, fight level stage | The player who acted; the host passes it on |

**Reading and changing the game.** Most game access is by reflection: `Plugin.FindGameType`
finds a game type once, `CoopDiagnostics.GetMember` / `GetStatic` read a member (cached per
type). Harmony prefixes and postfixes hook the game's own methods. When a remote player's
action must run through the game's code, [CoopPlayerContext.cs](../src/GK2Coop/CoopPlayerContext.cs)
points the game's "the player" statics at that player for one synchronous call.

**One session start and end.** Hosting starts from the Co-op menu: it sets
`[Network] StartupMode = Host`; once the world is loaded and the player has control,
`Plugin.HostExistingWorld` starts the game's host. Joining copies the host's world first
([joining](joining.md)), opens it with the game's Continue, then connects
(`StartupMode = Connect`). [CoopLifecycle.cs](../src/GK2Coop/CoopLifecycle.cs) notices when a
session ends and runs every reset in one place.

**Settings.** Every setting with its description and default is in
[package/de.fredwuz.gk2coop.cfg](../package/de.fredwuz.gk2coop.cfg). In a game it lives in
`BepInEx\config\de.fredwuz.gk2coop.cfg`. Each feature page lists its own.
