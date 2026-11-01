# Session and handshake

Players connect, the host checks that both run the same wire format, everyone learns the
others' names, and the joiners' clocks follow the host's. When a session ends, every part of the
mod is reset in one place.

## How it works

- **Handshake.** A joiner sends `Hello` (protocol number, mod version, name) every 3 s until the
  host answers with `Welcome`. The host accepts only the same `CoopSession.ProtocolVersion`. A
  refused joiner is told why and disconnected 1.5 s later, so it can show the reason.
- **What the host sends an accepted joiner, in this order:** `Welcome`, the other players
  (`Peer`, both ways), then the [join snapshot](joining.md), the [crafting stations](crafting.md),
  the [tech tree](knowledge.md), the [world values](world-values.md) and the [gem pool](knowledge.md).
- **Names.** Over Steam a player's Steam name; otherwise `[Session] PlayerName`; otherwise
  "Host" / "Player N". At most 24 characters. On the host, a joiner's chat name always comes
  from this list, so nobody can speak as someone else.
- **Third and fourth players.** Each joiner builds records only for itself and the host, so the
  host introduces joiners to each other (`Peer`) and tells them when one leaves (`PeerLeft`).
  A friend who only copied the world ([joining](joining.md)) sends their name with the request;
  when the copy is complete and they go to open it, everyone reads "<name> is loading the
  world…" instead of "left", then "<name> joined." when they connect again.
  [CoopHostRelay.cs](../src/GK2Coop/CoopHostRelay.cs) forwards a joiner's game commands
  (movement and the like) to the other joiners; the game itself dropped them.
- **Clock.** Every 5 s the host sends its day and time of day (`Clock`, unreliable). A joiner
  more than 0.004 of a day (about one real second) away sets its clock to the host's. A time of
  1.0 counts as the next day at 0.0.
- **Player ids.** Every `PlayerData` ships with the same GUID. The mod derives one from the
  Netcode client id, the same on every machine (`AssignStablePlayerGuid`).
- **Connecting.** A joiner retries a connection that never reaches the host
  (`[Coop] ConnectRetries`, `ConnectRetrySeconds`); the status says what to check.
- **Session end.** [CoopLifecycle.cs](../src/GK2Coop/CoopLifecycle.cs) reads the role from Netcode
  every frame. When it changes, `End` runs every reset (callbacks, other players' records and
  bodies, chest, drop, death and quest bookkeeping, profiles), each on its own so one failure
  does not stop the rest. New session state that must not outlive a session gets its reset there.
- **Host gone.** [CoopHostLeft.cs](../src/GK2Coop/CoopHostLeft.cs): a joiner whose host quit is told
  the session ended and goes back to the main menu (nothing is saved in a world copy). Going to
  the main menu leaves the session, for host and joiner.
- **Safety net.** [CoopWatchdog.cs](../src/GK2Coop/CoopWatchdog.cs) checks every few seconds that
  player records and bodies match the session and removes provable ghosts and doubles.
- **Host socket after a crash (IP only).** [CoopTransportGuard.cs](../src/GK2Coop/CoopTransportGuard.cs)
  rebinds the host's UDP socket a moment after a joiner vanishes. The game's Unity Transport
  otherwise loses a receive buffer per ICMP error until the host can no longer hear anyone.

## Files

- [CoopSession.cs](../src/GK2Coop/CoopSession.cs): handshake, names, roster, clock, player GUIDs,
  and the registration of every named message (`EnsureHandlers` / `UnregisterHandlers`).
- [CoopLifecycle.cs](../src/GK2Coop/CoopLifecycle.cs), [CoopHostLeft.cs](../src/GK2Coop/CoopHostLeft.cs),
  [CoopWatchdog.cs](../src/GK2Coop/CoopWatchdog.cs), [CoopTransportGuard.cs](../src/GK2Coop/CoopTransportGuard.cs),
  [CoopHostRelay.cs](../src/GK2Coop/CoopHostRelay.cs).
- [CoopStatus.cs](../src/GK2Coop/CoopStatus.cs): what the player is told (status line, notices).
  Every failure says what to do.
- [Plugin.cs](../src/GK2Coop/Plugin.cs): `HostExistingWorld`, `ConnectToHost`, `DriveConnectRetry`,
  `AttachClientToExistingWorld`, the host's connect and disconnect callbacks.

## Messages

| Name | Direction | Content |
| --- | --- | --- |
| `GK2Coop.Hello.v1` | joiner → host | protocol, version, name |
| `GK2Coop.Welcome.v1` | host → joiner | protocol, accepted, host name, reason |
| `GK2Coop.Peer.v1` | host → joiner | another player's client id and name |
| `GK2Coop.PeerLeft.v1` | host → joiner | client id, name and "loading the world" of a player who left |
| `GK2Coop.Clock.v1` | host → joiners | day, time of day |

## Settings

`[Session] PlayerName`, `ClockSync` (true), `RebindAfterClientLeaves` (true);
`[Coop] RelayClientCommands` (true), `ConnectRetries` (5), `ConnectRetrySeconds` (10);
`[Network] Address`, `Port` (8889), `Transport` (IP), `StartupMode` (set by the menu),
`AutoInitialize` (true).

## Changing the wire format

Any change to what a `GK2Coop.*` message carries: bump `CoopSession.ProtocolVersion`. Older and
newer builds then refuse each other with a clear message instead of misreading each other.

## Tests

Regression runs `menu`, `rehost`, `fightlock`; `tests/Verify-VersionMismatch.ps1` for two
different versions meeting.
