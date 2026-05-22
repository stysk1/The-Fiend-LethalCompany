# The Fiend — Lethal Company mod

A BepInEx mod that adds **The Fiend**, a demonic custom enemy, to [Lethal Company](https://store.steampowered.com/app/1966720/Lethal_Company/).

This repository is a from-source rebuild of the (previously discontinued) mod, modernized so it
compiles, runs on the current game version, and is distributable. See [CHANGELOG.md](CHANGELOG.md).

> The enemy model is third-party — from [Rinse and Repeat](https://hadriandev.itch.io/rinse-and-repeat) by HadrianDev. Original mod by [Rolevote](https://www.youtube.com/@rolevote). See **Credits** below.

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or newer
- [BepInEx 5](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) and [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/) in-game (declared as Thunderstore dependencies)

## Build

```sh
dotnet tool restore                          # installs the netcode patcher CLI (one-time)
dotnet build TheFiend/TheFiend.csproj -c Release
```

The build downloads publicized game references via the `LethalCompany.GameLibs.Steam` NuGet
package (pinned to the current game version) from nuget.org and the BepInEx feed (see
[nuget.config](nuget.config)). After compiling, a post-build target runs `netcode-patch` to
re-inject the Unity Netcode RPC plumbing — **this is the fix** for the v73 netcode upgrade that
broke the original DLL. Output lands in `TheFiend/bin/Release/netstandard2.1/` as `TheFiend.dll`
plus the `thefiend` AssetBundle.

## Test locally

```powershell
./deploy.ps1
```

Builds and copies `TheFiend.dll` + `thefiend` into your r2modman profile's plugin folder. Override
the target with `-ProfilePath "<...>\BepInEx\plugins\Rolevote-The_Fiend"`. Then launch the modded
profile. To force a spawn for testing, set `Spawn Weight` very high and `Moon = -1` in
`BepInEx/config/Fiend.cfg`.

## Package for distribution

```powershell
./package.ps1
```

Builds and assembles a Thunderstore-format zip at `dist/The_Fiend-<version>.zip` (manifest, icon,
README, changelog, DLL, bundle). Hand it to friends to import into r2modman, or upload it to
Thunderstore. Tagging a commit `v*` triggers the GitHub Actions workflow to build and attach the
same zip to a GitHub Release.

## How it works

- `TheFiend/Plugin.cs` — the BepInEx plugin (`TheFiend.TheFiend`). Binds config, loads the
  AssetBundle, and registers the enemy with LethalLib.
- `TheFiend/TheFiendAI.cs` — the enemy behavior (`TheFiendAI : EnemyAI`), in the **global
  namespace**. State machine + Unity Netcode RPCs.
- `TheFiend/Bundles/thefiend` — the prebuilt Unity AssetBundle (model, animations, audio, and the
  EnemyType / Terminal assets). Reused as-is and shipped next to the DLL.

**Do not** rename the `TheFiend` assembly, move `TheFiendAI` out of the global namespace, or rename
its public fields — the prebuilt prefab binds to the script by assembly name + namespace + class,
and sets public fields by name. Any of those changes silently breaks the enemy at runtime.

Editing the AssetBundle itself (model/animations) is a separate effort that requires a Unity
2022.3.62 project; it is intentionally out of scope for this code rebuild.

## Credits

- Original mod & code: [Rolevote](https://www.youtube.com/@rolevote) — [rolevote/The-Fiend-LethalCompany](https://github.com/rolevote/The-Fiend-LethalCompany)
- Enemy model & assets: [HadrianDev](https://twitter.com/HadrianDev) — [Rinse and Repeat](https://hadriandev.itch.io/rinse-and-repeat)
- Bundle-safe fixes adapted from the archived fork by [TheUnknownCod3r](https://github.com/TheUnknownCod3r/Fiend-LC)

## Status & license

This is a **community continuation** of Rolevote's mod. The upstream code carries **no software
license** (all rights reserved) and the enemy assets are third-party, so this repo is **not**
licensed for public redistribution. It's for personal use and local sharing; **public release
(e.g. Thunderstore) is on hold pending permission from Rolevote**. See [NOTICE.md](NOTICE.md) for
full attribution and details.
