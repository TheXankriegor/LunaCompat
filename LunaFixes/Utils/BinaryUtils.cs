using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LunaFixes.Utils;

public static class BinaryUtils
{
    #region Public Methods

    public static byte[] Serialize(object obj)
    {
        if (obj == null)
            return null;

        var bf = new BinaryFormatter();
        using var ms = new MemoryStream();

        bf.Serialize(ms, obj);
        return ms.ToArray();
    }

    public static T Deserialize<T>(byte[] param)
    {
        using var ms = new MemoryStream(param);

        IFormatter br = new BinaryFormatter();
        return (T)br.Deserialize(ms);
    }

    #endregion
}
