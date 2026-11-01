# Diagnostics

Logs for bug reports, an opt-in profiler, a read-only developer window, and test tools for test
copies.

## Logs

Everything goes to `BepInEx\LogOutput.log` (the game replaces it at start).

- **Always:** session events, every sync's warnings, the game's own errors and networking
  messages (`[Diagnostics] UnityLogBridge`).
- **`[Diagnostics] DetailedLogs = true`** (off for players; tests turn it on) adds:
  player census (`PlayerCensus`), command counts (`CommandTelemetry`), network volume every 30 s
  ([CoopNetStats.cs](../src/GK2Coop/CoopNetStats.cs), `MeasureNetworkVolume`), the drop trace
  ([CoopDropTrace.cs](../src/GK2Coop/CoopDropTrace.cs), `TraceDrops`), and the joiners' logs on the
  host ([CoopLogRelay.cs](../src/GK2Coop/CoopLogRelay.cs), `[Session] RelayLogToHost`, lines
  marked `[guest N]`). They cost frame time.
- [CoopDiagnostics.cs](../src/GK2Coop/CoopDiagnostics.cs) holds the read-only observation and the
  cached reflection helpers `GetMember` / `GetStatic` that the whole mod uses.

## Profiler

[CoopProfiler.cs](../src/GK2Coop/CoopProfiler.cs), `[Diagnostics] Profiler = true`. Times the mod's
per-frame entry points (by method name, `EntryNames`), every Harmony patch the mod owns and
every named-message handler, plus frame times, garbage collections and traffic per message and
peer. Writes a report every 10 s. "mod ms/frame" counts only the outermost mod sections. A new
per-frame entry point needs its name in `EntryNames` to be timed.

## The F8 window

`[UI] OverlayKey` (F8) shows a read-only report of the game's network objects; F9 writes the full
report to the log; "Initialize native network" starts the game's network layer by hand (it
normally starts by itself, `[Network] AutoInitialize`). It has no host or connect buttons: an
older "Host new test world" button replaced the loaded world with a blank one, which the next
save wrote into the player's slot.

## Test tools

[CoopTestTools.cs](../src/GK2Coop/CoopTestTools.cs), only with `[Testing] TestTools = true` (test
copies, never players): Pause > Co-op > Test tools builds a test yard (chest, stations, autopsy
table, graves with bodies, a garden bed, a working zombie), hands out a kit, starts scenes and
sermons, teleports, sets the weather. English only. `[Testing] ContinueSlot` makes Continue open
a given slot (the playground) and presses it; `[Testing] AutoJoin` copies the world of the host at
that address and joins, retried every 10 s until it gets in.

## Tests

Regression run `testtools`; `-PerfMinutes` runs of the harness read the profiler.
