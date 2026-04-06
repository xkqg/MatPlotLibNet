// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Verifies <see cref="PngTransform"/> behavior.</summary>
public class PngExportTests
{
    /// <summary>Verifies that PngTransform implements the IFigureTransform interface.</summary>
    [Fact]
    public void PngTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new PngTransform());
    }

    /// <summary>Verifies that PngTransform extends the FigureTransform base class.</summary>
    [Fact]
    public void PngTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new PngTransform());
    }

    /// <summary>Verifies that ToPng produces a non-empty byte array for a figure with data.</summary>
    [Fact]
    public void ToPng_ProducesNonEmptyByteArray()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        Assert.NotEmpty(figure.ToPng());
    }

    /// <summary>Verifies that ToPng output starts with the PNG magic header bytes.</summary>
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

    /// <summary>Verifies that the fluent Transform API produces bytes with a valid PNG header.</summary>
    [Fact]
    public void FluentTransform_ToBytes_ProducesValidPng()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        byte[] bytes = figure.Transform(new PngTransform()).ToBytes();

        Assert.Equal(0x89, bytes[0]);
    }

    /// <summary>Verifies that the fluent Transform API writes non-empty PNG data to a stream.</summary>
    [Fact]
    public void FluentTransform_ToStream_WritesValidPng()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        using var stream = new MemoryStream();
        figure.Transform(new PngTransform()).ToStream(stream);

        Assert.True(stream.Length > 0);
    }

    /// <summary>Verifies that the fluent Transform API creates a valid PNG file on disk.</summary>
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
