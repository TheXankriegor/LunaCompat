// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace LunaCompatCommon.Serializer
{
    /// <summary>
    /// Reads any supported value from the underlying <see cref="MemoryStream" />.
    /// </summary>
    internal sealed class SerializationReader : IDisposable
    {
        #region Fields

        private readonly MemoryStream _ms;

        #endregion

        #region Constructors

        internal SerializationReader(byte[] data) => _ms = new MemoryStream(data, writable: false);

        #endregion

        #region Public Methods

        public void Dispose() => _ms.Dispose();

        #endregion

        #region Non-Public Methods

        internal bool ReadBool() => _ms.ReadByte() != 0;

        internal byte ReadByte()
        {
            var b = _ms.ReadByte();
            if (b == -1)
                throw new Exception();

            return (byte)b;
        }

        internal sbyte ReadSByte() => (sbyte)ReadByte();

        internal short ReadInt16()
        {
            var b0 = ReadByte();
            var b1 = ReadByte();
            return (short)(b0 | (b1 << 8));
        }

        internal ushort ReadUInt16()
        {
            var b0 = ReadByte();
            var b1 = ReadByte();
            return (ushort)(b0 | (b1 << 8));
        }

        internal int ReadInt32()
        {
            var b0 = ReadByte();
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
        }

        internal uint ReadUInt32() => (uint)ReadInt32();

        internal long ReadInt64()
        {
            var b0 = ReadByte();
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            var b4 = ReadByte();
            var b5 = ReadByte();
            var b6 = ReadByte();
            var b7 = ReadByte();
            return (long)(b0 | ((ulong)b1 << 8) | ((ulong)b2 << 16) | ((ulong)b3 << 24) | ((ulong)b4 << 32) | ((ulong)b5 << 40) | ((ulong)b6 << 48) |
                          ((ulong)b7 << 56));
        }

        internal ulong ReadUInt64() => (ulong)ReadInt64();

        internal float ReadSingle()
        {
            var bits = ReadInt32();
            return new SingleUnion
            {
                IntValue = bits
            }.FloatValue;
        }

        internal double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

        internal char ReadChar() => (char)ReadUInt16();

        internal string ReadString()
        {
            var length = VarInt.Read(_ms);
            if (length == -1)
                return null;
            if (length == 0)
                return string.Empty;

            var bytes = new byte[length];
            var read = _ms.Read(bytes, 0, length);
            if (read != length)
                throw new Exception();

            return Encoding.UTF8.GetString(bytes, 0, length);
        }

        internal Guid ReadGuid()
        {
            var bytes = new byte[16];
            _ms.Read(bytes, 0, 16);
            return new Guid(bytes);
        }

        internal DateTime ReadDateTime()
        {
            var ticks = ReadInt64();
            var kind = (DateTimeKind)ReadByte();
            return new DateTime(ticks, kind);
        }

        internal TimeSpan ReadTimeSpan() => new TimeSpan(ReadInt64());

        internal byte[] ReadByteArray()
        {
            var length = VarInt.Read(_ms);
            if (length == -1)
                return null;
            if (length == 0)
                return new byte[0];

            var bytes = new byte[length];
            _ms.Read(bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Reads any value using runtime type dispatch. Handles all supported types.
        /// </summary>
        internal object ReadValue(Type type)
        {
            if (type == typeof(bool))
                return ReadBool();
            if (type == typeof(byte))
                return ReadByte();
            if (type == typeof(sbyte))
                return ReadSByte();
            if (type == typeof(short))
                return ReadInt16();
            if (type == typeof(ushort))
                return ReadUInt16();
            if (type == typeof(int))
                return ReadInt32();
            if (type == typeof(uint))
                return ReadUInt32();
            if (type == typeof(long))
                return ReadInt64();
            if (type == typeof(ulong))
                return ReadUInt64();
            if (type == typeof(float))
                return ReadSingle();
            if (type == typeof(double))
                return ReadDouble();
            if (type == typeof(char))
                return ReadChar();
            if (type == typeof(string))
                return ReadString();
            if (type == typeof(Guid))
                return ReadGuid();
            if (type == typeof(DateTime))
                return ReadDateTime();
            if (type == typeof(TimeSpan))
                return ReadTimeSpan();
            if (type == typeof(byte[]))
                return ReadByteArray();

            // Enum
            if (type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(type);
                var raw = ReadValue(underlying);
                return Enum.ToObject(type, raw);
            }

            // Arrays (except byte[])
            if (type.IsArray)
            {
                var elemType = type.GetElementType();
                var count = VarInt.Read(_ms);
                var arr = Array.CreateInstance(elemType, count);
                for (var i = 0; i < count; i++)
                    arr.SetValue(ReadValue(elemType), i);
                return arr;
            }

            // Complex object
            return ReadComplexObject(type);
        }

        private object ReadComplexObject(Type type)
        {
            var hasValue = ReadBool();
            if (!hasValue)
                return null;

            var obj = Activator.CreateInstance(type);
            var props = TypeCache.GetProperties(type);

            foreach (var prop in props)
            {
                var val = ReadValue(prop.PropertyType);
                prop.SetValue(obj, val);
            }

            return obj;
        }

        #endregion

        #region Nested Types

        [StructLayout(LayoutKind.Explicit)]
        private struct SingleUnion
        {
            #region Fields

            [FieldOffset(0)]
            public float FloatValue;
            [FieldOffset(0)]
            public int IntValue;

            #endregion
        }

        #endregion
    }
}
