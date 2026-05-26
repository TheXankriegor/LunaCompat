using System.IO;

using LunaCompatCommon.Serializer;

using Xunit;

namespace LunaCompatCommon.Tests;

public class VarIntTests
{
    #region Public Methods

    [Theory]
    [InlineData(0, new byte[]
    {
        0
    })]
    [InlineData(1, new byte[]
    {
        1
    })]
    [InlineData(127, new byte[]
    {
        127
    })]
    [InlineData(128, new byte[]
    {
        128, 1
    })]
    [InlineData(255, new byte[]
    {
        255, 1
    })]
    [InlineData(16383, new byte[]
    {
        255, 127
    })]
    [InlineData(16384, new byte[]
    {
        128, 128, 1
    })]
    public void Write_Read_VarInt(int value, byte[] expectedBytes)
    {
        using var ms = new MemoryStream();
        VarInt.Write(ms, value);
        Assert.Equal(expectedBytes, ms.ToArray());

        ms.Position = 0;
        var readValue = VarInt.Read(ms);
        Assert.Equal(value, readValue);
    }

    #endregion
}
