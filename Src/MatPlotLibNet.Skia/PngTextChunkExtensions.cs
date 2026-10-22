// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Text;

namespace MatPlotLibNet.Skia;

/// <summary>A piece of text a PNG carries under a keyword, such as a title.</summary>
/// <param name="Keyword">The keyword, 1 to 79 printable Latin-1 characters (the PNG standard's rule).</param>
/// <param name="Text">The text, any Unicode.</param>
internal readonly record struct PngTextEntry(string Keyword, string Text);

/// <summary>Adds text chunks to a finished PNG. The encoder under the PNG export writes none, and a PNG stores a title or
/// an author as an <c>iTXt</c> chunk right after its header, so the chunks are put there byte by byte.</summary>
internal static class PngTextChunkExtensions
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Header chunk: 4 bytes of length, 4 of type, 13 of data, 4 of CRC.</summary>
    private const int HeaderChunkLength = 4 + 4 + 13 + 4;

    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>The PNG with one uncompressed UTF-8 <c>iTXt</c> chunk per entry, in order, straight after the header.
    /// With no entries it returns <paramref name="png"/> itself.</summary>
    /// <exception cref="ArgumentException"><paramref name="png"/> is not a PNG, or a keyword is not 1 to 79 printable
    /// Latin-1 characters.</exception>
    public static byte[] WithTextChunks(this byte[] png, IReadOnlyList<PngTextEntry> entries)
    {
        if (entries.Count == 0)
        {
            return png;
        }

        bool isPng = png.Length >= Signature.Length + HeaderChunkLength
            && png.AsSpan(0, Signature.Length).SequenceEqual(Signature)
            && BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(Signature.Length, 4)) == 13
            && png.AsSpan(Signature.Length + 4, 4).SequenceEqual("IHDR"u8);
        if (!isPng)
        {
            throw new ArgumentException("The bytes are not a PNG: the signature and the header chunk are missing.", nameof(png));
        }

        var chunks = entries.Select(Chunk).ToList();
        int insertAt = Signature.Length + HeaderChunkLength;
        var result = new byte[png.Length + chunks.Sum(c => c.Length)];
        png.AsSpan(0, insertAt).CopyTo(result);
        int at = insertAt;
        foreach (byte[] chunk in chunks)
        {
            chunk.CopyTo(result, at);
            at += chunk.Length;
        }

        png.AsSpan(insertAt).CopyTo(result.AsSpan(at));
        return result;
    }

    /// <summary>The CRC-32 the PNG standard puts after each chunk, taken over the chunk's type and data.</summary>
    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>One <c>iTXt</c> chunk: keyword, a zero, "not compressed", method 0, an empty language tag and an empty
    /// translated keyword (each ended by a zero), then the text in UTF-8.</summary>
    private static byte[] Chunk(PngTextEntry entry)
    {
        byte[] keyword = Encoding.Latin1.GetBytes(entry.Keyword);
        bool valid = keyword.Length is >= 1 and <= 79
            && Encoding.Latin1.GetString(keyword) == entry.Keyword
            && keyword.All(b => b is >= 32 and <= 126 or >= 161);
        if (!valid)
        {
            throw new ArgumentException(
                $"The keyword '{entry.Keyword}' of a PNG text chunk must be 1 to 79 printable Latin-1 characters.", nameof(entry));
        }

        byte[] text = Encoding.UTF8.GetBytes(entry.Text);
        // type (4) + keyword + 0 + flag + method + 0 (language) + 0 (translated keyword) + text
        var covered = new byte[4 + keyword.Length + 5 + text.Length];
        "iTXt"u8.CopyTo(covered);
        keyword.CopyTo(covered, 4);
        // The five bytes after the keyword are zero: the keyword's terminator, the compression flag and method, and the
        // terminators of the empty language tag and the empty translated keyword.
        text.CopyTo(covered, 4 + keyword.Length + 5);

        var chunk = new byte[4 + covered.Length + 4];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)(covered.Length - 4));
        covered.CopyTo(chunk, 4);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(4 + covered.Length), Crc32(covered));
        return chunk;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
