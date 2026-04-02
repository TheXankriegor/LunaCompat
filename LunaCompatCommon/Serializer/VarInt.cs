// ReSharper disable RedundantUsingDirective

using System;
using System.IO;

namespace LunaCompatCommon.Serializer
{
    /// <summary>
    /// LEB128 (unsigned) variable-length integer helpers used for length prefixes.
    /// Values 0-127 cost 1 byte; 128-16383 cost 2 bytes, etc.
    /// </summary>
    internal static class VarInt
    {
        #region Non-Public Methods

        /// <summary>Writes a non-negative integer as a LEB128 varint.</summary>
        internal static void Write(Stream stream, int value)
        {
            var v = (uint)value;

            while (v >= 0x80)
            {
                stream.WriteByte((byte)(v | 0x80));
                v >>= 7;
            }

            stream.WriteByte((byte)v);
        }

        /// <summary>Reads a LEB128 varint from the stream.</summary>
        internal static int Read(Stream stream)
        {
            var result = 0;
            var shift = 0;

            while (true)
            {
                var b = stream.ReadByte();
                if (b == -1)
                    throw new Exception("Unexpected end of stream while reading varint.");

                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    break;

                shift += 7;
            }

            return result;
        }

        #endregion
    }
}
