// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies <see cref="SvgTransform"/> SVG output behavior.</summary>
public class SvgOutputTests
{
    /// <summary>Verifies that a simple line chart produces valid SVG with opening/closing tags and title.</summary>
    [Fact]
    public void SimpleLineChart_ProducesValidSvg()
    {
        string svg = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg();

        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
        Assert.Contains("Test", svg); // title appears
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that a scatter chart renders circle elements for each data point.
        new DrawnToken("ScatterChart_ContainsCircleElements", () => Plt.Create()
            .Scatter([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg(), "<circle"),
        // Verifies that the SVG output includes a viewBox attribute matching the configured size.
        new DrawnToken("SvgHasViewBox", () => Plt.Create()
            .WithSize(1024, 768)
            .ToSvg(), "viewBox=", "1024", "768"),
        // Verifies that a radar chart renders as a polygon element.
        new DrawnToken("RadarChart_ContainsPolygonElement", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C", "D"], [3.0, 5.0, 2.0, 4.0]))
            .ToSvg(), "<polygon"),
        // Verifies that an error bar chart renders line and circle elements for whiskers and points.
        new DrawnToken("ErrorBarChart_ContainsLineElements", () => Plt.Create()
            .ErrorBar([1.0, 2.0, 3.0], [10.0, 20.0, 15.0], [1.0, 1.0, 1.0], [2.0, 2.0, 2.0])
            .ToSvg(), "<line", "<circle"),
        // Verifies that a step chart renders as a polyline element.
        new DrawnToken("StepChart_ContainsPolylineElement", () => Plt.Create()
            .Step([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg(), "<polyline"),
        // Verifies that a fill-between area chart renders as a polygon element.
        new DrawnToken("AreaChart_ContainsPolygonElement", () => Plt.Create()
            .FillBetween([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg(), "<polygon"),
        // Verifies that an annotation appears as text in the SVG output.
        new DrawnToken("AnnotatedChart_ContainsTextElement", () =>
        {
            var figure = new Figure();
            var axes = figure.AddSubPlot();
            axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]);
            axes.Annotate("peak", 2.0, 20.0);
            return figure.ToSvg();
        }, "peak"),
        // Verifies that a horizontal reference line renders as a line element.
        new DrawnToken("ReferenceLineChart_ContainsLineElement", () =>
        {
            var figure = new Figure();
            var axes = figure.AddSubPlot();
            axes.Plot([1.0, 2.0], [3.0, 4.0]);
            axes.AxHLine(3.5);
            return figure.ToSvg();
        }, "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
