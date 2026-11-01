# Sleep, time, pause and weather

The night passes when all players sleep. One clock, one sky. Opening a window does not pause
the shared world.

## Sleep

[CoopSleepSync.cs](../src/GK2Coop/CoopSleepSync.cs). The game's sleep runs the whole simulation
at 50 times speed. On a joiner that raced its own world ahead; a host sleeping alone skipped the
night under an awake joiner.

- Nobody's sleep speeds up time unless every player is asleep. A lone sleeper still sleeps:
  energy refills at normal speed and the sleep ends as usual.
- Joiners tell the host whether they sleep (`Sleep`). When the host and all joiners do, the
  host runs the night at the game's speed and tells the joiners when it starts and ends
  (`Night`). A joiner still asleep when it ends wakes with a full energy bar.
- **Host rule.** `[Network] NightPasses = Host` (menu: "I sleep"): the night passes when the
  host sleeps; the others stay awake and their clock jumps with the host's.

## Clock

The host sends its time every 5 s; see [session](session.md). Day changes are written directly,
because the game's `AddToDay` also runs the host-side end-of-day updates of vendors and the town.

## Pause

[CoopPauseSync.cs](../src/GK2Coop/CoopPauseSync.cs): during a session, `MainGame.PauseGame` is
skipped, so a player in a menu or window does not stop the world for the others.

## Weather

[CoopWeatherSync.cs](../src/GK2Coop/CoopWeatherSync.cs). The weather picks its next state at
random when a phase ends, so each machine rolled its own. Only the host rolls; it sends its
weather state when it changes and to anyone who joins (`Weather`). Joiners enter that state
through `WeatherSystem.SetWeatherState`, which also switches rain, fog and wind.

## Messages

`GK2Coop.Sleep.v1` (joiner → host), `GK2Coop.Night.v1` (host → joiners),
`GK2Coop.Weather.v1` (host → joiners).

## Settings

`[Coop] NightPassesWhenAllSleep` (true), `ShareWeather` (true); `[Network] NightPasses`
(Everyone); `[Session] ClockSync` (true).

## Tests

Regression runs `sleep`, `pausesync` (Full), `world` (weather).
