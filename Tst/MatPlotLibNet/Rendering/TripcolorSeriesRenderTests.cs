// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="TripcolorSeries"/> rendering.</summary>
public class TripcolorSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 0.0, 1.0];
    private static readonly double[] Y = [0.0, 0.0, 1.0, 1.0, 1.0];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 1.5, 2.5];

    [Fact]
    public void Tripcolor_SvgContainsFilledPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polygon") || svg.Contains("<path"),
            "Expected SVG to contain filled triangle elements");
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Tripcolor_WithAnEmptyTriangleList_DrawsNoTriangle", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z, s => s.Triangles = []))
            .ToSvg()) { Absent = ["<polygon"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
