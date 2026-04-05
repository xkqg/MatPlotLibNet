// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

public class PngExportTests
{
    [Fact]
    public void PngTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new PngTransform());
    }

    [Fact]
    public void PngTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new PngTransform());
    }

    [Fact]
    public void ToPng_ProducesNonEmptyByteArray()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        Assert.NotEmpty(figure.ToPng());
    }

    [Fact]
    public void ToPng_StartsWithPngHeader()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var bytes = figure.ToPng();
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public void FluentTransform_ToBytes_ProducesValidPng()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        byte[] bytes = figure.Transform(new PngTransform()).ToBytes();

        Assert.Equal(0x89, bytes[0]);
    }

    [Fact]
    public void FluentTransform_ToStream_WritesValidPng()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        using var stream = new MemoryStream();
        figure.Transform(new PngTransform()).ToStream(stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void FluentTransform_ToFile_CreatesValidPng()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            figure.Transform(new PngTransform()).ToFile(path);

            Assert.True(File.Exists(path));
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(0x89, bytes[0]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
