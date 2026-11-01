# Story scenes and conversations

When a story scene or a sermon starts for one player, the others are asked whether to watch it.
Players nearby see the lines of another player's conversation. Players in another game scene
(prison, sewers) are hidden.

## Watching a scene together

[CoopSceneShare.cs](../src/GK2Coop/CoopSceneShare.cs). A scene plays only on the machine of the
player who triggers it.

- When one starts there (a script event fires, then the cinematic bars appear or the player's
  control is taken), the others are asked (`SceneShare`).
- Whoever accepts is taken to the triggering player, and the same scene plays on their machine:
  camera, dialogue, animations, and the story NPCs it moves or spawns. Every step the mod
  already syncs from the triggering player (quests, rewards, world values, weather, time,
  buildings, fights, zombies, further scripts) is skipped, so nothing happens twice.
- Afterwards the viewer is taken back to where they were. A player can stop watching.
- **The question.** [CoopScenePrompt.cs](../src/GK2Coop/CoopScenePrompt.cs): a game-style
  notification with the other player's head and a 10 s countdown. Keyboard: Y watch, N keep
  playing. Controller: Y and B; hold B to stop watching. For those 10 s the game does not see
  those buttons.

## Conversations

[CoopSpeechShare.cs](../src/GK2Coop/CoopSpeechShare.cs). Every dialogue line (`Bubble.Talk`) also
goes to the others (`Speech`). A player in the same area, within a few steps, sees an NPC's line
above that NPC and the talking player's line above that player. The text travels as the game's
translation key, so everyone reads it in their own language. Only what is shown is shared.

## Players in other scenes

[CoopSceneSync.cs](../src/GK2Coop/CoopSceneSync.cs). All game scenes use the same coordinates, so a
player in the prison was drawn standing in the open world. Each player tells the others which
scene they are in (`Scene`); a body in another scene has its renderers off and no name tag.
Its movement still arrives.

## Research

[CoopCutsceneTrace.cs](../src/GK2Coop/CoopCutsceneTrace.cs) only logs what starts each scripted
scene and whether a mirrored quest transition was running at the time.

## Messages

`GK2Coop.SceneShare.v1`, `GK2Coop.Speech.v1`, `GK2Coop.Scene.v1`.

## Settings

`[Coop] OfferScenes` (true), `WatchSceneKey` (Y), `SkipSceneKey` (N), `ShowOthersConversations`
(true); `[UI] HidePlayersInOtherScenes` (true).

## Limits

The mod does not show the answers another player chooses in a conversation; nearby players see
the lines.

## Tests

Regression runs `share`, `answer`, `skip`, `by-joiner`, `decline`, `stop`, `prompt`, `late`,
`late-ans`, `sermon`, `speech`, `scene` (Full).
