// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="ReferenceLine.Label"/> is rendered into the SVG output.</summary>
public class ReferenceLineLabelRenderTests
{
    /// <summary>The markup of a diagonal line on <paramref name="figure"/> with the reference line <paramref name="add"/> puts on it.</summary>
    private static string ReferenceLineSvg(Figure figure, Action<Axes> add)
    {
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        add(ax);
        return figure.ToSvg();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that a horizontal reference line with a label renders the label text in SVG.
        new DrawnToken("HorizontalReferenceLine_WithLabel_RendersLabelText", () => ReferenceLineSvg(new Figure(), ax => ax.AxHLine(1.5).Label = "threshold"), "threshold"),
        // Verifies that a vertical reference line with a label renders the label text in SVG.
        new DrawnToken("VerticalReferenceLine_WithLabel_RendersLabelText", () => ReferenceLineSvg(new Figure(), ax => ax.AxVLine(1.5).Label = "boundary"), "boundary"),
        // Verifies that a reference line without a label does not render any spurious label text.
        // no label
        new DrawnToken("ReferenceLine_WithoutLabel_NoExtraText", () => ReferenceLineSvg(new Figure(), ax => ax.AxHLine(1.5))) { Absent = ["nolabel_sentinel"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that a horizontal reference line label is positioned near the right edge of the plot area.</summary>
    [Fact]
    public void HorizontalReferenceLine_LabelPosition_NearRightEdge()
    {
        string svg = ReferenceLineSvg(new Figure { Width = 800, Height = 600 }, ax => ax.AxHLine(1.5).Label = "refhlabel");

        // Find the x position of the text element containing our label
        var match = svg.Elements("text").FirstOrDefault(text => text.Text.Contains("refhlabel", StringComparison.Ordinal));
        Assert.True(match.Attributes is not null, "Expected a <text> element with 'refhlabel' in SVG");
        double x = match.Number("x");

        // Label should be in the right half of an 800px-wide figure (right edge of plot area)
        Assert.True(x > 400, $"Expected label x > 400 (right edge), got {x}");
    }

    /// <summary>Verifies that a vertical reference line label is positioned near the top of the plot area.</summary>
    [Fact]
    public void VerticalReferenceLine_LabelPosition_NearTopEdge()
    {
        string svg = ReferenceLineSvg(new Figure { Width = 800, Height = 600 }, ax => ax.AxVLine(1.5).Label = "refvlabel");

        // Find the y position of the text element containing our label
        var match = svg.Elements("text").FirstOrDefault(text => text.Text.Contains("refvlabel", StringComparison.Ordinal));
        Assert.True(match.Attributes is not null, "Expected a <text> element with 'refvlabel' in SVG");
        double y = match.Number("y");

        // Label should be in the top quarter of a 600px-high figure
        Assert.True(y < 200, $"Expected label y < 200 (near top), got {y}");
    }
}
