using System.Text;

using LmpCommon.Xml;

namespace LunaCompatCommon.Utils
{
    /// <summary>
    /// Serialization for network transfer. Reason for not using binary formatting:
    /// - BinaryFormatter is obsolete
    /// - NuGet packages introduce additional DLLs (ignoring ILMerge)
    /// With patience, this could be reworked into manual serialization for each message type
    /// </summary>
    public static class SerializationUtil
    {
        #region Public Methods

        public static byte[] Serialize<T>(T obj)
            where T : class, new()
        {
            var serializedStr = LunaXmlSerializer.SerializeToXml(obj);
            return Encoding.UTF8.GetBytes(serializedStr);
        }

        public static T Deserialize<T>(byte[] param)
            where T : class, new()
        {
            var str = Encoding.UTF8.GetString(param);
            return LunaXmlSerializer.ReadXmlFromString<T>(str);
        }

        #endregion
    }
}
