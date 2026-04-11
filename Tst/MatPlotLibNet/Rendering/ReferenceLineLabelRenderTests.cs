// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="ReferenceLine.Label"/> is rendered into the SVG output.</summary>
public class ReferenceLineLabelRenderTests
{
    /// <summary>Verifies that a horizontal reference line with a label renders the label text in SVG.</summary>
    [Fact]
    public void HorizontalReferenceLine_WithLabel_RendersLabelText()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        var line = ax.AxHLine(1.5);
        line.Label = "threshold";

        string svg = figure.ToSvg();

        Assert.Contains("threshold", svg);
    }

    /// <summary>Verifies that a vertical reference line with a label renders the label text in SVG.</summary>
    [Fact]
    public void VerticalReferenceLine_WithLabel_RendersLabelText()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        var line = ax.AxVLine(1.5);
        line.Label = "boundary";

        string svg = figure.ToSvg();

        Assert.Contains("boundary", svg);
    }

    /// <summary>Verifies that a reference line without a label does not render any spurious label text.</summary>
    [Fact]
    public void ReferenceLine_WithoutLabel_NoExtraText()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        ax.AxHLine(1.5); // no label

        string svg = figure.ToSvg();

        Assert.DoesNotContain("nolabel_sentinel", svg);
    }

    /// <summary>Verifies that a horizontal reference line label is positioned near the right edge of the plot area.</summary>
    [Fact]
    public void HorizontalReferenceLine_LabelPosition_NearRightEdge()
    {
        var figure = new Figure { Width = 800, Height = 600 };
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        var line = ax.AxHLine(1.5);
        line.Label = "refhlabel";

        string svg = figure.ToSvg();

        // Find the x position of the text element containing our label
        var match = Regex.Match(svg, @"<text x=""([0-9.]+)""[^>]*>[^<]*refhlabel[^<]*</text>");
        Assert.True(match.Success, "Expected a <text> element with 'refhlabel' in SVG");
        double x = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        // Label should be in the right half of an 800px-wide figure (right edge of plot area)
        Assert.True(x > 400, $"Expected label x > 400 (right edge), got {x}");
    }

    /// <summary>Verifies that a vertical reference line label is positioned near the top of the plot area.</summary>
    [Fact]
    public void VerticalReferenceLine_LabelPosition_NearTopEdge()
    {
        var figure = new Figure { Width = 800, Height = 600 };
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        var line = ax.AxVLine(1.5);
        line.Label = "refvlabel";

        string svg = figure.ToSvg();

        // Find the y position of the text element containing our label
        var match = Regex.Match(svg, @"<text x=""[0-9.]+""[^>]*y=""([0-9.]+)""[^>]*>[^<]*refvlabel[^<]*</text>");
        Assert.True(match.Success, "Expected a <text> element with 'refvlabel' in SVG");
        double y = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        // Label should be in the top quarter of a 600px-high figure
        Assert.True(y < 200, $"Expected label y < 200 (near top), got {y}");
    }
}
