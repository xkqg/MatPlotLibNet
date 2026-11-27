// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms.Animation;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests.Animation;

/// <summary>Verifies animated GIF output: header, dimensions, frame count, loop extension, delay.</summary>
public class GifTransformTests
{
    /// <summary>Builds a minimal animation with <paramref name="frameCount"/> identical frames.</summary>
    private static AnimationBuilder MinimalAnimation(int frameCount = 1, int intervalMs = 100) =>
        new(frameCount, _ => new FigureBuilder()
            .WithSize(8, 8)
            .Plot([0.0, 1.0], [0.0, 1.0])
            .Build())
        {
            Interval = TimeSpan.FromMilliseconds(intervalMs),
            Loop     = true
        };

    private static byte[] ToGifBytes(AnimationBuilder anim) => anim.ToGif();

    [Fact]
    public void SingleFrame_StartsWithGif89aHeader()
    {
        var bytes = ToGifBytes(MinimalAnimation());
        Assert.Equal(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, bytes[..6]);
    }

    [Fact]
    public void SingleFrame_EndsWith_GifTrailer()
    {
        var bytes = ToGifBytes(MinimalAnimation());
        Assert.Equal(0x3B, bytes[^1]);
    }

    [Fact]
    public void LogicalScreenDescriptor_CorrectDimensions()
    {
        // Figure width = 8, height = 8
        var bytes = ToGifBytes(MinimalAnimation());
        ushort width  = (ushort)(bytes[6] | (bytes[7] << 8));
        ushort height = (ushort)(bytes[8] | (bytes[9] << 8));
        Assert.Equal(8, width);
        Assert.Equal(8, height);
    }

    [Fact]
    public void WithLoop_ContainsNetscapeExtension()
    {
        var bytes = ToGifBytes(MinimalAnimation());
        var netscape = "NETSCAPE2.0"u8.ToArray();
        Assert.True(FindSequence(bytes, netscape) >= 0, "NETSCAPE2.0 extension not found");
    }

    [Fact]
    public void WithoutLoop_NoNetscapeExtension()
    {
        var anim = new AnimationBuilder(1, _ => new FigureBuilder().WithSize(8, 8).Plot([0.0, 1.0], [0.0, 1.0]).Build())
        {
            Loop = false
        };
        var bytes = anim.ToGif();
        var netscape = "NETSCAPE2.0"u8.ToArray();
        Assert.True(FindSequence(bytes, netscape) < 0, "Unexpected NETSCAPE2.0 in non-looping GIF");
    }

    [Fact]
    public void FrameDelay_100ms_Stores10HundredthsOfSecond()
    {
        var bytes = ToGifBytes(MinimalAnimation(intervalMs: 100));
        int gce = FindGce(bytes);
        Assert.True(gce >= 0, "Graphic Control Extension not found");
        ushort delay = (ushort)(bytes[gce + 4] | (bytes[gce + 5] << 8));
        Assert.Equal(10, delay); // 100 ms = 10 hundredths
    }

    [Fact]
    public void FrameDelay_500ms_Stores50HundredthsOfSecond()
    {
        var bytes = ToGifBytes(MinimalAnimation(intervalMs: 500));
        int gce = FindGce(bytes);
        ushort delay = (ushort)(bytes[gce + 4] | (bytes[gce + 5] << 8));
        Assert.Equal(50, delay);
    }

    [Fact]
    public void MultiFrame_ProducesMoreBytes_ThanSingleFrame()
    {
        var single = ToGifBytes(MinimalAnimation(1));
        var multi  = ToGifBytes(MinimalAnimation(3));
        Assert.True(multi.Length > single.Length);
    }

    [Fact]
    public void ToGif_IAnimationTransform_Implemented()
    {
        Assert.IsAssignableFrom<IAnimationTransform>(new GifTransform());
    }

