// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PdfTransform"/> writes: a PDF file, one page or several.</summary>
public class PdfExportTests : TransformExportTests<PdfTransform>
{
    /// <inheritdoc/>
    protected override byte[] Export(Figure figure) => figure.ToPdf();

    /// <inheritdoc/>
    protected override byte[] Magic => "%PDF"u8.ToArray();

    [Fact]
    public void PdfTransform_ExplicitRenderer_ConstructorIsCovered()
    {
        var transform = new PdfTransform(new MatPlotLibNet.Rendering.ChartRenderer());
        Assert.IsAssignableFrom<FigureTransform>(transform);
    }

    /// <summary>Several figures are written as separate pages of one document.</summary>
    [Fact]
    public void TransformMultiPage_StreamOverload_RendersMultiplePages()
    {
        var f1 = Plt.Create().WithTitle("P1").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var f2 = Plt.Create().WithTitle("P2").Plot([5.0, 6.0], [7.0, 8.0]).Build();
        var f3 = Plt.Create().WithTitle("P3").Plot([1.0, 3.0], [2.0, 5.0]).Build();
        using var stream = new MemoryStream();

        new PdfTransform().TransformMultiPage([f1, f2, f3], stream);

        var bytes = stream.ToArray();
        Assert.Equal(Magic, bytes[..Magic.Length]);
        Assert.True(bytes.Length > 1000, "Multi-page PDF should be at least 1KB");
    }

    [Fact]
    public void TransformMultiPage_EmptyList_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => new PdfTransform().TransformMultiPage(Array.Empty<Figure>(), stream));
    }

    /// <summary>A multi-page document can be written to a file path.</summary>
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
            Assert.Equal(Magic, bytes[..Magic.Length]);
            Assert.True(bytes.Length > 500);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
