// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Minimal GIF89a encoder. Produces animated GIFs from a sequence of SKBitmaps.</summary>
/// <remarks>
/// Uses 3-3-2 uniform colour quantization (256-colour global palette) and GIF LZW compression.
/// All frames share the global palette for minimum file size.
/// </remarks>
internal static class GifEncoder
{
    private const int MinCodeSize = 8; // 256-color palette → 8-bit LZW
    private static readonly (byte R, byte G, byte B)[] Palette = ColorQuantizer.BuildPalette();

    /// <summary>Encodes <paramref name="frames"/> into a GIF89a byte stream written to <paramref name="output"/>.</summary>
    /// <param name="frames">Bitmaps to encode (all must have the same dimensions).</param>
    /// <param name="delayMs">Frame delay in milliseconds (minimum 10 ms, clamped to GIF 1/100 s grid).</param>
    /// <param name="loop">When <see langword="true"/>, writes a Netscape 2.0 loop-forever extension.</param>
    /// <param name="output">Destination stream.</param>
    public static void Encode(IReadOnlyList<SKBitmap> frames, int delayMs, bool loop, Stream output)
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
        var indices = ColorQuantizer.MapToIndices(bitmap);
        w.Write((byte)MinCodeSize);
        var compressed = LzwCompress(indices);
        w.Write(compressed);
    }

    // --- LZW compression ---

    private static byte[] LzwCompress(byte[] pixels)
    {
        int clearCode = 1 << MinCodeSize; // 256
        int eoi = clearCode + 1;          // 257

        var packer = new BitPacker();
        var table = new Dictionary<(int prefix, int suffix), int>(4096);
        int nextCode = eoi + 1;
        int codeSize = MinCodeSize + 1;
        int maxCode = 1 << codeSize;

        packer.Write(clearCode, codeSize);

        int prefix = pixels[0];
        for (int i = 1; i < pixels.Length; i++)
        {
            int suffix = pixels[i];
            var key = (prefix, suffix);
            if (table.TryGetValue(key, out int existing))
            {
                prefix = existing;
            }
            else
            {
                packer.Write(prefix, codeSize);
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
                    packer.Write(clearCode, codeSize);
                    table.Clear();
                    nextCode = eoi + 1;
                    codeSize = MinCodeSize + 1;
                    maxCode = 1 << codeSize;
                }
                prefix = suffix;
            }
        }
        packer.Write(prefix, codeSize);
        packer.Write(eoi, codeSize);
        packer.Flush();

        return PackIntoSubBlocks(packer.ToArray());
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

    // --- Bit packer (LSB-first within each byte, as required by GIF LZW) ---

    internal sealed class BitPacker
    {
        private readonly List<byte> _bytes = [];
        private int _pending;
        private int _bitsInPending;

        public void Write(int code, int bits)
        {
            _pending |= code << _bitsInPending;
            _bitsInPending += bits;
            while (_bitsInPending >= 8)
            {
                _bytes.Add((byte)(_pending & 0xFF));
                _pending >>= 8;
                _bitsInPending -= 8;
            }
        }

        public void Flush()
        {
            if (_bitsInPending > 0)
            {
                _bytes.Add((byte)(_pending & 0xFF));
                _pending = 0;
                _bitsInPending = 0;
            }
        }

        public byte[] ToArray() => [.. _bytes];
    }
}
