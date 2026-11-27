// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The dash pattern of an oscillator's lines: the one its caller gave reaches every line of it, and a caller who gave none leaves
/// the pattern to the theme, as matplotlib draws two <c>ax.plot</c> calls that name no <c>linestyle</c>: solid. The stochastic's %D was
/// dashed whatever the caller said.</summary>
public sealed class IndicatorStrokeTests
{
    private static readonly double[] Close = [.. Enumerable.Range(0, 40).Select(i => 100 + 5 * Math.Sin(2 * Math.PI * i / 25) + 3 * Math.Sin(2 * Math.PI * i / 7))];
    private static readonly double[] High = [.. Close.Select(c => c + 1)];
    private static readonly double[] Low = [.. Close.Select(c => c - 1)];

    [Fact]
    public void TheStochasticsDLine_IsLeftToTheTheme_WhenTheCallerGaveNoDashPattern()
    {
        var axes = new Axes();
        new Stochastic(OhlcvBars.Hlc(High, Low, Close), 5, 3).Apply(axes);

        Assert.Equal(2, axes.Series.Count);
        Assert.All(axes.Series.Cast<LineSeries>(), line => Assert.Null(line.Stroke.Style));
    }

    [Fact]
    public void ADashPatternTheCallerGave_ReachesBothLinesOfAStochastic()
    {
        var axes = new Axes();
        new Stochastic(OhlcvBars.Hlc(High, Low, Close), 5, 3) { LineStyle = LineStyle.Dotted }.Apply(axes);

        Assert.All(axes.Series.Cast<LineSeries>(), line => Assert.Equal(LineStyle.Dotted, line.Stroke.Style));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Classic_TheDLineOfAStochastic_IsDrawnSolid", () => Plt.Create().WithSize(500, 400).WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Indicator(new Stochastic(OhlcvBars.Hlc(High, Low, Close), 5, 3))).Build().ToSvg()) { Absent = ["stroke-dasharray"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