    [Fact]
    public void SaveGif_WritesValidFile()
    {
        var path = Path.GetTempFileName() + ".gif";
        try
        {
            MinimalAnimation(2).SaveGif(path);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, bytes[..6]);
            Assert.Equal(0x3B, bytes[^1]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── Phase J coverage additions ───────────────────────────────────────────

    /// <summary>Empty frame list → Encode returns immediately without writing.</summary>
    [Fact]
    public void Encode_EmptyFrames_WritesNothing()
    {
        var ms = new MemoryStream();
        GifTransform.Encode([], 100, false, ms);
        Assert.Equal(0, ms.Length);
    }

    /// <summary>LZW table fills (nextCode &gt; 4095) → encoder emits clear code
    /// and resets the table. A 90×90 high-entropy bitmap generates enough unique
    /// (prefix, suffix) patterns to exhaust all 3838 available table slots.</summary>
    [Fact]
    public void Encode_HighEntropyBitmap_FillsLzwTableAndResets()
    {
        using var bitmap = new SKBitmap(90, 90, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (int y = 0; y < 90; y++)
        for (int x = 0; x < 90; x++)
        {
            byte r = (byte)((x * y + x * 13 + 7) & 0xFF);
            byte g = (byte)((x * 3  + y * 97 + 5) & 0xFF);
            byte b = (byte)((x * 59 + y * 11 + 3) & 0xFF);
            bitmap.SetPixel(x, y, new SKColor(r, g, b));
        }
        var ms = new MemoryStream();
        GifTransform.Encode((IReadOnlyList<SKBitmap>)new[] { bitmap }, 100, false, ms);
        var bytes = ms.ToArray();
        Assert.Equal(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, bytes[..6]);
        Assert.Equal(0x3B, bytes[^1]);
    }

    /// <summary>Codes that end on a byte boundary leave no byte to fill out: six pixels that each start a new string write eight
    /// codes of nine bits (the clear code, six strings, the end), 72 bits, so the sub-block holds exactly nine bytes.</summary>
    [Fact]
    public void CodesThatEndOnAByteBoundary_AddNoPaddingByte()
    {
        var packed = GifTransform.LzwCompress([0, 1, 2, 3, 4, 5]);

        Assert.Equal(9, packed[0]);
        Assert.Equal(11, packed.Length);
        Assert.Equal(0, packed[^1]);
    }

    // --- Helpers ---

    private static int FindSequence(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    private static int FindGce(byte[] bytes)
    {
        for (int i = 0; i < bytes.Length - 2; i++)
            if (bytes[i] == 0x21 && bytes[i + 1] == 0xF9 && bytes[i + 2] == 0x04)
                return i;
        return -1;
    }

    // ---- the palette the frames are quantised to (BuildPalette) ----
    // The 3-3-2 palette a GIF frame is quantized to: 3 bits of red, 3 of green and 2 of blue, 256 colours exactly.

    [Fact]
    public void ThePalette_HasTwoHundredAndFiftySixEntries() =>
        Assert.Equal(256, GifTransform.BuildPalette().Length);

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255)]            // the top level of each channel is full scale
    [InlineData(0b100_000_00, 144, 0, 0)]       // red index 4 -> 4 * 36
    [InlineData(0b000_011_00, 0, 108, 0)]       // green index 3 -> 3 * 36
    [InlineData(0b000_000_10, 0, 0, 170)]       // blue index 2 -> 2 * 85
    [InlineData(0b111_000_00, 255, 0, 0)]       // red index 7 is 255, not 252
    [InlineData(0b000_111_00, 0, 255, 0)]
    [InlineData(0b000_000_11, 0, 0, 255)]       // blue index 3 is 255
    public void AnEntry_SplitsItsIndexIntoThreeBitsRedThreeGreenTwoBlue(int index, int red, int green, int blue)
    {
        var entry = GifTransform.BuildPalette()[index];

        Assert.Equal((byte)red, entry.R);
        Assert.Equal((byte)green, entry.G);
        Assert.Equal((byte)blue, entry.B);
    }

    [Fact]
    public void AnEntry_DeconstructsToItsThreeChannels()
    {
        var (r, g, b) = GifTransform.BuildPalette()[0b010_101_01];

        Assert.Equal((72, 180, 85), ((int)r, (int)g, (int)b));
    }

    [Fact]
    public void MapToIndices_GivesEachPixelTheIndexOfTheEntryItRoundsDownTo()
    {
        using var bitmap = new SKBitmap(2, 1);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0));
        bitmap.SetPixel(1, 0, new SKColor(0, 0, 255));

        var indices = GifTransform.MapToIndices(bitmap);

        Assert.Equal(new byte[] { 0b111_000_00, 0b000_000_11 }, indices);
        var palette = GifTransform.BuildPalette();
        Assert.Equal(new GifTransform.PaletteEntry(255, 0, 0), palette[indices[0]]);
        Assert.Equal(new GifTransform.PaletteEntry(0, 0, 255), palette[indices[1]]);
    }
}
