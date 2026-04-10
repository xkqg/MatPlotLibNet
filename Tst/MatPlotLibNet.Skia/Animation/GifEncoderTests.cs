// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia.Tests.Animation;

/// <summary>Verifies animated GIF output: header, dimensions, frame count, loop extension, delay.</summary>
public class GifEncoderTests
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
}
