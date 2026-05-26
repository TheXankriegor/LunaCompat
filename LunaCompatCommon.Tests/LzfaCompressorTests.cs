using System;
using System.Text;

using LunaCompatCommon.Utils;

using Xunit;

namespace LunaCompatCommon.Tests;

public class LzfaCompressorTests
{
    #region Public Methods

    [Fact]
    public void Compress_Decompress_MatchesOriginal()
    {
        var strBuilder = new StringBuilder();

        for (var i = 0; i < 100; i++)
            strBuilder.Append("Lorem ipsum dolor sit amet");
        var originalString = strBuilder.ToString();
        var originalBytes = Encoding.UTF8.GetBytes(originalString);

        var compressed = LzfaCompressor.Compress(originalBytes);
        var decompressed = LzfaCompressor.Decompress(compressed);

        Assert.True(compressed.Length < originalBytes.Length);
        Assert.Equal(originalBytes, decompressed);
        Assert.Equal(originalString, Encoding.UTF8.GetString(decompressed));
    }

    [Fact]
    public void SmallData_DoesNotCompressButDecompressesFine()
    {
        var originalBytes = new byte[]
        {
            1, 2, 3, 4, 5
        };
        var compressed = LzfaCompressor.Compress(originalBytes);
        var decompressed = LzfaCompressor.Decompress(compressed);

        Assert.Equal(originalBytes, decompressed);
    }

    [Fact]
    public void SmallData_DoesNotFail()
    {
        var originalBytes = new byte[]
        {
            1
        };
        var compressed = LzfaCompressor.Compress(originalBytes);
        var decompressed = LzfaCompressor.Decompress(compressed);

        Assert.Equal(originalBytes, decompressed);

        originalBytes = Array.Empty<byte>();
        compressed = LzfaCompressor.Compress(originalBytes);
        decompressed = LzfaCompressor.Decompress(compressed);

        Assert.Equal(originalBytes, decompressed);
    }

    #endregion
}
