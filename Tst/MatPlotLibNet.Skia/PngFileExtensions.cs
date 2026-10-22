// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Text;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A PNG's size in pixels, as its IHDR chunk states it.</summary>
internal readonly record struct PngSize(int Width, int Height);

/// <summary>One chunk of a PNG file: its four-letter type, its data and the CRC the file stores after it.</summary>
internal readonly record struct PngChunk(string Type, byte[] Data, uint StoredCrc);

/// <summary>Reads a PNG file the way a viewer does, without decoding the pixels. The tests read the bytes themselves
/// because the encoder under test is the thing being checked: its own decoder would forgive what a reader may not.</summary>
internal static class PngFileExtensions
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The size the IHDR chunk states.</summary>
    public static PngSize PngPixelSize(this byte[] png)
    {
        var header = png.PngChunks()[0];
        Assert.Equal("IHDR", header.Type);
        return new PngSize(
            (int)BinaryPrimitives.ReadUInt32BigEndian(header.Data.AsSpan(0, 4)),
            (int)BinaryPrimitives.ReadUInt32BigEndian(header.Data.AsSpan(4, 4)));
    }

    /// <summary>Every chunk of the file in order, up to and including IEND.</summary>
    public static IReadOnlyList<PngChunk> PngChunks(this byte[] png)
    {
        Assert.True(png.AsSpan(0, Signature.Length).SequenceEqual(Signature), "not a PNG: the signature is missing");

        var chunks = new List<PngChunk>();
        int at = Signature.Length;
        while (at < png.Length)
        {
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at, 4));
            string type = Encoding.ASCII.GetString(png, at + 4, 4);
            byte[] data = png.AsSpan(at + 8, length).ToArray();
            uint crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length, 4));
            chunks.Add(new PngChunk(type, data, crc));
            at += 12 + length;
            if (type == "IEND") break;
        }

        Assert.Equal(png.Length, at);
        return chunks;
    }

    /// <summary>The keyword and text of an iTXt chunk (uncompressed, UTF-8), as a reader would split it.</summary>
    public static PngText InternationalText(this PngChunk chunk)
    {
        Assert.Equal("iTXt", chunk.Type);
        int keywordEnd = Array.IndexOf(chunk.Data, (byte)0);
        string keyword = Encoding.Latin1.GetString(chunk.Data, 0, keywordEnd);
        byte compressionFlag = chunk.Data[keywordEnd + 1];
        byte compressionMethod = chunk.Data[keywordEnd + 2];
        Assert.Equal(0, compressionFlag);
        Assert.Equal(0, compressionMethod);
        int languageEnd = Array.IndexOf(chunk.Data, (byte)0, keywordEnd + 3);
        int translatedEnd = Array.IndexOf(chunk.Data, (byte)0, languageEnd + 1);
        string text = Encoding.UTF8.GetString(chunk.Data, translatedEnd + 1, chunk.Data.Length - translatedEnd - 1);
        return new PngText(keyword, text);
    }
}

/// <summary>A keyword and the text stored under it.</summary>
internal readonly record struct PngText(string Keyword, string Text);
