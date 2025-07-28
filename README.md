# Luna Compat

Miscellaneous compatibility patches for KSP's [Luna Multiplayer Mod](https://github.com/LunaMultiplayer/LunaMultiplayer).

## Overview

### Harmony patches

- [Extraplanetary-Launchpads](https://github.com/taniwha/Extraplanetary-Launchpads): Use a static random seed for recylcing
- [InfernalRobotics](https://github.com/meirumeiru/InfernalRobotics): Old LMP fixes ([https://github.com/LunaMultiplayer/LmpIrPlugin](https://github.com/LunaMultiplayer/LmpIrPlugin))
- [KIS](https://github.com/ihsoft/KIS): Old LMP fixes ([https://github.com/LunaMultiplayer/LmpKisPlugin](https://github.com/LunaMultiplayer/LmpKisPlugin))
- [Kethane](https://github.com/taniwha/Kethane): Use a static random seed for recourse distribution
- [SCANsat](https://github.com/KSPModStewards/SCANsat): Sync active scanners, background scanning and progress

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

## Creating a release

When creating a new release follow these steps

1. Update `AssemblyVersion` in [BuildConfigurationBase.targets](./BuildConfigurationBase.targets)
2. Update version in [lunacompat.version](./lunacompat.version)
3. Create release changenotes (e.g. `yaclog release <release-version>`)
4. Add changes to main branch
5. Create version tag to trigger release pipeline