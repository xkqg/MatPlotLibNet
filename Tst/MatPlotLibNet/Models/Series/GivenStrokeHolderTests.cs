// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Every kind whose line takes a width and a dash pattern holds what its caller gave as one value, its
/// <see cref="GivenStroke"/>: the public <c>LineWidth</c> and <c>LineStyle</c> write into it and report its parts, or the kind's resting
/// width and the solid line while a part was not given; the JSON writes the given parts only and reads them back as given; and giving
/// what the series already holds is no change of it.</summary>
public class GivenStrokeHolderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    /// <summary>A new series of every kind that holds a given stroke, with the resting width it reports.</summary>
    internal static readonly Dictionary<string, (Func<ChartSeries> Make, double RestingWidth)> Kinds = new()
    {
        ["line"] = (() => new LineSeries(X, Y), LineWidthRule.Line.Resting),
        ["step"] = (() => new StepSeries(X, Y), LineWidthRule.Line.Resting),
        ["signal"] = (() => new SignalSeries(Y), LineWidthRule.Line.Resting),
        ["signalxy"] = (() => new SignalXYSeries(X, Y), LineWidthRule.Line.Resting),
        ["ecdf"] = (() => new EcdfSeries(Y), LineWidthRule.Line.Resting),
        ["kde"] = (() => new KdeSeries(Y), LineWidthRule.Line.Resting),
        ["regression"] = (() => new RegressionSeries(X, Y), LineWidthRule.TwoPixelLine.Resting),
        ["area"] = (() => new AreaSeries(X, Y), LineWidthRule.Line.Resting),
        ["line3d"] = (() => new Line3DSeries(X, Y, Y), LineWidthRule.Line.Resting),
        ["polarline"] = (() => new PolarLineSeries(Y, X), LineWidthRule.Line.Resting),
        ["ternaryline"] = (() => new TernaryLineSeries(X, Y, Y), LineWidthRule.Line.Resting),
        ["streamingline"] = (() => new StreamingLineSeries(8), LineWidthRule.Line.Resting),
    };

    public static TheoryData<string> KindNames => [.. Kinds.Keys];

    private static PropertyInfo Public(ChartSeries series, string name) => series.GetType().GetProperty(name)!;

    private static GivenStroke StrokeOf(object holder) =>
        (GivenStroke)holder.GetType().GetProperty("Stroke", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(holder)!;

    [Theory]
    [MemberData(nameof(KindNames))]
    public void ASeriesGivenNothing_HoldsNoStroke_AndReportsItsRestingWidthAndTheSolidLine(string kind)
    {
        var series = Kinds[kind].Make();

        Assert.Equal(default, StrokeOf(series));
        Assert.Equal(Kinds[kind].RestingWidth, Public(series, "LineWidth").GetValue(series));
        Assert.Equal(GivenStroke.RestingStyle, Public(series, "LineStyle").GetValue(series));
    }

    [Theory]
    [MemberData(nameof(KindNames))]
    public void AWidthAndADashTheCallerGives_AreTheSeriesStroke_TheRestingValuesIncluded(string kind)
    {
        var series = Kinds[kind].Make();

        Public(series, "LineWidth").SetValue(series, Kinds[kind].RestingWidth);
        Assert.Equal(new GivenStroke(Kinds[kind].RestingWidth, null), StrokeOf(series));
        Public(series, "LineStyle").SetValue(series, LineStyle.Solid);

        Assert.Equal(new GivenStroke(Kinds[kind].RestingWidth, LineStyle.Solid), StrokeOf(series));
        Assert.Equal(LineStyle.Solid, Public(series, "LineStyle").GetValue(series));
    }

    [Theory]
    [MemberData(nameof(KindNames))]
    public void TheJson_WritesTheGivenStrokeOnly_AndReadsItBackAsGiven(string kind)
    {
        var unset = Kinds[kind].Make();
        var given = Kinds[kind].Make();
        Public(given, "LineWidth").SetValue(given, 2.5);
        Public(given, "LineStyle").SetValue(given, LineStyle.DashDot);

        var unsetDto = unset.ToSeriesDto();
        var givenDto = given.ToSeriesDto();

        Assert.Null(unsetDto.LineWidth);
        Assert.Null(unsetDto.LineStyle);
        Assert.Equal(2.5, givenDto.LineWidth);
        Assert.Equal("dashdot", givenDto.LineStyle);
        Assert.Equal(default, StrokeOf(SeriesRegistry.Create(unsetDto.Type!, new Axes(), unsetDto)!));
        Assert.Equal(new GivenStroke(2.5, LineStyle.DashDot), StrokeOf(SeriesRegistry.Create(givenDto.Type!, new Axes(), givenDto)!));
    }

    /// <summary>Giving a line the width or the dash it reads at rest changes what it draws under a theme that names its own, so it is
    /// a change of the series; giving the same again is not.</summary>
    [Theory]
    [MemberData(nameof(KindNames))]
    public void GivingTheRestingValues_IsAChange_AndGivingThemAgainIsNot(string kind)
    {
        var series = Kinds[kind].Make();
        long before = series.LatestChange;

        Public(series, "LineWidth").SetValue(series, Kinds[kind].RestingWidth);
        long widthGiven = series.LatestChange;
        Public(series, "LineStyle").SetValue(series, LineStyle.Solid);
        long styleGiven = series.LatestChange;
        Public(series, "LineWidth").SetValue(series, Kinds[kind].RestingWidth);
        Public(series, "LineStyle").SetValue(series, LineStyle.Solid);

        Assert.True(widthGiven > before);
        Assert.True(styleGiven > widthGiven);
        Assert.Equal(styleGiven, series.LatestChange);
    }

    /// <summary>The list above is every kind that holds a given stroke: a kind that comes to hold one is named here, and so is held to
    /// the rule.</summary>
    [Fact]
    public void EveryKindThatHoldsAGivenStroke_IsListedHere()
    {
        var listed = Kinds.Values.Select(k => k.Make().GetType()).ToHashSet();

        var holders = typeof(ChartSeries).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ChartSeries))
                && t.GetProperty("Stroke", BindingFlags.Instance | BindingFlags.NonPublic)?.PropertyType == typeof(GivenStroke))
            .ToHashSet();

        Assert.Equal(listed.OrderBy(t => t.Name), holders.OrderBy(t => t.Name));
    }

    [Fact]
    public void AnIndicator_HoldsWhatItWasGiven_AndHandsItToItsLines()
    {
        double[] close = [.. Enumerable.Range(0, 40).Select(i => 10.0 + i % 7)];
        var unset = new Sma(close, 5);
        var given = new Sma(close, 5) { LineWidth = 1.5 };
        var axes = new Axes();

        given.Apply(axes);

        Assert.Equal(default, StrokeOf(unset));
        Assert.Equal(LineWidthRule.Line.Resting, unset.LineWidth);
        Assert.Equal(GivenStroke.RestingStyle, unset.LineStyle);
        Assert.Equal(new GivenStroke(1.5, null), StrokeOf(given));
        Assert.Equal(new GivenStroke(1.5, null), StrokeOf(Assert.Single(axes.Series)));
    }

    /// <summary>Giving a width or a dash writes one value through the series' change mark and allocates nothing: the value's equality
    /// is its own, so the comparison never boxes it.</summary>
    [Fact]
    public void GivingAWidthOrADash_AllocatesNothing()
    {
        var line = new LineSeries(X, Y);
        void Give(int times)
        {
            for (int i = 0; i < times; i++)
            {
                line.LineWidth = i % 3;
                line.LineStyle = (i & 1) == 0 ? LineStyle.Dashed : LineStyle.Solid;
            }
        }

        Give(1_000);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Give(100_000);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A boxed value would be 32 bytes a give, 6.4 MB here.
        Assert.True(allocated < 1_000, $"{allocated} bytes allocated by 200,000 gives");
    }
}
