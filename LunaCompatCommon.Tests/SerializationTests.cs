using System;
using System.Collections.Generic;

using LunaCompatCommon.Serializer;

using Xunit;

namespace LunaCompatCommon.Tests;

public class SerializationTests
{
    #region Enums

    private enum TestEnum
    {
        First,
        Second,
        Third
    }

    #endregion

    #region Public Methods

    [Fact]
    public void Serialize_Deserialize_Primitive()
    {
        var val = 42;
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<int>(serialized, false);

        Assert.Equal(val, deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_String()
    {
        var val = "Hello, LunaCompat!";
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<string>(serialized, false);

        Assert.Equal(val, deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_ComplexObject()
    {
        var val = new TestComplexObject
        {
            Id = 123,
            Name = "Test Name",
            IsActive = true,
            Value = 1.23f,
            Identifier = Guid.NewGuid(),
            Status = TestEnum.Second,
            Timestamp = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var serialized = SerializationUtil.Serialize(val);
        var deserialized = SerializationUtil.Deserialize<TestComplexObject>(serialized);

        Assert.Equal(val.Id, deserialized.Id);
        Assert.Equal(val.Name, deserialized.Name);
        Assert.Equal(val.IsActive, deserialized.IsActive);
        Assert.Equal(val.Value, deserialized.Value);
        Assert.Equal(val.Identifier, deserialized.Identifier);
        Assert.Equal(val.Status, deserialized.Status);
        Assert.Equal(val.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void Serialize_Deserialize_NullString()
    {
        string val = null;
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<string>(serialized, false);

        Assert.Null(deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_Array()
    {
        var val = new[]
        {
            1, 2, 3, 4, 5
        };
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<int[]>(serialized, false);

        Assert.Equal(val, deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_AllPrimitives()
    {
        Assert.True(SerializationUtil.Deserialize<bool>(SerializationUtil.Serialize(true, false), false));
        Assert.Equal((byte)255, SerializationUtil.Deserialize<byte>(SerializationUtil.Serialize((byte)255, false), false));
        Assert.Equal((sbyte)-128, SerializationUtil.Deserialize<sbyte>(SerializationUtil.Serialize((sbyte)-128, false), false));
        Assert.Equal((short)-32768, SerializationUtil.Deserialize<short>(SerializationUtil.Serialize((short)-32768, false), false));
        Assert.Equal((ushort)65535, SerializationUtil.Deserialize<ushort>(SerializationUtil.Serialize((ushort)65535, false), false));
        Assert.Equal(4294967295, SerializationUtil.Deserialize<uint>(SerializationUtil.Serialize(4294967295, false), false));
        Assert.Equal(-9223372036854775808L, SerializationUtil.Deserialize<long>(SerializationUtil.Serialize(-9223372036854775808L, false), false));
        Assert.Equal(18446744073709551615UL, SerializationUtil.Deserialize<ulong>(SerializationUtil.Serialize(18446744073709551615UL, false), false));
        Assert.Equal(3.14159f, SerializationUtil.Deserialize<float>(SerializationUtil.Serialize(3.14159f, false), false));
        Assert.Equal(2.718281828459045, SerializationUtil.Deserialize<double>(SerializationUtil.Serialize(2.718281828459045, false), false));
        Assert.Equal('A', SerializationUtil.Deserialize<char>(SerializationUtil.Serialize('A', false), false));
    }

    [Fact]
    public void Serialize_Deserialize_TimeTypes()
    {
        var dateVal = new DateTime(2026, 5, 22, 12, 34, 56, DateTimeKind.Utc);
        var dateSerialized = SerializationUtil.Serialize(dateVal, false);
        var dateDeserialized = SerializationUtil.Deserialize<DateTime>(dateSerialized, false);
        Assert.Equal(dateVal, dateDeserialized);

        var timeVal = new TimeSpan(1, 2, 3, 4, 5);
        var timeSerialized = SerializationUtil.Serialize(timeVal, false);
        var timeDeserialized = SerializationUtil.Deserialize<TimeSpan>(timeSerialized, false);
        Assert.Equal(timeVal, timeDeserialized);
    }

    [Fact]
    public void Serialize_Deserialize_ByteArray()
    {
        var val = new byte[]
        {
            0xDE, 0xAD, 0xBE, 0xEF
        };
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<byte[]>(serialized, false);
        Assert.Equal(val, deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_NullByteArray()
    {
        byte[] val = null;
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<byte[]>(serialized, false);
        Assert.Null(deserialized);
    }

    [Fact]
    public void Serialize_Deserialize_MultiDimensionalArray()
    {
        var val = new[,]
        {
            {
                1, 2
            },
            {
                3, 4
            },
            {
                5, 6
            }
        };
        var serialized = SerializationUtil.Serialize(val, false);
        var deserialized = SerializationUtil.Deserialize<int[,]>(serialized, false);
        Assert.Equal(val, deserialized);
    }

    [Fact]
    public void Serialize_UnsupportedList_ThrowsNotSupportedException()
    {
        var val = new List<int>
        {
            1,
            2,
            3
        };
        var ex = Assert.Throws<NotSupportedException>(() => SerializationUtil.Serialize(val, false));
        Assert.Contains("Serialization of collections is not supported.", ex.Message);
    }

    [Fact]
    public void Serialize_UnsupportedDictionary_ThrowsNotSupportedException()
    {
        var val = new Dictionary<string, int>
        {
            {
                "A", 1
            }
        };
        var ex = Assert.Throws<NotSupportedException>(() => SerializationUtil.Serialize(val, false));
        Assert.Contains("Serialization of collections is not supported.", ex.Message);
    }

    #endregion

    #region Nested Types

    private class TestComplexObject
    {
        #region Properties

        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public float Value { get; set; }

        public Guid Identifier { get; set; }

        public TestEnum Status { get; set; }

        public DateTime Timestamp { get; set; }

        #endregion
    }

    #endregion
}
