# Tech tree, recipes and action gems

One tech tree and one recipe book for everyone. Action gems (red, green and blue tech points)
are one pool by default.

## Knowledge

[CoopKnowledgeSync.cs](../src/GK2Coop/CoopKnowledgeSync.cs). Learned techs, recipes, buildings,
alchemy formulas, dialogue phrases, organs, unlocked character-window and tech-tree tabs, and
known map zones live in `GameSave.knowledgeSystem`, which is world state.

- Every couple of seconds each machine compares the lists with what it last saw and sends only
  what is new (`Knowledge`); the host applies a joiner's news and passes it on.
- Knowledge only grows: what one player learns, everyone knows.
- A learned tech is applied with the game's own `TechDef.Unlock(free: true)`, so its recipes,
  buildings and perks follow without charging the receiver.
- A joiner receives the host's whole knowledge on joining.

## Action gems

[CoopSharedGems.cs](../src/GK2Coop/CoopSharedGems.cs). The host's `[Coop] ShareActionGems` decides.

- Joiners send what changed since they last looked (+3 red, -10 blue), never a total, so two
  players earning at the same moment both count.
- The host adds each change to its own values (the pool) and sends the totals to everyone
  (`Gems`). A joiner's unsent gains go out before the host's totals are written.
- With sharing off, each player keeps their own gems, and [player profiles](player-profiles.md)
  store them per player.

## Messages

`GK2Coop.Knowledge.v1` (any → host → others), `GK2Coop.Gems.v1` (both).

## Settings

`[Coop] ShareKnowledge` (true), `ShareActionGems` (true).

## Tests

Regression runs `knowledge`, `tabs`, `world` (gems).
