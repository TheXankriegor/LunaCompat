using LunaCompatCommon.Utils;

namespace LunaCompatCommon.Serializer
{
    /// <summary>
    /// High-performance binary serializer.
    /// Serializes any supported type to a compact <see cref="byte" /> array and back
    /// without relying on <c>BinaryFormatter</c> or any external libraries.
    /// <para>
    /// <b>Supported types</b>: bool, byte, sbyte, short, ushort, int, uint, long, ulong,
    /// float, double, decimal, char, string, Guid, DateTime, TimeSpan, enum,
    /// Nullable&lt;T&gt;, byte[], T[], List&lt;T&gt;, Dictionary&lt;TKey,TValue&gt;,
    /// and arbitrary complex objects with public readable+writable properties.
    /// </para>
    /// <para>
    /// <b>Wire format</b>: All multi-byte integers are little-endian.
    /// Length prefixes use LEB128 varint encoding; null references use a sentinel value of -1.
    /// Complex objects are preceded by a 1-byte has-value flag (0 = null, 1 = present).
    /// </para>
    /// </summary>
    public static class SerializationUtil
    {
        #region Public Methods

        /// <summary>
        /// Serializes <paramref name="value" /> into a compact byte array.
        /// </summary>
        /// <typeparam name="T">Any supported type.</typeparam>
        /// <param name="value">The value to serialize. May be null for reference types.</param>
        /// <returns>A byte array containing the serialized data.</returns>
        public static byte[] Serialize<T>(T value)
        {
            using var writer = new SerializationWriter();

            writer.WriteValue(value, typeof(T));
            return writer.ToArray();
        }

        /// <summary>
        /// Deserializes a value of type <typeparamref name="T" /> from <paramref name="data" />.
        /// </summary>
        /// <typeparam name="T">Any supported type.</typeparam>
        /// <param name="data">The byte array produced by <see cref="Serialize{T}" />.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(byte[] data)
        {
            using var reader = new SerializationReader(data);

            return (T)reader.ReadValue(typeof(T));
        }

        public static string CreatePrefixedModMessageId<T>()
        {
            return $"{Constants.Prefix}{typeof(T).Name}";
        }

        public static bool IsMessageOfType<T>(string messageName)
        {
            return messageName.Equals($"{Constants.Prefix}{typeof(T).Name}");
        }

        #endregion
    }
}
