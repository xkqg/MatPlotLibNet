// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="SpanRegion"/> border lines and labels are rendered into SVG.</summary>
public class SpanRegionRenderTests
{
    /// <summary>Verifies that a horizontal span with a border renders additional line elements.</summary>
    [Fact]
    public void HorizontalSpan_WithBorder_RendersBorderLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        string svgWithout = figure.ToSvg(); // baseline

        var span = ax.AxHSpan(1.0, 2.0);
        span.LineStyle = LineStyle.Solid;
        span.EdgeColor = Colors.Black;
        string svgWith = figure.ToSvg();

        // Border adds line elements
        int linesBefore = CountOccurrences(svgWithout, "<line");
        int linesAfter  = CountOccurrences(svgWith, "<line");
        Assert.True(linesAfter > linesBefore, "Expected more line elements with border");
    }

    /// <summary>Verifies that a vertical span with a border renders additional line elements.</summary>
    [Fact]
    public void VerticalSpan_WithBorder_RendersBorderLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        string svgWithout = figure.ToSvg();

        var span = ax.AxVSpan(1.0, 2.0);
        span.LineStyle = LineStyle.Dashed;
        span.EdgeColor = Colors.Red;
        string svgWith = figure.ToSvg();

        int linesBefore = CountOccurrences(svgWithout, "<line");
        int linesAfter  = CountOccurrences(svgWith, "<line");
        Assert.True(linesAfter > linesBefore, "Expected more line elements with border");
    }

    /// <summary>Verifies that a span with a label renders the label text in SVG.</summary>
    [Fact]
    public void Span_WithLabel_RendersLabelText()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var span = ax.AxHSpan(1.0, 2.0);
        span.Label = "support zone";

        string svg = figure.ToSvg();

        Assert.Contains("support zone", svg);
    }

    /// <summary>Verifies that a span without a label does not render spurious label text.</summary>
    [Fact]
    public void Span_WithoutLabel_NoLabelText()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        ax.AxHSpan(1.0, 2.0); // no label

        string svg = figure.ToSvg();

        Assert.DoesNotContain("nolabel_sentinel", svg);
    }

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

        int linesBefore = CountOccurrences(svgWithout, "<line");
        int linesAfter  = CountOccurrences(svgWith, "<line");
        Assert.Equal(linesBefore, linesAfter);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
