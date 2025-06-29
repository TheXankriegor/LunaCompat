using System;

using HarmonyLib;

using JetBrains.Annotations;

using LmpClient.Events;

using LunaFixes.Attributes;

namespace LunaFixes.Mods.InfernalRobotics;

[LunaFixFor(PackageName)]
[UsedImplicitly]
internal class InfernalRoboticsCompat
{
    #region Constants

    private const string PackageName = "InfernalRobotics_v3";

    #endregion

    #region Fields

    private readonly Type _moduleIrServo;

    #endregion

    #region Constructors

    public InfernalRoboticsCompat(LunaFixForAttribute _)
    {
        // TODO: I am 99% sure that this does effectively nothing useful. Original LmpIrPlugin has it however?
        _moduleIrServo = AccessTools.TypeByName("InfernalRobotics_v3.Module.ModuleIRServo_v3");

        PartModuleEvent.onPartModuleBoolFieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleIntFieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleFloatFieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleDoubleFieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleVector2FieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleVector3FieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleQuaternionFieldProcessed.Add((x, y, z) => UpdatePartPos(x, y, z));
        PartModuleEvent.onPartModuleStringFieldProcessed.Add(UpdatePartPos);
        PartModuleEvent.onPartModuleObjectFieldProcessed.Add(UpdatePartPos);
        PartModuleEvent.onPartModuleEnumFieldProcessed.Add((x, y, z, _) => UpdatePartPos(x, y, z));
    }

    #endregion

    #region Non-Public Methods

    private void UpdatePartPos(ProtoPartModuleSnapshot module, string fieldName, object value)
    {
        if (module.moduleRef != null && _moduleIrServo.IsAssignableFrom(module.moduleRef.GetType()))
        {
            module.moduleRef.Fields[fieldName].SetValue(value, module.moduleRef);
            module.moduleRef.OnStart(PartModule.StartState.None);
        }
    }

    #endregion
}