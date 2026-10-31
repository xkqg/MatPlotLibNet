// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

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
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Xcorr(x, y, options, configure)).Build();

    private static Axes AxesOf(Figure figure) => figure.SubPlots[0];

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(AxesOf(figure).Series));

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual, double tolerance = 1e-12)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            if (double.IsNaN(expected[i]))
            {
                Assert.True(double.IsNaN(actual[i]), $"[{i}]: expected NaN, got {actual[i]:R}");
            }
            else
            {
                Assert.True(Math.Abs(expected[i] - actual[i]) <= tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
            }
        }
    }

    // ---- the shape the contracts hold ---------------------------------------------------------------------------------

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.XcorrByHand(false).ToSvg(), ComposedChartShapes.Named("xcorr").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.XcorrByHand(true).ToSvg(), ComposedChartShapes.Named("xcorr").Build(true).ToSvg());
    }

    [Fact]
    public void TheMarkerVerb_DrawsExactlyWhatItsHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.XcorrMarkersByHand(false).ToSvg(), ComposedChartShapes.Named("xcorr-markers").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.XcorrMarkersByHand(true).ToSvg(), ComposedChartShapes.Named("xcorr-markers").Build(true).ToSvg());
    }

    // ---- the lag sign ------------------------------------------------------------------------------------------------

    [Fact]
    public void ThePositiveLag_IsXDelayedAgainstY_AsMatplotlibPinsIt()
    {
        // matplotlib: xcorr([0, 1], [1, 0], maxlags=1) = [0, 0, 1] at the lags -1, 0, 1: the stem stands at +1, none at -1.
        var line = OnlySeriesOf(Of([0, 1], [1, 0], new CorrelationOptions { MaxLags = 1 }));

        // Only the segment at +1 is above zero; the segments at -1 and 0 are zero stems, from 0 to 0.
        AssertClose([-1, -1, Gap, 0, 0, Gap, 1, 1, Gap], line.XData);
        AssertClose([0, 0, Gap, 0, 0, Gap, 0, 1, Gap], line.YData);
    }

    [Fact]
    public void ASignalDelayedByTwo_PeaksAtMinusTwo_AndTheOtherWayRoundAtPlusTwo()
    {
        var options = new CorrelationOptions { MaxLags = 3, Markers = true };

        var yAfterX = OnlySeriesOf(Of(Early, Late, options));
        var xAfterY = OnlySeriesOf(Of(Late, Early, options));

        Assert.Equal(-2.0, yAfterX.XData[Array.IndexOf(yAfterX.YData, yAfterX.YData.Max())]);
        Assert.Equal(1.0, yAfterX.YData.Max(), 12);
        Assert.Equal(2.0, xAfterY.XData[Array.IndexOf(xAfterY.YData, xAfterY.YData.Max())]);
        AssertClose([.. Enumerable.Reverse(yAfterX.YData)], xAfterY.YData);   // swapping the signals mirrors the lags
    }

    [Fact]
    public void XcorrOfASignalWithItself_IsAcorrOfIt()
    {
        double[] signal = [3, -1, 4, 1, -5, 9, 2, -6];
        var options = new CorrelationOptions { MaxLags = 3 };

        var cross = OnlySeriesOf(Of(signal, [.. signal], options));
        var auto = OnlySeriesOf(Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Acorr(signal, options)).Build());

        Assert.Equal(auto.XData, cross.XData);
        Assert.Equal(auto.YData, cross.YData);
    }

    // ---- stems and markers -------------------------------------------------------------------------------------------

    [Fact]
    public void TheStems_AreOneLineSeries_AndTheReferenceLineAtZeroIsTheOtherPart()
    {
        CorrelationParts parts = default;
        var figure = Of([1, 2, 3], [1, 2, 3], new CorrelationOptions { MaxLags = 1 }, p => parts = p);

        var line = OnlySeriesOf(figure);

        double top = 8 / 14.0;
        AssertClose([0, top, Gap, 0, 1, Gap, 0, top, Gap], line.YData);
        Assert.Same(line, parts.Line);
        var zero = Assert.Single(AxesOf(figure).ReferenceLines);
        Assert.Same(zero, parts.Zero);
        Assert.Equal(0.0, zero.Value);
    }

    [Fact]
    public void WithMarkers_TheChartIsTheLagsAndTheCorrelations_WithCirclesAndNoLine_AndNoReferenceLine()
    {
        CorrelationParts parts = default;
        var figure = Of([1, 2, 3], [1, 2, 3], new CorrelationOptions { MaxLags = 1, Markers = true }, p => parts = p);

        var line = OnlySeriesOf(figure);

        AssertClose([-1, 0, 1], line.XData);
        AssertClose([8 / 14.0, 1, 8 / 14.0], line.YData);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
        Assert.Equal(LineStyle.None, line.LineStyle);
        Assert.Same(line, parts.Line);
        Assert.Null(parts.Zero);
        Assert.Empty(AxesOf(figure).ReferenceLines);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_IsOneLegendEntry_NamedByTheLine(bool markers)
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Xcorr(Early, Late, new CorrelationOptions { MaxLags = 3, Markers = markers }, parts => parts.Line.Label = "cross-correlation")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("cross-correlation", svg);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid_AndItRunsOnce()
    {
        var seen = new List<CorrelationParts>();

        var figure = Of(Early, Late, new CorrelationOptions { MaxLags = 3 }, parts =>
        {
            seen.Add(parts);
            parts.Line.Color = Colors.Red;
            parts.Zero!.LineStyle = LineStyle.Dotted;
        });

        Assert.Single(seen);
        Assert.Equal(Colors.Red, OnlySeriesOf(figure).Color);
        Assert.Equal(LineStyle.Dotted, Assert.Single(AxesOf(figure).ReferenceLines).LineStyle);
    }

    // ---- the options reach the sums -----------------------------------------------------------------------------------

    [Fact]
    public void Unnormalised_DrawsTheRawSums()
    {
        // [0, 1] against [1, 0]: the raw sum at +1 is 1.
        var line = OnlySeriesOf(Of([0, 1], [1, 0], new CorrelationOptions { MaxLags = 1, Unnormalised = true, Markers = true }));

        Assert.Equal([0.0, 0.0, 1.0], line.YData);
    }

    [Fact]
    public void TheDetrend_IsAppliedToBothSignals()
    {
        var line = OnlySeriesOf(Of([1, 2, 3], [3, 2, 1], new CorrelationOptions { MaxLags = 2, Detrend = SpectralDetrend.Mean, Markers = true }));

        // Less their means the signals are [-1, 0, 1] and [1, 0, -1]: the sums at the lags -2 .. 2 are 1, 0, -2, 0, 1, over 2.
        AssertClose([0.5, 0, -1, 0, 0.5], line.YData);
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

        Assert.Empty(OnlySeriesOf(stems).XData);
        Assert.Single(AxesOf(stems).ReferenceLines);
        Assert.All(OnlySeriesOf(markers).YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", stems.ToSvg());
        Assert.DoesNotContain("NaN", markers.ToSvg());
    }

    // ---- the JSON ----------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture(bool markers)
    {
        var figure = Of(Early, Late, new CorrelationOptions { MaxLags = 3, Markers = markers }, parts => parts.Line.Label = "cross-correlation");

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(markers ? 0 : 1, back.SubPlots[0].ReferenceLines.Count);
    }

    // ---- what it refuses ---------------------------------------------------------------------------------------------

    [Fact]
    public void WhatCannotBeCorrelated_IsRefusedByName_AndLeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => ax.Xcorr(null!, Late)).ParamName);
            Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => ax.Xcorr(Early, null!)).ParamName);
            Assert.Equal("x", Assert.Throws<ArgumentException>(() => ax.Xcorr([], [])).ParamName);
            Assert.Equal("y", Assert.Throws<ArgumentException>(() => ax.Xcorr(Early, [1, 2, 3])).ParamName);
            Assert.Equal("options", Assert.Throws<ArgumentException>(() => ax.Xcorr(Early, Late)).ParamName);   // ten lags need eleven samples
            Assert.Equal("options", Assert.Throws<ArgumentException>(() => ax.Xcorr(Early, Late, new CorrelationOptions { MaxLags = 8 })).ParamName);
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Empty(axes.ReferenceLines);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Xcorr(Early, Late, new CorrelationOptions { MaxLags = 3 })).ParamName);
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Same(ax, ax.Xcorr(Early, Late, new CorrelationOptions { MaxLags = 3 }));
            Assert.Same(ax, ax.Xcorr(Early, Late, new CorrelationOptions { MaxLags = 3, Markers = true }));
        });

        Assert.Equal(2, builder.Build().SubPlots[0].Series.Count);
    }
}
