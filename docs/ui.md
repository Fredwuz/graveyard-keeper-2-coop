# Menus, HUD, controller and translations

The Co-op menu on the main menu, the status panel and name tags in the game, a Co-op entry in
the pause menu, full controller support, and every text in the game's eleven languages.

## Co-op menu

[CoopMenu.cs](../src/GK2Coop/CoopMenu.cs). A "Co-op" button in the main menu opens pages to host
(players 2 to 4, "Host on Steam" or without Steam, the night rule) or join (Steam friends who
host, by address, recent addresses). It does not start the game itself: the player then presses
New Game or Continue as always, and the choice is written to the settings
(`[Network] StartupMode`, `Address`, `Port`, `Transport`, `MaxPlayers`, `NightPasses`), which the
plugin acts on (see [architecture](README.md#architecture)). The join window with the two
keeper heads is [CoopProgressWindow.cs](../src/GK2Coop/CoopProgressWindow.cs).

## In the game

- **Status panel and name tags.** [CoopHud.cs](../src/GK2Coop/CoopHud.cs): F6 status, F7 name tags.
  Name tags sit over the drawn head and are left out for players in another scene.
- **Notices.** [CoopStatus.cs](../src/GK2Coop/CoopStatus.cs): `Announce` shows a short notice;
  `Set` sets the session status shown in the panel and the join window.
- **Pause > Co-op.** [CoopPauseEntry.cs](../src/GK2Coop/CoopPauseEntry.cs): invite friends, change
  look, status and name tags, the mod's version, and (test copies only) test tools. It is how a
  controller reaches what the F keys do.

## The game's look

[GameUi.cs](../src/GK2Coop/GameUi.cs) copies the game's own window frame, buttons, fonts and input
field from the live game (as the game's Workshop window does), so the mod's screens look and
react like the game's. When the parts cannot be found (a game update renamed them),
`GameUi.Ready` stays false and every screen draws its plain IMGUI fallback.
`[UI] GameStyle = false` forces the plain look.

## Controller

[CoopInput.cs](../src/GK2Coop/CoopInput.cs) reads the game's own input (`LazyInput` over Rewired):
whether a controller is in use, a button read through every game key bound to it, hiding buttons
from the game while a mod screen uses them, and the game's button icons.
[CoopKeyboard.cs](../src/GK2Coop/CoopKeyboard.cs) is an on-screen keyboard where Steam offers none.

## Translations

[CoopText.cs](../src/GK2Coop/CoopText.cs) (class `L`). Every player-facing sentence is English in the
code and is the key: `L.T("text")`, `L.F("text {0}", value)`, or `L.Key("text")` to mark a key
looked up later. A missing translation shows English. The language follows the game's own
setting. Tables: `CoopText.<lang>.cs`, generated from [tests/translations.py](../tests/translations.py)
by `python tests/Generate-Translations.py`, except German, kept by hand in `CoopText.de.cs`.
`python tests/Check-Translations.py` checks every text has every language with the same
placeholders. Developer-only screens (F8, test tools) stay English.

## Keys and settings

`[UI] ShowCoopMenu` (true), `GameStyle` (true), `ShowSessionStatus` (true), `SessionStatusKey`
(F6), `ShowNameTags` (true), `NameTagKey` (F7), `InviteKey` (F10), `ChangeLookKey` (F11),
`ChatKey` (T), `OverlayKey` (F8, [diagnostics](diagnostics.md)); `[Coop] RecentAddresses`.

## Tests

Regression runs `menu`, `plain`, `nav`, `pause`, `prompt`, `tabs`, `tagtrace`, `world` (HUD
look); `surveys` (every language, resolutions).
