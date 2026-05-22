# The Fiend

This mod adds a new entity into the game — a demon-like creature.

The model is from a game called [Rinse and Repeat](https://hadriandev.itch.io/rinse-and-repeat) — please check it out if you have time.

> **2.0.0** — The Fiend has been rebuilt from source and updated to run on the current version of
> Lethal Company. (The original was discontinued on 13/09/2024 and stopped working after the v73
> netcode update.)

## Information

The Fiend is an entity that is taller than the player. He's a black figure with a red glow on his
face — a deadly demonic creature you won't want to face. The Fiend will jumpscare you and make you
bust out of fear.

This creature can be killed by stunning it (including stun grenades).

## Abilities

- **Break Doors** — The Fiend breaks doors when chasing or enraged, but he can't open them.
- **Hide** — He hides on the walls / ceiling.
- **Pro Flashlight Pain** — A pro-flashlight beam to his face stuns him.
- **Seeking Mode** — Acts like the Bracken.
- **Flicker Lights** — The lights tingle.
- **Invisible** — He teleports to a random player and stands still, waiting.
- **Rage** — He gets mad if you take the Apparatus or flash him too much.
- **Goodbye Breaker Box** — He rips the breaker box out of its hinges, door and all.
- **Funky** — A value that increases his speed and ability chance once it is 15:00+ in-game.

## Configuration

Settings live in `BepInEx/config/Fiend.cfg`:

| Setting | Default | Description |
|---|---|---|
| Spawn Weight | 30 | Chance to spawn the Fiend inside the building. |
| Moon | All | The only moon it can spawn on (one value at a time). |
| Flicker Chance | 1000 | Random 1/N chance of a light flicker affecting a random player. |
| Rage After Apparatus | true | Trigger rage mode if the Apparatus is removed. |
| Volume | 1 | Scream/idle sound volume (not footsteps). |

## Credits

- Original mod by [@Rolevote](https://www.youtube.com/@rolevote)
- Enemy model by [@HadrianDev](https://twitter.com/HadrianDev) — from [Rinse and Repeat](https://hadriandev.itch.io/rinse-and-repeat)
- Bundle-safe fixes adapted from [TheUnknownCod3r](https://github.com/TheUnknownCod3r/Fiend-LC)'s archived fork

This is a community continuation. The original code carries no license (all rights reserved) and the
enemy assets are third-party — all rights remain with their original creators. Redistributed with
attribution for preservation; public release is pending the original author's permission.
