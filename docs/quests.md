# Quests

Quest progress is shared: one player doing the work advances the story for everyone.

## How it works

- Each quest transition on one machine is sent (`Quest`); the host passes it on.
- The receiver applies it through the game's own `QuestSystemData` methods, so the quest's
  guards still run: a quest already completed or cancelled is left alone, and an unknown id is
  logged, never invented. A duplicate or out-of-order message is therefore harmless; sequence
  numbers are a second guard.
- The [join snapshot](joining.md) replays the transitions made since the host's session began.
- Story scenes that a transition starts play only for the player who caused them; the others
  are asked whether to watch ([story scenes](scenes.md)).

## Files

[CoopQuestSync.cs](../src/GK2Coop/CoopQuestSync.cs).

## Messages

`GK2Coop.Quest.v1` (any → host → others).

## Settings

`[Coop] ShareQuestProgress` (true).

## Tests

Regression runs `knowledge`, `mix`.
