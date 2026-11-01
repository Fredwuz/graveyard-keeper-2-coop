# AGENTS.md

Guide for AI coding agents (and people) working in this repository.

## What this is

A BepInEx 5 plugin (C#, .NET Framework 4.7.2) that adds 2 to 4 player co-op to Graveyard Keeper 2
(Windows, Steam). The host plays their own save; joiners play in a copy of it. The mod finishes
the game's own unfinished Unity Netcode co-op and shares the world through its own named network
messages. Players are friends on home PCs; any one session has one host and up to three joiners.

Start with [docs/README.md](docs/README.md): the architecture, then one page per feature.

## Layout

| Path | What |
| --- | --- |
| `src/GK2Coop/Plugin.cs` | Entry point: settings, start-up of every part, per-frame and per-second drivers, hosting and connecting |
| `src/GK2Coop/Coop*.cs` | One class per feature (see the docs page of that feature) |
| `src/GK2Coop/CoopText*.cs` | Translations (`L.T`, `L.F`); `CoopText.<lang>.cs` are generated |
| `src/GK2Coop/GameUi.cs` | The game's own look for the mod's screens |
| `tests/` | Offline checks (`*.py`) and the live multiplayer regression (`*.ps1`, `GameplayProbe/`) |
| `scripts/` | Build, package, Workshop folder, test-copy and VM setup, decompiling the game |
| `package/` | Default settings file and the player guides shipped in the zip |
| `docs/` | Feature documentation |

## Commands

```powershell
dotnet build src\GK2Coop\GK2Coop.csproj -c Release   # needs the game (with BepInEx)
python tests/Check-Messages.py                        # unique message names; no game needed
python tests/Check-Translations.py                    # every text in every language; no game needed
.\tests\Run-Regression.ps1 -Changed                   # live tests on the test copies in testbed\
.\scripts\Decompile-Game.ps1                          # the game's code into artifacts\decomp\<game version>
```

The build references the game's DLLs. The game folder comes from `Directory.Build.props`
(default: Steam's default library) or a local, git-ignored `dev.props`; scripts read the same
value with `dotnet msbuild src\GK2Coop\GK2Coop.csproj -getProperty:GamePath`. Never hard-code a
game path in a project or script. Without the game installed the mod cannot build: say so in
your report instead of claiming the change compiles. Live tests need the test copies in the
git-ignored `testbed\` folder and the day-18 test save ([docs/testing.md](docs/testing.md)).
Never commit anything from `testbed\` or `artifacts\`.

**Read the game before patching it.** Look up the method you hook, its callers and the fields
you read in `artifacts\decomp\<game version>\` (one `.cs` per type, `grep` it). If the folder for
the installed version is missing, run `scripts\Decompile-Game.ps1`. The decompiled code is the
game developers': never commit it or copy it into the repository, docs or commits.

## Rules

**Player data**
- Never write into the player's own saves. Joiners never save; world copies are
  `GK2Coop_<16 hex>` slots; host saves stay single-player saves ([docs/joining.md](docs/joining.md)).
- Tests run only on test copies, never on the Steam install, and never touch the player's saves
  (`tests/GameplayProbe/TestSafety.ps1`).

**Network messages**
- Name: `GK2Coop.<Name>.v<N>`, a `const` in the class that owns it. Register it in
  `CoopSession.EnsureHandlers` and unregister it in `CoopSession.UnregisterHandlers`.
  Run `tests/Check-Messages.py`.
- Any change to what a message carries: bump `CoopSession.ProtocolVersion`.
- Joiners send to the host. The host applies a joiner's news and passes it on to the other
  joiners; where the host decides (chest contents, pickups), it sends the result to everyone,
  the sender included.
- A handler first checks its role (`IsHost`) and, on a joiner, that the sender is
  `NetworkManager.ServerClientId`. It reads with `ReadValueSafe` inside `try`/`catch`, checks
  sizes and indexes, and never throws into Netcode. Network data is untrusted input.
- Pack with `Gzip.Compress` / `Gzip.Decompress` (size-capped). Whole game objects go through
  `CoopGameSerializer` (Odin) or `CoopGameSerializer.LazySerializerMethod`.

**Frame time**
- No `Resources.FindObjectsOfTypeAll` in per-frame or per-second code: use `CoopBodies.All()`.
- Reflection through `Plugin.FindGameType` and `CoopDiagnostics.GetMember` / `GetStatic` (cached).
- Once-a-second work: a `Tick` in the list in `Plugin.RunSecondTicks`.
- Detailed logging only behind `CoopDiagnostics.Detailed` (`[Diagnostics] DetailedLogs`).

**Structure of a feature**
- A static class with `Init(ManualLogSource)`, `Enabled` (set from a setting in `Plugin.Awake`)
  and `Install(Harmony)`. `Install` catches its own failure, logs a warning and turns the
  feature off: a game update must disable one feature, not the mod.
- State that must not outlive a session gets reset in `CoopLifecycle.End`.
- Log exceptions with `Plugin.Unwrap(ex).Message`.

**Text players see**
- English literal through `L.T("…")` or `L.F("… {0}", x)`. Add the translations to
  `tests/translations.py` and German by hand to `CoopText.de.cs`, then run
  `python tests/Generate-Translations.py` and `python tests/Check-Translations.py`.
- Player guides: edit `tests/guides.py`, run `python tests/Generate-Guides.py`.

**Versions**
- `Plugin.Version` and `<Version>` in `GK2Coop.csproj` must be equal; the build fails otherwise.

**Style**
- Match the surrounding code. Comments say why, in plain English, often with the case found in
  play or in a test run ("found by `mix`").
- When a change alters what players see or a limit, update the feature's page in `docs/` and the
  README ("Shared data", "Limits of this version", "Keys").

**Commits**
- Commit to `dev`. `main` holds released versions only: it gets one pull request from `dev` per
  release, after the full regression. A branch of its own only for an experiment that may be
  thrown away.
- Agents may commit without asking, and may group the changes into commits as they see fit.
- Do not push unless asked.
- Commit messages: `<version>: <what changed for players>` for a release, otherwise
  `<area>: <what changed>`; then a bullet list.
- Never commit `testbed\`, `artifacts\` or `dev.props` (git ignores them).

## Settings

Every setting with its default and description: [package/de.fredwuz.gk2coop.cfg](package/de.fredwuz.gk2coop.cfg).
They are bound in `Plugin.Awake`; keep that file in step when adding one.
