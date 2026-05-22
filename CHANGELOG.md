# Changelog

## 2.0.0
- Community continuation: rebuilt the mod from source as a modern, SDK-style C# project.
- Updated to run on the current version of Lethal Company (v81). The original build broke with the
  v73 update, which upgraded Unity Netcode for GameObjects; the RPCs are now re-patched against the
  current netcode version, restoring multiplayer behavior.
- Adopted bundle-safe fixes from TheUnknownCod3r's archived fork: corrected the door/ceiling raycast
  layer-mask typo (`Enmies` -> `Enemies`), a divide-by-zero guard on the "Funky" timer, more robust
  Apparatus-removal detection (via `isLungDocked`), null-safety in the player-rotate loop, and
  coroutine cleanup after a kill.
- Otherwise faithful to the official 1.0.7 behavior (same prebuilt asset bundle).

## 1.0.7
- The Fiend's damage has been buffed.

## 1.0.6
- Fixed not killing the player; nerfed so it only kills you below 50 HP. The Fiend can now be
  killed by stunning it, including stun grenades.

## 1.0.5
- Fixed look-at-player; new pain sound; "Eclipsed" mode (gets mad after 15:00 in-game); turning
  fixes; rewrote the readme.

## 1.0.4
- Patched doors not breaking for other players; fixed audio bugs; fixed collision with other
  players not wanting to kill; added more configs.

## 1.0.3
- Fixed collision with player; can now unhide randomly; fixed the Fiend breaking inside custom
  interiors.

## 1.0.2
- Fixed Invisible mode not making the Fiend stand still.

## 1.0.1
- Fixed zero-speed player after a kill.
