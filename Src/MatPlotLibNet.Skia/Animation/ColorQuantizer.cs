// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Reduces an image to a 256-color palette using 3-3-2 uniform quantization
/// (3 bits red, 3 bits green, 2 bits blue = 8 × 8 × 4 = 256 colors exactly).</summary>
/// <remarks>
/// This is a fast, lossless-in-palette approach — no dithering. Quality is adequate
/// for most chart output where large blocks of solid color dominate.
/// </remarks>
internal static class ColorQuantizer
{
    /// <summary>Builds a 256-entry RGB palette using the 3-3-2 colour space.</summary>
    public static (byte R, byte G, byte B)[] BuildPalette()
    {
        var palette = new (byte R, byte G, byte B)[256];
        for (int i = 0; i < 256; i++)
        {
            int rIdx = (i >> 5) & 0x7;
            int gIdx = (i >> 2) & 0x7;
            int bIdx = i & 0x3;
            palette[i] = (
                R: (byte)(rIdx == 7 ? 255 : rIdx * 36),
                G: (byte)(gIdx == 7 ? 255 : gIdx * 36),
                B: (byte)(bIdx == 3 ? 255 : bIdx * 85));
        }
        return palette;
    }

    /// <summary>Converts each pixel in <paramref name="bitmap"/> to its nearest palette index.</summary>
    public static byte[] MapToIndices(SKBitmap bitmap)
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
