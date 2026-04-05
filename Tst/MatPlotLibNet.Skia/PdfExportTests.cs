// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

public class PdfExportTests
{
    [Fact]
    public void PdfTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new PdfTransform());
    }

    [Fact]
    public void PdfTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new PdfTransform());
    }

    [Fact]
    public void ToPdf_ProducesNonEmptyByteArray()
    {
        var figure = Plt.Create()
            .WithTitle("Test PDF")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        Assert.NotEmpty(figure.ToPdf());
    }

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

    [Fact]
    public void FluentTransform_ToBytes_ProducesValidPdf()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        byte[] bytes = figure.Transform(new PdfTransform()).ToBytes();

        Assert.Equal((byte)'%', bytes[0]);
    }
}
