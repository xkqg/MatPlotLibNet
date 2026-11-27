// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="SpanRegion"/> border lines and labels are rendered into SVG.</summary>
public class SpanRegionRenderTests
{
    /// <summary>Verifies that a horizontal span with a solid border, and a vertical one with a dashed border, render additional line elements.</summary>
    [Theory]
    [InlineData(true, LineStyle.Solid)]
    [InlineData(false, LineStyle.Dashed)]
    public void ASpan_WithBorder_RendersBorderLines(bool horizontal, LineStyle style)
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        string svgWithout = figure.ToSvg(); // baseline

        var span = horizontal ? ax.AxHSpan(1.0, 2.0) : ax.AxVSpan(1.0, 2.0);
        span.LineStyle = style;
        span.EdgeColor = Colors.Black;
        string svgWith = figure.ToSvg();

        // Border adds line elements
        int linesBefore = svgWithout.CountOf("<line");
        int linesAfter  = svgWith.CountOf("<line");
        Assert.True(linesAfter > linesBefore, "Expected more line elements with border");
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that a span with a label renders the label text in SVG.
        new DrawnToken("Span_WithLabel_RendersLabelText", () =>
        {
            var figure = new Figure();
            var ax = figure.AddSubPlot();
            ax.Plot([0.0, 3.0], [0.0, 3.0]);
            var span = ax.AxHSpan(1.0, 2.0);
            span.Label = "support zone";
            return figure.ToSvg();
        }, "support zone"),
        // Verifies that a span without a label does not render spurious label text.
        // no label
        new DrawnToken("Span_WithoutLabel_NoLabelText", () =>
        {
            var figure = new Figure();
            var ax = figure.AddSubPlot();
            ax.Plot([0.0, 3.0], [0.0, 3.0]);
            ax.AxHSpan(1.0, 2.0);
            return figure.ToSvg();
        }) { Absent = ["nolabel_sentinel"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that a span without border enabled does not add extra line elements.</summary>
    [Fact]
    public void Span_WithoutBorder_NoBorderLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        string svgWithout = figure.ToSvg();

        ax.AxHSpan(1.0, 2.0); // LineStyle defaults to None
        string svgWith = figure.ToSvg();

        int linesBefore = svgWithout.CountOf("<line");
        int linesAfter  = svgWith.CountOf("<line");
        Assert.Equal(linesBefore, linesAfter);
    }
}
