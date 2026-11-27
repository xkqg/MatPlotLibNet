// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Transforms a sequence of <see cref="Figure"/> frames into an animated GIF.</summary>
/// <remarks>Uses <see cref="Transforms.Animation.GifTransform"/> for GIF89a encoding and <see cref="Transforms.Animation.GifTransform"/>
/// for 256-color uniform quantization. Each frame is drawn at the resolution of its own figure
/// (<see cref="Figure.Dpi"/>), by the same rule as a PNG: <c>dpi / 96</c> pixels for each unit of the figure. The frames
/// of one animation share a size, so they share a resolution. A GIF is not saved through
/// <see cref="SaveOptions"/>.</remarks>
public sealed class GifTransform : IAnimationTransform
{
    private readonly IChartRenderer _renderer;

    /// <summary>Creates a <see cref="GifTransform"/> using the default chart renderer.</summary>
    public GifTransform() => _renderer = new ChartRenderer();

    /// <summary>Creates a <see cref="GifTransform"/> using the specified chart renderer.</summary>
    public GifTransform(IChartRenderer renderer) => _renderer = renderer;

    /// <inheritdoc />
    public void Transform(IEnumerable<Figure> frames, TimeSpan interval, bool loop, Stream output)
    {
        var bitmaps = new List<SKBitmap>();
        try
        {
            foreach (var figure in frames)
                bitmaps.Add(RenderToBitmap(figure));

            Encode(bitmaps, (int)interval.TotalMilliseconds, loop, output);
        }
        finally
        {
            foreach (var bmp in bitmaps) bmp.Dispose();
        }
    }

    /// <summary>A frame is drawn at the resolution of its own figure, by the same rule as a PNG.</summary>
    private SKBitmap RenderToBitmap(Figure figure) => _renderer.RenderToBitmap(figure, figure.Dpi);

    private const int MinCodeSize = 8; // 256-color palette → 8-bit LZW

    private static readonly PaletteEntry[] Palette = BuildPalette();

    /// <summary>Encodes <paramref name="frames"/> into a GIF89a byte stream written to <paramref name="output"/>.</summary>
    /// <param name="frames">Bitmaps to encode (all must have the same dimensions).</param>
    /// <param name="delayMs">Frame delay in milliseconds (minimum 10 ms, clamped to GIF 1/100 s grid).</param>
    /// <param name="loop">When <see langword="true"/>, writes a Netscape 2.0 loop-forever extension.</param>
    /// <param name="output">Destination stream.</param>
    internal static void Encode(IReadOnlyList<SKBitmap> frames, int delayMs, bool loop, Stream output)
    {
        if (frames.Count == 0) return;
        int width  = frames[0].Width;
        int height = frames[0].Height;

        using var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);

        WriteHeader(writer, width, height);

        if (loop)
            WriteNetscapeExtension(writer);

        int delayHundredths = Math.Max(1, delayMs / 10);
        foreach (var bitmap in frames)
            WriteFrame(writer, bitmap, width, height, delayHundredths);

