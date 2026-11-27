// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Xcorr</c> verb (matplotlib's <c>xcorr</c>): the correlation of two signals at each lag, drawn as stems and a
/// reference line at 0, or as markers alone, exactly as <c>Acorr</c> draws the correlation of a signal with itself (which is
/// <c>Xcorr</c> of the signal with a copy of it). The positive lag is x delayed against y. The sums are <c>Correlation</c>,
/// held to matplotlib's vectors in <c>CorrelationTests</c>; this holds what the verb does with them.</summary>
public class XcorrTests
{
    private const double Gap = double.NaN;

    /// <summary>y is x two samples later.</summary>
    private static readonly double[] Early = [0, 0, 1, 3, 2, 0, 0, 0];

    private static readonly double[] Late = [0, 0, 0, 0, 1, 3, 2, 0];

    private static Figure Of(double[] x, double[] y, CorrelationOptions options = default, Action<CorrelationParts>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.Xcorr(x, y, options, configure));

    // ---- the lag sign ------------------------------------------------------------------------------------------------

    [Fact]
    public void ThePositiveLag_IsXDelayedAgainstY_AsMatplotlibPinsIt()
    {
        // matplotlib: xcorr([0, 1], [1, 0], maxlags=1) = [0, 0, 1] at the lags -1, 0, 1: the stem stands at +1, none at -1.
        var line = Of([0, 1], [1, 0], new CorrelationOptions { MaxLags = 1 }).OnlySeries<LineSeries>();

        // Only the segment at +1 is above zero; the segments at -1 and 0 are zero stems, from 0 to 0.
        (new double[] { -1, -1, Gap, 0, 0, Gap, 1, 1 }).AssertCloseTo(line.XData, 1e-12);
        (new double[] { 0, 0, Gap, 0, 0, Gap, 0, 1 }).AssertCloseTo(line.YData, 1e-12);
    }

    [Fact]
    public void ASignalDelayedByTwo_PeaksAtMinusTwo_AndTheOtherWayRoundAtPlusTwo()
    {
        var options = new CorrelationOptions { MaxLags = 3, Markers = true };

        var yAfterX = Of(Early, Late, options).OnlySeries<LineSeries>();
        var xAfterY = Of(Late, Early, options).OnlySeries<LineSeries>();

        Assert.Equal(-2.0, yAfterX.XData[Array.IndexOf(yAfterX.YData, yAfterX.YData.Max())]);
        Assert.Equal(1.0, yAfterX.YData.Max(), 12);
        Assert.Equal(2.0, xAfterY.XData[Array.IndexOf(xAfterY.YData, xAfterY.YData.Max())]);
        Enumerable.Reverse(yAfterX.YData).ToList().AssertCloseTo(xAfterY.YData, 1e-12);   // swapping the signals mirrors the lags
    }

    [Fact]
    public void XcorrOfASignalWithItself_IsAcorrOfIt()
    {
        double[] signal = [3, -1, 4, 1, -5, 9, 2, -6];
        var options = new CorrelationOptions { MaxLags = 3 };

        var cross = Of(signal, [.. signal], options).OnlySeries<LineSeries>();
        var auto = TestFigures.OfOneVerb(ax => ax.Acorr(signal, options)).OnlySeries<LineSeries>();

        Assert.Equal(auto.XData, cross.XData);
        Assert.Equal(auto.YData, cross.YData);
    }

    // ---- stems and markers -------------------------------------------------------------------------------------------

    [Fact]
    public void TheStems_AreOneLineSeries_AndTheReferenceLineAtZeroIsTheOtherPart()
    {
        CorrelationParts parts = default;
        var figure = Of([1, 2, 3], [1, 2, 3], new CorrelationOptions { MaxLags = 1 }, p => parts = p);

        var line = figure.OnlySeries<LineSeries>();

        double top = 8 / 14.0;
        (new double[] { 0, top, Gap, 0, 1, Gap, 0, top }).AssertCloseTo(line.YData, 1e-12);
        Assert.Same(line, parts.Line);
        var zero = Assert.Single(figure.SubPlots[0].ReferenceLines);
        Assert.Same(zero, parts.Zero);
        Assert.Equal(0.0, zero.Value);
    }

    [Fact]
    public void WithMarkers_TheChartIsTheLagsAndTheCorrelations_WithCirclesAndNoLine_AndNoReferenceLine()
    {
        CorrelationParts parts = default;
        var figure = Of([1, 2, 3], [1, 2, 3], new CorrelationOptions { MaxLags = 1, Markers = true }, p => parts = p);

        var line = figure.OnlySeries<LineSeries>();

        (new double[] { -1, 0, 1 }).AssertCloseTo(line.XData, 1e-12);
        (new double[] { 8 / 14.0, 1, 8 / 14.0 }).AssertCloseTo(line.YData, 1e-12);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
        Assert.Equal(LineStyle.None, line.LineStyle);
        Assert.Same(line, parts.Line);
        Assert.Null(parts.Zero);
        Assert.Empty(figure.SubPlots[0].ReferenceLines);
    }

    // ---- the options reach the sums -----------------------------------------------------------------------------------

    [Fact]
    public void Unnormalised_DrawsTheRawSums()
    {
        // [0, 1] against [1, 0]: the raw sum at +1 is 1.
        var line = Of([0, 1], [1, 0], new CorrelationOptions { MaxLags = 1, Unnormalised = true, Markers = true }).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 0.0, 1.0], line.YData);
    }

    [Fact]
    public void TheDetrend_IsAppliedToBothSignals()
    {
        var line = Of([1, 2, 3], [3, 2, 1], new CorrelationOptions { MaxLags = 2, Detrend = SpectralDetrend.Mean, Markers = true }).OnlySeries<LineSeries>();

        // Less their means the signals are [-1, 0, 1] and [1, 0, -1]: the sums at the lags -2 .. 2 are 1, 0, -2, 0, 1, over 2.
        (new double[] { 0.5, 0, -1, 0, 0.5 }).AssertCloseTo(line.YData, 1e-12);
    }

    // ---- a missing value is a gap -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AMissingSample_InEitherSignal_LeavesNothingToDraw(bool inY)
    {
        double[] gappy = [1, Gap, 3, 4];
        double[] whole = [4, 3, 2, 1];

        var stems = inY ? Of(whole, gappy, new CorrelationOptions { MaxLags = 2 }) : Of(gappy, whole, new CorrelationOptions { MaxLags = 2 });
        var markers = inY ? Of(whole, gappy, new CorrelationOptions { MaxLags = 2, Markers = true }) : Of(gappy, whole, new CorrelationOptions { MaxLags = 2, Markers = true });

        Assert.Empty(stems.OnlySeries<LineSeries>().XData);
        Assert.Single(stems.SubPlots[0].ReferenceLines);
        Assert.All(markers.OnlySeries<LineSeries>().YData, value => Assert.True(double.IsNaN(value)));
        stems.ToSvg().AssertNoNonFiniteNumber();
        markers.ToSvg().AssertNoNonFiniteNumber();
    }
}
