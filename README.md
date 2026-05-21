# Luna Compat

[![][shield:support-ksp]][KSP:developers]&nbsp;
[![][shield:support-lmp]][mod:lmp]&nbsp;
[![][shield:ckan]][CKAN:org]&nbsp;
[![][shield:license-mit]][LunaCompat:license]&nbsp;
[![][shield:issues]][LunaCompat:issues]&nbsp;

Compatibility patches to make KSP's [Luna Multiplayer Mod](https://github.com/LunaMultiplayer/LunaMultiplayer) support other mods.

- [Luna Compat](#luna-compat)
  - [Overview](#overview)
    - [Harmony patches](#harmony-patches)
    - [Part Syncs](#part-syncs)
  - [Installation](#installation)
    - [CKAN](#ckan)
    - [Dependencies](#dependencies)
    - [Manual installation](#manual-installation)
    - [Server plugin](#server-plugin)
      - [A word of caution](#a-word-of-caution)
  - [Creating a release](#creating-a-release)

## Overview


Complex mods are incompatible with Luna Multiplayer.

LunaCompat adds compatibility patches for various mods via LMP part module configurations and Harmony patches. 
Patches are only applied to mods added to the modlist. Check the repository to see the full list of supported mods.

### Harmony patches

| Mod | | Patches |
| --- |-| ------- |
|[![][shield:epl]][mod:epl]||Use a static random seed for recycling|
|[![][shield:ir]][mod:ir]||Old LMP fixes ([https://github.com/LunaMultiplayer/LmpIrPlugin](https://github.com/LunaMultiplayer/LmpIrPlugin))|
|[![][shield:kis]][mod:kis]||Old LMP fixes ([https://github.com/LunaMultiplayer/LmpKisPlugin](https://github.com/LunaMultiplayer/LmpKisPlugin))|
|[![][shield:kethane]][mod:kethane]||Use a static random seed for resource distribution|
|[![][shield:tufx]][mod:tufx]||Keep settings between disconnects|
|[![][shield:ctb]][mod:ctb]||Keep settings between disconnects|
|[![][shield:sua]][mod:sua]||Keep settings between disconnects|
|[![][shield:pre]][mod:pre]||Force disable PRE terrain extender|
|[![][shield:kk]][mod:kk]|[![][shield:serverplugin]][LunaCompat:serverplugin]|Sync instances, groups, map decals and facilities across clients| 
|[![][shield:kc]][mod:kc]|[![][shield:serverplugin]][LunaCompat:serverplugin]|Sync colonies & facilities across clients|
|[![][shield:scansat]][mod:scansat]|[![][shield:serverplugin]][LunaCompat:serverplugin]|Sync active scanners, background scanning and progress|

### Part Syncs

Part modules from the following mods are synced:

- [B9PartSwitch](https://github.com/blowfishpro/B9PartSwitch)
- [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks)
- [DecouplerShroud](https://github.com/linuxgurugamer/DecouplerShroud)
- [Extraplanetary-Launchpads](https://github.com/taniwha/Extraplanetary-Launchpads)
- [FarFutureTechnologies](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies)
- [InfernalRobotics](https://github.com/meirumeiru/InfernalRobotics)
- [Interstellar Extended](https://github.com/sswelm/KSP-Interstellar-Extended)
- [Interstellar Fuel Switch](https://github.com/sswelm/KSP-Interstellar-Extended/tree/master/FuelSwitch)
- [Photon Sailor](https://github.com/sswelm/KSP-Interstellar-Extended/tree/master/PhotonSail)
- [IR-ConnectionSystem](https://github.com/meirumeiru/IR-ConnectionSystem)
- [KerbalActuators](https://github.com/Angel-125/KerbalActuators)
- [KIS](https://github.com/ihsoft/KIS)
- [KAS](https://github.com/ihsoft/KAS)
- [KerbalColonies](https://github.com/KerbalColonies/KerbalColoniesCore)
- [KerbalPlanetaryBaseSystems](https://github.com/Nils277/KerbalPlanetaryBaseSystems)
- [Kethane](https://github.com/taniwha/Kethane)
- [NearFutureElectrical](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical)
- [NearFuturePropulsion](https://github.com/post-kerbin-mining-corporation/NearFuturePropulsion)
- [NearFutureSolar](https://github.com/post-kerbin-mining-corporation/NearFutureSolar)
- [NearFutureSpacecraft](https://github.com/post-kerbin-mining-corporation/NearFutureSpacecraft)
- [ProceduralParts](https://github.com/KSP-RO/ProceduralParts)
- [Sandcastle](https://github.com/Angel-125/Sandcastle)
- [SCANsat](https://github.com/KSPModStewards/SCANsat)
- [SpaceDust](https://github.com/post-kerbin-mining-corporation/SpaceDust)
- [StationPartsExpansionRedux](https://github.com/post-kerbin-mining-corporation/StationPartsExpansionRedux)
- [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat)
- [TexturesUnlimited](https://github.com/KSPModStewards/TexturesUnlimited)
- [TweakScale](https://github.com/JonnyOThan/TweakScale)
- [WildBlueCore](https://github.com/Angel-125/WildBlueCore)
- [WildBlueTools](https://github.com/Angel-125/WildBlueTools)

## Installation

### CKAN

The mod is available on [CKAN](https://github.com/KSP-CKAN/CKAN/releases) - just search for 'Luna Compat'!

### Dependencies

- [Luna Multiplayer Client](https://github.com/LunaMultiplayer/LunaMultiplayer)
- [Harmony 2](https://github.com/KSPModdingLibs/HarmonyKSP)
- [Module Manager](https://github.com/sarbian/ModuleManager)

### Manual installation

- Download the latest `LunaCompat.zip` from the [Releases](https://github.com/TheXankriegor/LunaCompat/releases)
- Unpack into your Kerbal Space Program folder
  - The ZIP file contents in `GameData/LunaMultiplayer/` should be merged with your existing LMP mod folder

### Server plugin

Certain mods require the LunaCompat server plugin to function correctly. If you are hosting a server, follow the steps below to add the server plugin. After launching the server once, configure the server settings using the '.\Universe\LunaCompat\ModSettingsStructure.xml' file.

- Download the latest `LunaCompatServerPlugin.zip` from the [Releases](https://github.com/TheXankriegor/LunaCompat/releases)
- Unpack into your Luna Multiplayer Server `Plugins` folder (create a new folder if it does not exist)

#### A word of caution

Just like most of the other features in LMP, syncing things across the network is done on a best-effort basis and may sometimes mess up. 
To avoid losing progress, try to limit the users interacting with one system at the same time (e.g. editing KerbalColonies instances).

## Creating a release

When creating a new release follow these steps

1. Update `AssemblyVersion` in [BuildConfigurationBase.targets](./BuildConfigurationBase.targets)
2. Update version in [lunacompat.version](./lunacompat.version)
3. Create release changenotes (e.g. `yaclog release <release-version>`)
4. Add changes to main branch
5. Create version tag to trigger release pipeline

[KSP:developers]: https://kerbalspaceprogram.com/index.php
[CKAN:org]: http://ksp-ckan.org/

[LunaCompat:license]: https://github.com/TheXankriegor/LunaCompat/blob/main/LICENSE
[LunaCompat:serverplugin]: https://github.com/TheXankriegor/LunaCompat#server-plugin
[LunaCompat:issues]:https://github.com/TheXankriegor/LunaCompat/issues?q=is%3Aissue+is%3Aopen

[mod:lmp]: https://github.com/LunaMultiplayer/LunaMultiplayer
[mod:scansat]: https://github.com/KSPModStewards/SCANsat
[mod:kc]: https://github.com/KerbalColonies/KerbalColoniesCore
[mod:kk]: https://github.com/KSP-RO/Kerbal-Konstructs
[mod:pre]: https://github.com/jrodrigv/PhysicsRangeExtender
[mod:ctb]: https://github.com/linuxgurugamer/ClickThroughBlocker
[mod:sua]: https://github.com/yalov/SpeedUnitAnnex
[mod:tufx]: https://github.com/KSPModStewards/TUFX
[mod:kethane]: https://github.com/taniwha/Kethane
[mod:kethane]: https://github.com/taniwha/Kethane
[mod:kis]: https://github.com/ihsoft/KIS
[mod:ir]: https://github.com/meirumeiru/InfernalRobotics
[mod:epl]: https://github.com/taniwha/Extraplanetary-Launchpads

[shield:license-mit]: http://img.shields.io/:License-MIT-a31f34.svg
[shield:support-ksp]: http://img.shields.io/badge/For%20KSP-1.12.5-bad455.svg
[shield:support-lmp]: http://img.shields.io/badge/For%20LMP-0.29.2-F57D27.svg
[shield:ckan]: https://img.shields.io/badge/CKAN-Indexed-brightgreen.svg
[shield:serverplugin]: https://img.shields.io/badge/Server%20Plugin-red.svg
[shield:issues]: https://img.shields.io/github/issues/TheXankriegor/LunaCompat.svg

[shield:scansat]: https://img.shields.io/badge/SCANsat-21.1-1D24E2.svg
[shield:kc]: https://img.shields.io/badge/KerbalColonies-1.2.2-1D24E2.svg
[shield:kk]: https://img.shields.io/badge/Kerbal--Konstructs-1.12.2-1D24E2.svg
[shield:pre]: https://img.shields.io/badge/PhysicsRangeExtender-1.21-1D24E2.svg
[shield:ctb]: https://img.shields.io/badge/ClickThroughBlocker-2.1.10-1D24E2.svg
[shield:sua]: https://img.shields.io/badge/SpeedUnitAnnex-1.6.1-1D24E2.svg
[shield:tufx]: https://img.shields.io/badge/TUFX-1.1.1-1D24E2.svg
[shield:kethane]: https://img.shields.io/badge/Kethane-0.11-1D24E2.svg
[shield:kis]: https://img.shields.io/badge/KIS-1.29-1D24E2.svg
[shield:ir]: https://img.shields.io/badge/InfernalRobotics-3.1.18-1D24E2.svg
[shield:epl]: https://img.shields.io/badge/Extraplanetary--Launchpads-6.99.3-1D24E2.svg

