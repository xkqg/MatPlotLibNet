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
/// document information; the settings that belong to an SVG or a PNG are not used. The document holds no date, so a figure
/// written twice writes the same bytes. PDFs are written one at a time in a process, because Skia's text map of a page is
/// not safe when two are made at once.</remarks>
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

        string? title = options.TitleOrNull();
        string? author = options.AuthorOrNull();
        WritePages([figure], output, title is null && author is null ? null : new SKDocumentPdfMetadata { Title = title, Author = author });
    }

    /// <summary>Renders multiple figures as pages in a single PDF document.</summary>
    /// <param name="figures">The figures to render, one per page.</param>
    /// <param name="output">The output stream to write the PDF to.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="figures"/> is empty.</exception>
    public void TransformMultiPage(IReadOnlyList<Figure> figures, Stream output)
    {
        figures.ThrowIfEmpty("At least one figure is required.");

        WritePages(figures, output, null);
    }

    // Skia fills the text map of a page (which character each glyph stands for) by asking the typeface, and the typeface is
    // one every thread shares. Two documents written at the same moment came out with U+0000 for every glyph in about one
    // of ten, so the same figure wrote different bytes and its text could not be copied. One document at a time keeps the
    // map whole; the drawing costs the same CPU, only a PDF waits for the one before it.
    private static readonly object Gate = new();

    private void WritePages(IReadOnlyList<Figure> figures, Stream output, SKDocumentPdfMetadata? metadata)
    {
        lock (Gate)
        {
            using var document = metadata is null ? SKDocument.CreatePdf(output) : SKDocument.CreatePdf(output, metadata.Value);

            foreach (var figure in figures)
            {
                using var canvas = document.BeginPage((float)figure.Width, (float)figure.Height);
                FigureRenderer.Render(figure, new MatPlotLibNet.Skia.SkiaRenderContext(canvas));
                document.EndPage();
            }

            document.Close();
        }
    }

    /// <summary>Renders multiple figures as pages in a single PDF file.</summary>
    /// <param name="figures">The figures to render, one per page.</param>
    /// <param name="filePath">The output PDF file path.</param>
    /// <remarks>The file is replaced once the whole document is made, so a figure that fails leaves the file that was there.</remarks>
    public void TransformMultiPage(IReadOnlyList<Figure> figures, string filePath) =>
        filePath.ReplaceFileWith(stream => TransformMultiPage(figures, stream));
}
