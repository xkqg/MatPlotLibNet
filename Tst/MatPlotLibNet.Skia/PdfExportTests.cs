// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Verifies <see cref="PdfTransform"/> behavior.</summary>
public class PdfExportTests
{
    /// <summary>Verifies that PdfTransform implements the IFigureTransform interface.</summary>
    [Fact]
    public void PdfTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new PdfTransform());
    }

    /// <summary>Verifies that PdfTransform extends the FigureTransform base class.</summary>
    [Fact]
    public void PdfTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new PdfTransform());
    }

    /// <summary>Verifies that ToPdf produces a non-empty byte array for a figure with data.</summary>
    [Fact]
    public void ToPdf_ProducesNonEmptyByteArray()
    {
        var figure = Plt.Create()
            .WithTitle("Test PDF")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        Assert.NotEmpty(figure.ToPdf());
    }

    /// <summary>Verifies that ToPdf output starts with the %PDF magic header bytes.</summary>
    [Fact]
    public void ToPdf_StartsWithPdfHeader()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var bytes = figure.ToPdf();
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    /// <summary>Verifies that the fluent Transform API produces bytes with a valid PDF header.</summary>
    [Fact]
    public void FluentTransform_ToBytes_ProducesValidPdf()
    {
        byte[] bytes = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Transform(new PdfTransform()).ToBytes();

        Assert.Equal((byte)'%', bytes[0]);
    }
}
