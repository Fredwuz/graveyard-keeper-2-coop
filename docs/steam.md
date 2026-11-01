# Steam

With `[Network] Transport = Steam`, the whole session runs over Steam's networking: friends
join by Steam ID through Valve's relay, without port forwarding. The host opens a friends-only
lobby, so friends can be invited (F10) or click "Join game" in their friends list.

## How it works

- **Switching the wire.** [CoopSteamTransportSwitch.cs](../src/GK2Coop/CoopSteamTransportSwitch.cs)
  takes over the game's `UNetworkManager.StartHostGame` / `ConnectToHost` in Steam mode: it
  makes [SteamSocketsTransport.cs](../src/GK2Coop/SteamSocketsTransport.cs) Netcode's transport and
  starts Netcode directly. Without Steam it falls back to the game's UDP transport.
- **Addresses.** `steam:<SteamID>` connects to that Steam user through the relay. A plain
  address connects through Steam's sockets over IP (LAN, one-PC tests).
- **The host listens both ways:** by Steam ID (P2P) and on IP port `[Network] Port`.
- **Who may connect.** The host refuses a connection when the session already has
  `[Network] MaxPlayers` players. A P2P connection must also come from a Steam friend, a member
  of the hosted lobby, or the host's own account (one-account tests). See
  `SteamSocketsTransport.Refusal`. A Steam ID is public, so without this check anyone could
  connect.
- **Lobby.** [CoopSteamLobby.cs](../src/GK2Coop/CoopSteamLobby.cs): while hosting over Steam, a
  friends-only lobby sized `MaxPlayers` (2 to 4), carrying the mod version and protocol, and
  rich presence (`connect = +connect_lobby <id>`). A lobby Steam closes is opened again.
- **Joining from Steam.** An accepted invite, "Join game" in the friends list, or a launch with
  `+connect_lobby <id>` enters the lobby, checks version and protocol (a clear message before
  anything is copied), then runs the usual [world copy](joining.md) to `steam:<owner>`. A
  request that arrives while the player is in a world waits for the main menu.
- **Friends hosting.** The Join page lists friends who play this game in a lobby
  (`FriendsHosting`).
- **Names.** Over Steam, players are named by their Steam names.

## Files

[SteamSocketsTransport.cs](../src/GK2Coop/SteamSocketsTransport.cs), [CoopSteamTransportSwitch.cs](../src/GK2Coop/CoopSteamTransportSwitch.cs),
[CoopSteamLobby.cs](../src/GK2Coop/CoopSteamLobby.cs).

## Settings

`[Network] Transport` (IP or Steam), `MaxPlayers` (4), `Port` (8889);
`[UI] InviteKey` (F10). Environment variable `GK2COOP_TEST_STEAM_LOCALIP=1` (tests only): join
the lobby's host over 127.0.0.1 instead of its Steam ID, so one account can play both sides.

## Limits

- The transport maps Netcode's reliable deliveries to Steam's reliable channel, which fragments
  messages up to 512 KB.
- Plain IP connections to a Steam host are not checked for friendship (there is no Steam
  identity); only the player count applies.

## Tests

Regression runs `steam` and `lobby` (one Steam account plays both sides).
