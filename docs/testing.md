# Testing

Two kinds: quick checks that need no game, and live multiplayer runs that drive two to four
copies of the game on one PC.

## Without the game

```powershell
python tests/Check-Messages.py       # every GK2Coop.* message name is unique
python tests/Check-Translations.py   # every player text has every language, same placeholders
```

`Check-Translations.py` currently exits 1 for two texts that `CoopSaveBootstrap.Explain` passes
to `L.T` through a variable ("The host has no loaded save slot…", "The host is already sending a
save…"); the checker sees only literals.

## Live regression

`tests\Run-Regression.ps1` runs [Verify-ProgressedCoop.ps1](../tests/GameplayProbe/Verify-ProgressedCoop.ps1)
(the harness, one switch per experiment) on pairs of test copies, up to two pairs at once:
`testbed\GK2Coop-FullHost` + `testbed\GK2Coop-FullClient`, `testbed\GK2Coop-FullHost2` + `testbed\GK2Coop-FullClient2`.

```powershell
.\tests\Run-Regression.ps1 -Changed            # only runs that cover files changed since the last green run
.\tests\Run-Regression.ps1 -Only worldcopy,menu
.\tests\Run-Regression.ps1 -Profile Full       # everything, before a release
```

- **What it needs on the PC** (none of it is in git):
  - the test copies, in the repo's git-ignored `testbed\` folder, each a 1.4 GB clone of the game
    folder from [dev.props](build-and-release.md#build), with BepInEx and the mod installed. From
    the repo root:

    ```powershell
    .\scripts\Setup-LocalCoopClient.ps1 -ClientPath testbed\GK2Coop-FullHost
    .\scripts\Setup-LocalCoopClient.ps1 -ClientPath testbed\GK2Coop-FullClient
    ```

    `testbed\` also holds the copies' save folders (`GK2Coop-Saves`) and the results
    (`GK2Coop-Artifacts`). `git clean -x` deletes it;
  - the test world: `artifacts\save-backups\test-world\Steam_1.dat` and `Steam_1.info`, a copy
    of any save (`Steam_<n>.dat/.info` from the game's save folder, renamed to `Steam_1`). Every
    run starts from it. The experiments were written on a day-18 world
    (`save-backups\full-release-20260923-071217`, which also gets exact quest and object counts
    checked); an earlier world can lack what some experiments use (stations, story events, NPCs).
  The script stops with a message when either is missing.
- Each run lists the source files it covers (`Covers`); a file no run names means everything
  runs with `-Changed`. Each feature page here names its runs.
- Before starting: the mod is built (Release, against the first host copy) and copied to every
  copy (`-NoDeploy` skips both), the probe is built, the player's own saves are hashed and the
  game's registry preferences kept. Afterwards the preferences are restored and the saves checked.
- Before every start of a test copy, the game's own Workshop sync (`BepInEx\patchers\GK2.WorkshopSync`)
  is removed from it: it copies the Workshop's `GK2Coop.dll` over the build under test.
- A run also fails on problems in the games' logs: a `[game/Exception]`, or a co-op warning or
  error saying "could not", "failed" or "Exception". Known game noise is listed in `$logNoise`.
- Results: `testbed\GK2Coop-Artifacts\regress-<version>-<time>\summary.txt`.

## The probe

[tests/GameplayProbe](../tests/GameplayProbe/README.md) is a second BepInEx plugin, only on test
copies, enabled only when the config has `AutoStartNewGame = true`. It runs game methods on the
main thread on command (spawn, collect, chest edits, quests, players, bodies…) and answers with
real game state. `Invoke-Probe.ps1 -Peer Host|Client -Command inspect` asks it by hand.

## Safety rules

[TestSafety.ps1](../tests/GameplayProbe/TestSafety.ps1), dot-sourced by the live tests, so they can
run beside someone playing the real game:

- test copies only, never a folder inside any Steam library (`\steamapps\`);
- processes matched by path, never by name;
- test windows on a private desktop, muted, low priority;
- the game's shared registry preferences restored; the player's saves hashed before and after;
- each copy uses its own save folder (`GK2COOP_TEST_SAVE_FOLDER`), so tests never create, load or
  remove a slot among the player's saves.

Environment variables the mod reads (tests only): `GK2COOP_TEST_SAVE_FOLDER`,
`GK2COOP_TEST_HOST_PATH`, `GK2COOP_TEST_CONTINUE_SLOT`, `GK2COOP_TEST_TIDY`,
`GK2COOP_TEST_STEAM_LOCALIP`, `GK2COOP_TEST_VANILLA` (the game without the mod, for a
performance baseline).

## Other test scripts

| Script | Checks |
| --- | --- |
| `tests/Verify-CleanInstall.ps1` | The released zip into a game without mods, then the guide's uninstall |
| `tests/Verify-VersionMismatch.ps1` | Two mod versions meet; what each player is told |
| `tests/GameplayProbe/Verify-WorkshopUpdate.ps1` | The [Workshop update](workshop-updates.md) |
| `tests/GameplayProbe/Verify-MultiPlayer.ps1` | Three or four players (clones `testbed\GK2Coop-LocalClient*`) |
| `tests/vm/Verify-VmCoop.ps1` | Host on this PC, joiner in a Hyper-V VM, over a real network |
| `tests/Start-Playground.ps1` | Opens host and joiner on the day-18 playground for testing by hand; they host and join by themselves; `-SecondJoiner` adds a third player (`testbed\GK2Coop-FullClient2`). Windowed 1280×720 |
| `tests/Update-TestCopies.ps1` | Brings the test copies to the Steam install's game version |
| `tests/Capture-Media.ps1` | Screenshots for GitHub and the Workshop (`scripts/Make-Media.py` crops them) |

Test copies are made with `scripts/Setup-LocalCoopClient.ps1`; the VM with `scripts/vm/*`.
