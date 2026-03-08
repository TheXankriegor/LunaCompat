// ReSharper disable RedundantUsingDirective
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace LunaCompatCommon.Serializer
{
    /// <summary>
    /// Writes any supported value into the underlying <see cref="MemoryStream" />.
    /// </summary>
    internal sealed class SerializationWriter : IDisposable
    {
        #region Fields

        private readonly MemoryStream _ms;

        #endregion

        #region Constructors

        internal SerializationWriter() => _ms = new MemoryStream();

        #endregion

        #region Public Methods

        public void Dispose() => _ms.Dispose();

        #endregion

        #region Non-Public Methods

        internal byte[] ToArray() => _ms.ToArray();

        internal void Write(bool value) => _ms.WriteByte(value ? (byte)1 : (byte)0);

        internal void Write(byte value) => _ms.WriteByte(value);

        internal void Write(sbyte value) => _ms.WriteByte((byte)value);

        internal void Write(short value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
        }

        internal void Write(ushort value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
        }

        internal void Write(int value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
            _ms.WriteByte((byte)(value >> 16));
            _ms.WriteByte((byte)(value >> 24));
        }

        internal void Write(uint value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
            _ms.WriteByte((byte)(value >> 16));
            _ms.WriteByte((byte)(value >> 24));
        }

        internal void Write(long value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
            _ms.WriteByte((byte)(value >> 16));
            _ms.WriteByte((byte)(value >> 24));
            _ms.WriteByte((byte)(value >> 32));
            _ms.WriteByte((byte)(value >> 40));
            _ms.WriteByte((byte)(value >> 48));
            _ms.WriteByte((byte)(value >> 56));
        }

        internal void Write(ulong value)
        {
            _ms.WriteByte((byte)value);
            _ms.WriteByte((byte)(value >> 8));
            _ms.WriteByte((byte)(value >> 16));
            _ms.WriteByte((byte)(value >> 24));
            _ms.WriteByte((byte)(value >> 32));
            _ms.WriteByte((byte)(value >> 40));
            _ms.WriteByte((byte)(value >> 48));
            _ms.WriteByte((byte)(value >> 56));
        }

        internal void Write(float value)
        {
            Write(new SingleUnion
            {
                FloatValue = value
            }.IntValue);
        }

        internal void Write(double value)
        {
            Write(BitConverter.DoubleToInt64Bits(value));
        }

        internal void Write(char value)
        {
            Write((ushort)value);
        }

        internal void Write(string value)
        {
            if (value == null)
            {
                // -1 sentinel for null
                VarInt.Write(_ms, -1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            VarInt.Write(_ms, bytes.Length);
            _ms.Write(bytes, 0, bytes.Length);
        }

        internal void Write(Guid value)
        {
            var bytes = value.ToByteArray();
            _ms.Write(bytes, 0, 16);
        }

        internal void Write(DateTime value)
        {
            Write(value.Ticks);
            _ms.WriteByte((byte)value.Kind);
        }

        internal void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        internal void WriteByteArray(byte[] value)
        {
            if (value == null)
            {
                VarInt.Write(_ms, -1);
                return;
            }

            VarInt.Write(_ms, value.Length);
            _ms.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Writes any value using runtime type dispatch. Handles all supported types.
        /// </summary>
        internal void WriteValue(object value, Type type)
        {
            // Null object
            if (value == null)
            {
                if (type == typeof(string))
                {
                    Write(null);
                    return;
                }

                if (type == typeof(byte[]))
                {
                    WriteByteArray(null);
                    return;
                }


                // Reference type null
                _ms.WriteByte(0); // has-value = false
                return;
            }


            // Resolve the actual runtime type for dispatch
            if (type == typeof(bool))
            {
                Write((bool)value);
                return;
            }

            if (type == typeof(byte))
            {
                Write((byte)value);
                return;
            }

            if (type == typeof(sbyte))
            {
                Write((sbyte)value);
                return;
            }

            if (type == typeof(short))
            {
                Write((short)value);
                return;
            }

            if (type == typeof(ushort))
            {
                Write((ushort)value);
                return;
            }

            if (type == typeof(int))
            {
                Write((int)value);
                return;
            }

            if (type == typeof(uint))
            {
                Write((uint)value);
                return;
            }

            if (type == typeof(long))
            {
                Write((long)value);
                return;
            }

            if (type == typeof(ulong))
            {
                Write((ulong)value);
                return;
            }

            if (type == typeof(float))
            {
                Write((float)value);
                return;
            }

            if (type == typeof(double))
            {
                Write((double)value);
                return;
            }

            if (type == typeof(char))
            {
                Write((char)value);
                return;
            }

            if (type == typeof(string))
            {
                Write((string)value);
                return;
            }

            if (type == typeof(Guid))
            {
                Write((Guid)value);
                return;
            }

            if (type == typeof(DateTime))
            {
                Write((DateTime)value);
                return;
            }

            if (type == typeof(TimeSpan))
            {
                Write((TimeSpan)value);
                return;
            }

            if (type == typeof(byte[]))
            {
                WriteByteArray((byte[])value);
                return;
            }

            // Enum: write as underlying type
            if (type.IsEnum)
            {
                WriteValue(Convert.ChangeType(value, Enum.GetUnderlyingType(type)), Enum.GetUnderlyingType(type));
                return;
            }

            // Arrays (except byte[])
            if (type.IsArray)
            {
                var arr = (Array)value;
                var elemType = type.GetElementType();
                VarInt.Write(_ms, arr.Length);
                foreach (var item in arr)
                    WriteValue(item, elemType);
                return;
            }

            // Complex object: serialize public properties
            WriteComplexObject(value, type);
        }
        private void WriteComplexObject(object obj, Type type)
        {
            if (obj == null)
            {
                _ms.WriteByte(0);
                return;
            }

            _ms.WriteByte(1);
            var props = TypeCache.GetProperties(type);
            foreach (var prop in props)
                WriteValue(prop.GetValue(obj), prop.PropertyType);
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
