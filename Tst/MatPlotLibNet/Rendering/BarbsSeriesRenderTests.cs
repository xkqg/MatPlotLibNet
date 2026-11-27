// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="BarbsSeries"/> rendering.</summary>
public class BarbsSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [0.0, 0.0, 0.0, 0.0];
    private static readonly double[] Speed = [5.0, 15.0, 25.0, 55.0];
    private static readonly double[] Dir = [0.0, 45.0, 90.0, 270.0];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Barbs_SvgContainsLines", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs(X, Y, Speed, Dir))
            .ToSvg(), "<line"),
        // BarbsSeriesRenderer: i >= series.Speed.Length ternary false-branch. The verb refuses arrays that do not pair, so the
        // series is built directly: the renderer's own guard is what is characterised.
        new DrawnToken("Barbs_SpeedArrayShorterThanPositions_FallsBackToZero", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new BarbsSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0, 0.0 }, new double[] { 20.0 }, new double[] { 45.0, 90.0 })))
            .ToSvg(), "<line"),
        // BarbsSeriesRenderer: i >= series.Direction.Length ternary false-branch (built directly, as above).
        new DrawnToken("Barbs_DirectionArrayShorterThanPositions_FallsBackToZero", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new BarbsSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0, 0.0 }, new double[] { 20.0, 30.0 }, new double[] { 45.0 })))
            .ToSvg(), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Branch coverage ──────────────────────────────────────────────────────

    /// <summary>A barb whose speed or direction is missing (NaN) or infinite is left out with its staff, as matplotlib's <c>Barbs</c>
    /// deletes a masked point (<c>cbook.delete_masked_points</c>). An infinite speed used to hang the draw: the renderer drew a flag
    /// for every 50 knots of it, forever.</summary>
    [Theory]
    [InlineData(double.NaN, 45)]
    [InlineData(double.PositiveInfinity, 45)]
    [InlineData(20, double.NaN)]
    [InlineData(20, double.NegativeInfinity)]
    public void ABarbWithAMissingOrInfiniteSpeedOrDirection_IsLeftOut(double speed, double direction)
    {
        var withGap = Plt.Create().WithTheme(MatPlotLibNet.Styling.Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([0.0, 1.0], [0.0, 0.0], [speed, 20.0], [direction, 90.0]).SetXLim(-1, 2).SetYLim(-1, 1))
            .ToSvg();
        var without = Plt.Create().WithTheme(MatPlotLibNet.Styling.Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([1.0], [0.0], [20.0], [90.0]).SetXLim(-1, 2).SetYLim(-1, 1))
            .ToSvg();

        Assert.Equal(without, withGap);
    }
}
