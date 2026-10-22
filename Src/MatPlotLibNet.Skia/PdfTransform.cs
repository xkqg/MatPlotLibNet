// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into PDF format using SkiaSharp.</summary>
/// <remarks>A page is as large as the figure, one point to a unit, and holds vector drawing, so the resolution of a figure
/// or of <see cref="SaveOptions.Dpi"/> has nothing to size and changes nothing. Handed <see cref="SaveOptions"/>, the
/// transform reads <see cref="SaveOptions.Title"/> and <see cref="SaveOptions.Author"/> and writes them into the
/// document information; the settings that belong to an SVG or a PNG are not used.</remarks>
public sealed class PdfTransform : FigureTransform
{
    /// <summary>Creates a new PDF transform with the specified chart renderer.</summary>
    public PdfTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Creates a new PDF transform with the default chart renderer.</summary>
    public PdfTransform() { }

    /// <summary>Transforms the figure and writes PDF bytes to the output stream.</summary>
    public override void Transform(Figure figure, Stream output) => Transform(figure, output, new SaveOptions());

    /// <summary>Transforms the figure and writes PDF bytes to the output stream, with the title and the author of
    /// <paramref name="options"/> in the document information. Options that carry neither write the same file as
    /// <see cref="Transform(Figure, Stream)"/>.</summary>
    public override void Transform(Figure figure, Stream output, SaveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        float width = (float)figure.Width;
        float height = (float)figure.Height;

        string? title = options.TitleOrNull();
        string? author = options.AuthorOrNull();
        using var document = title is null && author is null
            ? SKDocument.CreatePdf(output)
            : SKDocument.CreatePdf(output, new SKDocumentPdfMetadata { Title = title, Author = author });
        using var canvas = document.BeginPage(width, height);

        var ctx = new MatPlotLibNet.Skia.SkiaRenderContext(canvas);
        Renderer.Render(figure, ctx);

        document.EndPage();
        document.Close();
    }

    /// <summary>Renders multiple figures as pages in a single PDF document.</summary>
    /// <param name="figures">The figures to render, one per page.</param>
    /// <param name="output">The output stream to write the PDF to.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="figures"/> is empty.</exception>
    public void TransformMultiPage(IReadOnlyList<Figure> figures, Stream output)
    {
        if (figures.Count == 0)
            throw new ArgumentException("At least one figure is required.", nameof(figures));

        using var document = SKDocument.CreatePdf(output);

        foreach (var figure in figures)
        {
            float width = (float)figure.Width;
            float height = (float)figure.Height;

            using var canvas = document.BeginPage(width, height);
            var ctx = new MatPlotLibNet.Skia.SkiaRenderContext(canvas);
            Renderer.Render(figure, ctx);
            document.EndPage();
        }

        document.Close();
    }

    /// <summary>Renders multiple figures as pages in a single PDF file.</summary>
    /// <param name="figures">The figures to render, one per page.</param>
    /// <param name="filePath">The output PDF file path.</param>
    public void TransformMultiPage(IReadOnlyList<Figure> figures, string filePath)
    {
        using var stream = File.Create(filePath);
        TransformMultiPage(figures, stream);
    }
}
