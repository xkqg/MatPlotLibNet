// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        byte[] bytes = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build().Transform(new PdfTransform()).ToBytes();

        Assert.Equal((byte)'%', bytes[0]);
    }

    // ── Phase X.4.f (v1.7.2, 2026-04-19) — lift PdfTransform from 38.5%L / 0%B → 90%L+ ──

    /// <summary>Constructor with explicit renderer (line 14) — pre-X only the
    /// parameterless ctor was tested.</summary>
    [Fact]
    public void PdfTransform_ExplicitRenderer_ConstructorIsCovered()
    {
        var transform = new PdfTransform(new MatPlotLibNet.Rendering.ChartRenderer());
        Assert.IsAssignableFrom<FigureTransform>(transform);
    }

    /// <summary>TransformMultiPage(IReadOnlyList&lt;Figure&gt;, Stream) (line 39-58)
    /// renders multiple figures as separate pages in one PDF document.</summary>
    [Fact]
    public void TransformMultiPage_StreamOverload_RendersMultiplePages()
    {
        var f1 = Plt.Create().WithTitle("P1").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var f2 = Plt.Create().WithTitle("P2").Plot([5.0, 6.0], [7.0, 8.0]).Build();
        var f3 = Plt.Create().WithTitle("P3").Plot([1.0, 3.0], [2.0, 5.0]).Build();
        using var stream = new MemoryStream();

        new PdfTransform().TransformMultiPage([f1, f2, f3], stream);

        var bytes = stream.ToArray();
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
        Assert.True(bytes.Length > 1000, "Multi-page PDF should be at least 1KB");
    }

    /// <summary>TransformMultiPage with empty list throws ArgumentException (line 41-42).</summary>
    [Fact]
    public void TransformMultiPage_EmptyList_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => new PdfTransform().TransformMultiPage(Array.Empty<MatPlotLibNet.Models.Figure>(), stream));
    }

    /// <summary>TransformMultiPage(IReadOnlyList&lt;Figure&gt;, string) (line 63-67)
    /// writes a multi-page PDF to a file path.</summary>
    [Fact]
    public void TransformMultiPage_FilePathOverload_WritesValidPdf()
    {
        var f1 = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var f2 = Plt.Create().Plot([5.0, 6.0], [7.0, 8.0]).Build();
        var path = Path.GetTempFileName();
        try
        {
            new PdfTransform().TransformMultiPage([f1, f2], path);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal((byte)'%', bytes[0]);
            Assert.True(bytes.Length > 500);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
