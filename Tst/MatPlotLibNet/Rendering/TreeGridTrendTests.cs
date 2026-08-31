// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A row may carry its own SHAPE OVER TIME beside its numbers: <see cref="TreeGridRow.Trend"/>, the same
/// field <see cref="StatTileSeries.Trend"/> already has, drawn by the same <see cref="SparklineSeries"/> renderer.
/// The sparkline scales to ITS OWN values, so a row at 1 % has a shape instead of a flat line on the floor of
/// somebody else's axis.</summary>
public class TreeGridTrendTests
{
    private static string Svg(params TreeGridRow[] rows)
        => Plt.Create().WithSize(900, 200)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(rows, s => s.ColumnHeaders = ["CPU"]))
            .ToSvg();

    private static int Polylines(string svg) => Regex.Count(svg, "<polyline");

    [Fact]
    public void ARowWithATrend_DrawsASparkline()
        => Assert.Equal(1, Polylines(Svg(new TreeGridRow("Ait.Bus", ["12 %"]) { Trend = [3, 9, 4, 14, 6, 11] })));

    [Fact]
    public void ARowWithoutATrend_DrawsNoLine()
        => Assert.Equal(0, Polylines(Svg(new TreeGridRow("Ait.Bus", ["12 %"]))));

    /// <summary>Two points is a line; one is a dot with no shape, and the stat tile draws nothing below two
    /// either — one rule for the whole wall.</summary>
    [Fact]
    public void ASingleSample_IsNotAShape()
        => Assert.Equal(0, Polylines(Svg(new TreeGridRow("Ait.Bus", ["12 %"]) { Trend = [7] })));

    [Fact]
    public void EveryRowWithATrend_GetsItsOwnLine()
    {
        string svg = Svg(
            new TreeGridRow("Ait", ["29 %"]) { Trend = [1, 5, 2, 6] },
            new TreeGridRow("Ait.Binance", ["1 %"]) { Depth = 1, Trend = [1, 1, 2, 1] },
            new TreeGridRow("Ait.Forge", ["silent"]) { Depth = 1 });

        Assert.Equal(2, Polylines(svg));
    }

    /// <summary>The sparkline is INSIDE the name column, never over the numbers: the digits are what the row is
    /// read for, and a line across them is the one thing a grid may not do.</summary>
    [Fact]
    public void TheSparkline_StaysLeftOfTheNumberColumns()
    {
        string svg = Plt.Create().WithSize(900, 200)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(
                [new TreeGridRow("Ait.Bus", ["12 %"]) { Trend = [3, 9, 4, 14] }],
                s => { s.ColumnHeaders = ["CPU"]; s.ColumnWidth = 120; s.TrendWidth = 200; }))
            .ToSvg();

        var points = Regex.Match(svg, "<polyline[^>]*points=\"([^\"]+)\"").Groups[1].Value;
        double rightmost = points.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => double.Parse(p.Split(',')[0], CultureInfo.InvariantCulture)).Max();

        var numbers = Regex.Matches(svg, "<text[^>]*x=\"([0-9.]+)\"[^>]*>12 %<")
            .Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();

        Assert.NotEmpty(numbers);
        Assert.True(rightmost < numbers.Min() - 100,
            $"the line ends at {rightmost}, the column it must not cross starts near {numbers.Min()}");
    }

    [Fact]
    public void TheTrend_TakesTheInkItIsGiven()
    {
        string svg = Svg(new TreeGridRow("Ait.Bus", ["12 %"])
        {
            Trend = [3, 9, 4, 14], TrendColor = Color.FromHex("#E69F00"),
        });

        Assert.Contains("#E69F00", svg, StringComparison.Ordinal);
    }
}