        writer.Write((byte)0x3B); // GIF Trailer
    }

    // --- GIF structural blocks ---

    private static void WriteHeader(BinaryWriter w, int width, int height)
    {
        // Header
        w.Write(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }); // "GIF89a"

        // Logical Screen Descriptor
        WriteLE16(w, (ushort)width);
        WriteLE16(w, (ushort)height);
        w.Write((byte)0xF7); // Global CT present, 8 color resolution, not sorted, CT size = 256 (2^(7+1))
        w.Write((byte)0);    // Background color index
        w.Write((byte)0);    // Pixel aspect ratio (0 = no info)

        // Global Color Table (256 × 3 bytes)
        foreach (var (r, g, b) in Palette)
        {
            w.Write(r);
            w.Write(g);
            w.Write(b);
        }
    }

    private static void WriteNetscapeExtension(BinaryWriter w)
    {
        w.Write((byte)0x21);  // Extension introducer
        w.Write((byte)0xFF);  // Application extension label
        w.Write((byte)0x0B);  // Block size = 11
        w.Write(new byte[] { 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30 }); // "NETSCAPE2.0"
        w.Write((byte)0x03);  // Sub-block size
        w.Write((byte)0x01);  // Sub-block ID = 1 (loop count follows)
        WriteLE16(w, 0);      // Repeat count: 0 = loop forever
        w.Write((byte)0x00);  // Block terminator
    }

    private static void WriteFrame(BinaryWriter w, SKBitmap bitmap, int width, int height, int delayHundredths)
    {
        // Graphic Control Extension
        w.Write((byte)0x21);  // Extension introducer
        w.Write((byte)0xF9);  // Graphic control label
        w.Write((byte)0x04);  // Block size
        w.Write((byte)0x00);  // Packed: no disposal, no user input, no transparent
        WriteLE16(w, (ushort)delayHundredths);
        w.Write((byte)0);     // Transparent color index (unused)
        w.Write((byte)0x00);  // Block terminator

        // Image Descriptor
        w.Write((byte)0x2C);  // Image separator
        WriteLE16(w, 0);      // Left
        WriteLE16(w, 0);      // Top
        WriteLE16(w, (ushort)width);
        WriteLE16(w, (ushort)height);
        w.Write((byte)0x00);  // Packed: no local CT, not interlaced

        // LZW-compressed pixel data
        var indices = MapToIndices(bitmap);
        w.Write((byte)MinCodeSize);
        var compressed = LzwCompress(indices);
        w.Write(compressed);
    }

    // --- LZW compression ---

    /// <summary>The pixels compressed as GIF's LZW and packed into its sub-blocks: the codes written least significant bit first within
    /// each byte, as the format requires, the last byte filled out with zeros when the codes end inside it.</summary>
    internal static byte[] LzwCompress(byte[] pixels)
    {
        int clearCode = 1 << MinCodeSize; // 256
        int eoi = clearCode + 1;          // 257

        var bytes = new List<byte>();
        int pending = 0, bitsInPending = 0;
        void Write(int code, int bits)
        {
            pending |= code << bitsInPending;
            bitsInPending += bits;
            while (bitsInPending >= 8)
            {
                bytes.Add((byte)(pending & 0xFF));
                pending >>= 8;
                bitsInPending -= 8;
            }
        }

        // A string the table knows, keyed by the code of the string so far (at most 12 bits) above the pixel that extends it (8 bits).
        var table = new Dictionary<int, int>(4096);
        int nextCode = eoi + 1;
        int codeSize = MinCodeSize + 1;
        int maxCode = 1 << codeSize;

        Write(clearCode, codeSize);

        int prefix = pixels[0];
        for (int i = 1; i < pixels.Length; i++)
        {
            int suffix = pixels[i];
            int key = (prefix << 8) | suffix;
            if (table.TryGetValue(key, out int existing))
            {
                prefix = existing;
            }
            else
            {
                Write(prefix, codeSize);
                if (nextCode <= 4095)
                {
                    table[key] = nextCode++;
                    if (nextCode > maxCode && codeSize < 12)
                    {
                        codeSize++;
                        maxCode <<= 1;
                    }
                }
                else
                {
                    // Table full — emit clear and reset
                    Write(clearCode, codeSize);
                    table.Clear();
                    nextCode = eoi + 1;
                    codeSize = MinCodeSize + 1;
                    maxCode = 1 << codeSize;
                }
                prefix = suffix;
            }
        }
        Write(prefix, codeSize);
        Write(eoi, codeSize);
        if (bitsInPending > 0) bytes.Add((byte)(pending & 0xFF));

        return PackIntoSubBlocks([.. bytes]);
    }

    /// <summary>Packages raw compressed bytes into GIF sub-blocks (max 255 bytes each + block terminator).</summary>
    private static byte[] PackIntoSubBlocks(byte[] data)
    {
        var result = new List<byte>(data.Length + data.Length / 255 + 2);
        int offset = 0;
        while (offset < data.Length)
        {
            int blockSize = Math.Min(255, data.Length - offset);
            result.Add((byte)blockSize);
            result.AddRange(data.AsSpan(offset, blockSize).ToArray());
            offset += blockSize;
        }
        result.Add(0x00); // Block terminator
        return [.. result];
    }

    private static void WriteLE16(BinaryWriter w, ushort value)
    {
        w.Write((byte)(value & 0xFF));
        w.Write((byte)(value >> 8));
    }

    /// <summary>One colour of the palette.</summary>
    /// <param name="R">Red, 0 to 255.</param>
    /// <param name="G">Green, 0 to 255.</param>
    /// <param name="B">Blue, 0 to 255.</param>
    internal readonly record struct PaletteEntry(byte R, byte G, byte B);

    /// <summary>Builds a 256-entry RGB palette using the 3-3-2 colour space.</summary>
    internal static PaletteEntry[] BuildPalette()
    {
        var palette = new PaletteEntry[256];
        for (int i = 0; i < 256; i++)
        {
            int rIdx = (i >> 5) & 0x7;
            int gIdx = (i >> 2) & 0x7;
            int bIdx = i & 0x3;
            palette[i] = new PaletteEntry(
                R: (byte)(rIdx == 7 ? 255 : rIdx * 36),
                G: (byte)(gIdx == 7 ? 255 : gIdx * 36),
                B: (byte)(bIdx == 3 ? 255 : bIdx * 85));
        }
        return palette;
    }

    /// <summary>Converts each pixel in <paramref name="bitmap"/> to its nearest palette index.</summary>
    internal static byte[] MapToIndices(SKBitmap bitmap)
    {
        int total = bitmap.Width * bitmap.Height;
        var indices = new byte[total];
        var span = bitmap.GetPixelSpan();

        for (int i = 0; i < total; i++)
        {
            // SkiaSharp stores pixels as BGRA in little-endian order
            int offset = i * 4;
            byte b = span[offset];
            byte g = span[offset + 1];
            byte r = span[offset + 2];
            // byte a = span[offset + 3];   // alpha ignored

            int rIdx = r >> 5;
            int gIdx = g >> 5;
            int bIdx = b >> 6;
            indices[i] = (byte)((rIdx << 5) | (gIdx << 2) | bIdx);
        }
        return indices;
    }
}
