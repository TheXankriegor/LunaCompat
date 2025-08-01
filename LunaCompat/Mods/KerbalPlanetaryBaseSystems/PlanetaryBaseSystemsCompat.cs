﻿using JetBrains.Annotations;

using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.KerbalPlanetaryBaseSystems;

[LunaFix]
[UsedImplicitly]
internal class PlanetaryBaseSystemsCompat : ModCompat
{
    #region Properties

    public override string PackageName => "PlanetarySurfaceStructures";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        // TODO add Kerbal Planetary Base Systems fixes here
    }

    #endregion
}
