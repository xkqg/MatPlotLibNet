// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia.Tests.Animation;

/// <summary>An animated GIF is a raster export, so each frame is drawn at the resolution of its figure, by the same
/// rule as a PNG: a unit of the figure is 1 pixel at 96 dpi and dpi/96 pixels at any other.</summary>
public class GifResolutionTests
{
    private static Figure Frame(int index, double dpi) =>
        new FigureBuilder().WithSize(20, 20).WithDpi(dpi).Plot([0.0, 1.0], [0.0, index + 1.0]).Build();

    /// <summary>The logical screen size the GIF header states: two little-endian 16-bit numbers after the signature.</summary>
    private static GifScreen ScreenOf(byte[] gif) =>
        new(BinaryPrimitives.ReadUInt16LittleEndian(gif.AsSpan(6, 2)), BinaryPrimitives.ReadUInt16LittleEndian(gif.AsSpan(8, 2)));

    private readonly record struct GifScreen(int Width, int Height);

    private static byte[] Gif(double dpi)
    {
        using var stream = new MemoryStream();
        new FuncAnimation(2, i => Frame(i, dpi)).SaveGif(stream);
        return stream.ToArray();
    }

    [Fact]
    public void DefaultDpi_DrawsOnePixelPerUnit()
    {
        Assert.Equal(new GifScreen(20, 20), ScreenOf(Gif(96)));
    }

    [Fact]
    public void WithDpi192_DrawsTwoPixelsPerUnit()
    {
        Assert.Equal(new GifScreen(40, 40), ScreenOf(Gif(192)));
    }

    [Fact]
    public void ScaledSize_IsRoundedToWholePixels()
    {
        // 20 * 150/96 = 31.25
        Assert.Equal(new GifScreen(31, 31), ScreenOf(Gif(150)));
    }

    [Fact]
    public void ATransformMadeWithARenderer_DrawsEachFrameAtItsResolution()
    {
        using var stream = new MemoryStream();
        var frames = new[] { Frame(0, 192), Frame(1, 192) };

        new GifTransform(new MatPlotLibNet.Rendering.ChartRenderer()).Transform(frames, TimeSpan.FromMilliseconds(50), loop: true, stream);

        Assert.Equal(new GifScreen(40, 40), ScreenOf(stream.ToArray()));
    }

    [Fact]
    public void ADpiNoPictureCanHave_IsRefused()
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Gif(0));

        Assert.Equal("dpi", refusal.ParamName);
    }

    [Fact]
    public void SaveFrames_DrawsEachFrameAtTheResolutionOfItsFigure()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}");
        try
        {
            new FuncAnimation(2, i => Frame(i, 192)).SaveFrames(directory, "frame");

            foreach (string file in Directory.GetFiles(directory, "frame_*.png"))
            {
                byte[] png = File.ReadAllBytes(file);
                Assert.Equal(40, (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)));
                Assert.Equal(40, (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
