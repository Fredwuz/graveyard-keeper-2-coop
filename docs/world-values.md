# World values

Reputation, the congregation, happiness, sermon readiness, body counts and farming modifiers
are the same for everyone.

## How it works

- The game keeps these in the player's resource store (`GameRes`), next to the player's own
  money and energy. They changed only on the machine where something happened (a sermon, a
  reward).
- Every couple of seconds each machine compares the store with what it last saw and sends the
  changed world values (`WorldRes`); the host applies a joiner's and passes them on. The last
  change wins.
- Left out: each player's own values (`CoopPlayerProfiles.PersonalResources`, stamina), zone
  ratings (`wz_*`, which the game computes from the world), and the action gems, which
  [CoopSharedGems](knowledge.md) pools by adding changes.
- A joiner receives all world values on joining.

## Files

[CoopWorldResSync.cs](../src/GK2Coop/CoopWorldResSync.cs).

## Messages

`GK2Coop.WorldRes.v1` (any → host → others).

## Settings

`[Coop] ShareWorldValues` (true).

## Tests

Regression run `worldres` (Full profile).
