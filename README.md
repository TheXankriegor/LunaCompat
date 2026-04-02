# Luna Compat

Compatibility patches to make KSP's [Luna Multiplayer Mod](https://github.com/LunaMultiplayer/LunaMultiplayer) support other mods.

- [Luna Compat](#luna-compat)
  - [Overview](#overview)
    - [Harmony patches](#harmony-patches)
    - [Part Syncs](#part-syncs)
  - [Installation](#installation)
    - [CKAN](#ckan)
    - [Manual installation](#manual-installation)
    - [Server plugin](#server-plugin)
    - [Dependencies](#dependencies)
  - [Creating a release](#creating-a-release)

## Overview

Complex mods are incompatible with Luna Multiplayer. 
LunaCompat adds compatibility patches for various mods via LMP part module configurations and Harmony patches. 
Patches are only applied to mods added to the modlist.
Check the repository to see the full list of supported mods.

### Harmony patches

- [Extraplanetary-Launchpads](https://github.com/taniwha/Extraplanetary-Launchpads): Use a static random seed for recycling
- [InfernalRobotics](https://github.com/meirumeiru/InfernalRobotics): Old LMP fixes ([https://github.com/LunaMultiplayer/LmpIrPlugin](https://github.com/LunaMultiplayer/LmpIrPlugin))
- [KIS](https://github.com/ihsoft/KIS): Old LMP fixes ([https://github.com/LunaMultiplayer/LmpKisPlugin](https://github.com/LunaMultiplayer/LmpKisPlugin))
- [Kethane](https://github.com/taniwha/Kethane): Use a static random seed for resource distribution
- [SCANsat](https://github.com/KSPModStewards/SCANsat): Sync active scanners, background scanning and progress
- [TUFX](https://github.com/KSPModStewards/TUFX): Keep settings between disconnects
- [ClickThroughBlocker](https://github.com/linuxgurugamer/ClickThroughBlocker): Keep settings between disconnects
- [PhysicsRangeExtender](https://github.com/jrodrigv/PhysicsRangeExtender): Force disable PRE at all times
- [Kerbal-Konstructs](https://github.com/KSP-RO/Kerbal-Konstructs): Sync instances, groups, map decals and facilities across clients (see [Server plugin](#server-plugin))

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
To avoid losing progress, try to interacting with one system at the same time (e.g. editing KerbalKolonies instances).

### Dependencies

- [Luna Multiplayer Client](https://github.com/LunaMultiplayer/LunaMultiplayer)
- [Harmony 2](https://github.com/KSPModdingLibs/HarmonyKSP)
- [Module Manager](https://github.com/sarbian/ModuleManager)

## Creating a release

When creating a new release follow these steps

1. Update `AssemblyVersion` in [BuildConfigurationBase.targets](./BuildConfigurationBase.targets)
2. Update version in [lunacompat.version](./lunacompat.version)
3. Create release changenotes (e.g. `yaclog release <release-version>`)
4. Add changes to main branch
5. Create version tag to trigger release pipeline