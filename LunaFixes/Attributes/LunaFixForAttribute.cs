using System;

namespace LunaFixes.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class LunaFixForAttribute : Attribute
{
    #region Constructors

    public LunaFixForAttribute(string packageId)
    {
        PackageId = packageId;
    }

    #endregion

    #region Properties

    public override object TypeId => this;

    public string PackageId { get; }

    #endregion
}