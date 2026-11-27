// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An SVG writer says itself whether it can draw a figure as a document in its parts, the form a publisher sends the
/// parts that changed from (<see cref="ISvgRenderer"/>'s default member): the library's own writer can, and draws the document
/// its <see cref="SvgTransform.Render"/> writes; a writer of the caller's cannot, so a publisher sends it whole, by asking the
/// writer instead of testing its type.</summary>
public class SvgRendererDocumentTests
{
    private static Figure TwoPanels() => Plt.Create().WithSize(300, 200)
        .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 2, 1]))
        .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1, 2], [2.0, 1, 2]))
        .Build();

    private sealed class CallersWriter : ISvgRenderer
    {
        public string Render(Figure figure) => "<svg/>";
    }

    [Fact]
    public void AWriterOfTheCallers_HasNoDocumentInParts()
    {
        ISvgRenderer writer = new CallersWriter();

        Assert.Null(writer.RenderDocument(TwoPanels()));
    }

    [Fact]
    public void TheLibrarysWriter_DrawsTheDocumentInParts_ThatItsRenderWrites()
    {
        ISvgRenderer writer = new SvgTransform();

        var document = writer.RenderDocument(TwoPanels());

        Assert.NotNull(document);
        Assert.Equal(2, document.Axes.Count);
        Assert.Equal(new SvgTransform().Render(TwoPanels()), document.ToSvg());
    }
}
