// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;
using System.Buffers.Binary;
using System.Text;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into PNG format using SkiaSharp.</summary>
/// <remarks>A figure is laid out in units of one pixel at 96 dpi, and the PNG is drawn with <c>dpi / 96</c> pixels for
/// each unit, so <c>Figure.Dpi</c> (set with <c>WithDpi</c>) sizes the picture: a 500 by 350 figure at 192 dpi is a
/// 1000 by 700 PNG with the same layout, drawn finer. Handed <see cref="SaveOptions"/>, the transform reads three
/// of its settings: <see cref="SaveOptions.Dpi"/> (when it is not <see cref="Figure.ReferenceDpi"/>) takes the place of the figure's
/// resolution, and <see cref="SaveOptions.Title"/> and <see cref="SaveOptions.Author"/> are stored in the file as text
/// chunks with the keywords Title and Author. The settings that belong to an SVG are not used.</remarks>
public sealed class PngTransform : FigureTransform
{
    /// <summary>Creates a new PNG transform with the specified chart renderer.</summary>
    public PngTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Creates a new PNG transform with the default chart renderer.</summary>
    public PngTransform() { }

    /// <summary>Transforms the figure and writes PNG bytes to the output stream, drawn at the figure's own resolution.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Figure.Dpi"/> is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">At that resolution the figure is not a whole number of pixels from 1 up.</exception>
    public override void Transform(Figure figure, Stream output) => Transform(figure, output, new SaveOptions());

    /// <summary>Transforms the figure and writes PNG bytes to the output stream, drawn at the resolution of
    /// <paramref name="options"/> when they ask for one and at the figure's own otherwise, with the title and the author
    /// of the options stored as text chunks.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The resolution is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">At that resolution the figure is not a whole number of pixels from 1 up.</exception>
    public override void Transform(Figure figure, Stream output, SaveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var bitmap = FigureRenderer.RenderToBitmap(figure, options.DpiFor(figure));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var text = new List<PngTextEntry>(2);
        if (options.TitleOrNull() is { } title) text.Add(new PngTextEntry("Title", title));
        if (options.AuthorOrNull() is { } author) text.Add(new PngTextEntry("Author", author));

        if (text.Count == 0)
        {
            data.SaveTo(output);
            return;
        }

        byte[] png = WithTextChunks(data.ToArray(), text);
        output.Write(png, 0, png.Length);
    }

    /// <summary>A piece of text a PNG carries under a keyword, such as a title.</summary>
    /// <param name="Keyword">The keyword, 1 to 79 printable Latin-1 characters (the PNG standard's rule).</param>
    /// <param name="Text">The text, any Unicode.</param>
    internal readonly record struct PngTextEntry(string Keyword, string Text);

    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Header chunk: 4 bytes of length, 4 of type, 13 of data, 4 of CRC.</summary>
    private const int HeaderChunkLength = 4 + 4 + 13 + 4;

    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>The PNG with one uncompressed UTF-8 <c>iTXt</c> chunk per entry, in order, straight after the header.
    /// With no entries it returns <paramref name="png"/> itself.</summary>
    /// <exception cref="ArgumentException"><paramref name="png"/> is not a PNG, or a keyword is not 1 to 79 printable
    /// Latin-1 characters.</exception>
    internal static byte[] WithTextChunks(byte[] png, IReadOnlyList<PngTextEntry> entries)
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
    internal static uint Crc32(ReadOnlySpan<byte> data)
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
