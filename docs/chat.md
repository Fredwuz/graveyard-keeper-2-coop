# Chat

Text chat between players.

## How it works

- T (`[UI] ChatKey`) opens a line at the bottom left; Enter sends, Escape closes. While it is
  open the game's input is switched off (`LazyInput.SetInputActivity`), so typing does not move
  the player or open windows.
- Messages go to the host, which passes them on. On the host the sender's name always comes from
  the [session's name list](session.md), so nobody can speak as someone else.
- Plain text, at most 160 characters. Lines are drawn with rich text off, so tags in a message
  are shown as typed.
- Each player sees the last few lines, fading after a while, and the recent history while the
  line is open.
- **Controller.** Steam's on-screen keyboard where Steam offers one, otherwise the mod's own
  ([CoopKeyboard.cs](../src/GK2Coop/CoopKeyboard.cs)). Quick lines ("Come here!", "Thanks!"…)
  travel as a number, so each player reads them in their own language.

## Files

[CoopChat.cs](../src/GK2Coop/CoopChat.cs).

## Messages

`GK2Coop.Chat.v1` (joiner → host → others): name, text.

## Settings

`[Coop] Chat` (true), `[UI] ChatKey` (T).

## Tests

Regression runs `world`, `pause`, `steam`.
