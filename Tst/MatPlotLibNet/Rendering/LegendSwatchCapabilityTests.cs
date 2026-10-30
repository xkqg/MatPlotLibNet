// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series says how it is drawn in its legend entry (<c>ChartSeries.LegendSwatch</c>) and the legend draws what it
/// says: the legend holds no list of series types. A series that says nothing is a filled rectangle; the kinds that are
/// lines, markers, error bars or timelines of states say so, with the values they hold.</summary>
public class LegendSwatchCapabilityTests
{
    /// <summary>The swatch of each kind that is not a patch; every other shipped series is a patch.</summary>
    private static readonly Dictionary<Type, LegendSwatchKind> NotPatches = new()
    {
        [typeof(LineSeries)] = LegendSwatchKind.LineMarker,
        [typeof(StepSeries)] = LegendSwatchKind.LineMarker,
        [typeof(SignalSeries)] = LegendSwatchKind.Line,
        [typeof(SignalXYSeries)] = LegendSwatchKind.Line,
        [typeof(EcdfSeries)] = LegendSwatchKind.Line,
        [typeof(RegressionSeries)] = LegendSwatchKind.Line,
        [typeof(SparklineSeries)] = LegendSwatchKind.SolidLine,
        [typeof(ScatterSeries)] = LegendSwatchKind.Marker,
        [typeof(TernaryScatterSeries)] = LegendSwatchKind.Marker,
        [typeof(TernaryLineSeries)] = LegendSwatchKind.Line,
        [typeof(ErrorBarSeries)] = LegendSwatchKind.ErrorBar,
        [typeof(StateTimelineSeries)] = LegendSwatchKind.Stripes,
    };

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void EveryShippedSeries_SaysItsSwatch_APatchUnlessItIsInTheTable(ISeries series, string name)
    {
        var expected = NotPatches.GetValueOrDefault(series.GetType(), LegendSwatchKind.Patch);

        Assert.True(((ChartSeries)series).LegendSwatch.Kind == expected, $"{name} says {((ChartSeries)series).LegendSwatch.Kind}, expected {expected}.");
    }

    [Fact]
    public void ALine_SaysItsOwnWidthStyleMarkerAndMarkerSize()
    {
        var line = new LineSeries([1.0], [2.0]) { LineWidth = 3, LineStyle = LineStyle.Dashed, Marker = MarkerStyle.Square, MarkerSize = 9 };

        Assert.Equal(new LegendSwatch(LegendSwatchKind.LineMarker, 3, LineStyle.Dashed, 1.5, MarkerStyle.Square, 9), line.LegendSwatch);
    }

    [Fact]
    public void ALineThatNamesNoMarker_SaysNone_SoTheSlotMayName_One() =>
        Assert.Null(new LineSeries([1.0], [2.0]).LegendSwatch.Marker);

    [Fact]
    public void AStep_SaysTheRestingMarkerSizeOfALine()
    {
        var step = new StepSeries([1.0], [2.0]) { Marker = MarkerStyle.Diamond };

        Assert.Equal(LineSeries.RestingMarkerSize, step.LegendSwatch.MarkerSize);
        Assert.Equal(MarkerStyle.Diamond, step.LegendSwatch.Marker);
    }

    [Fact]
    public void ARegression_SaysItsOwnRestingWidth_WhichIsNotALinesDefault()
    {
        var swatch = new RegressionSeries([1.0, 2.0], [1.0, 2.0]) { LineWidth = 4 }.LegendSwatch;

        Assert.Equal(4, swatch.LineWidth);
        Assert.Equal(RegressionSeries.RestingLineWidth, swatch.RestingLineWidth);
    }

    [Fact]
    public void AScatter_SaysItsMarkerAndItsSize()
    {
        var swatch = new ScatterSeries([1.0], [2.0]) { Marker = MarkerStyle.Triangle, MarkerSize = 80 }.LegendSwatch;

        Assert.Equal(MarkerStyle.Triangle, swatch.Marker);
        Assert.Equal(80, swatch.MarkerSize);
    }

    [Fact]
    public void AnErrorBar_SaysItsLineWidth() =>
        Assert.Equal(2.5, new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]) { LineWidth = 2.5 }.LegendSwatch.LineWidth);

    [Fact]
    public void ASparkline_SaysItsLineWidth() =>
        Assert.Equal(2, new SparklineSeries([1.0, 2.0]) { LineWidth = 2 }.LegendSwatch.LineWidth);

    [Fact]
    public void ASeriesThatSaysNothing_IsAPatch() =>
        Assert.Equal(LegendSwatch.Patch, new BarSeries(["a"], [1.0]).LegendSwatch);
}
