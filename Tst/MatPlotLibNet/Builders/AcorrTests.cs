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

/// <summary>The <c>Acorr</c> verb (matplotlib's <c>acorr</c>): the correlation of a signal with itself at each lag, drawn as
/// matplotlib draws it, either as stems (ONE line series built from a segment from 0 to the correlation at each lag, and a
/// reference line at 0) or, when the options ask for markers, as the points of the correlation alone. Never both. The sums are
/// <c>Correlation</c>, held to matplotlib's vectors in <c>CorrelationTests</c>; this holds what the verb does with them.</summary>
public class AcorrTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private const double Gap = double.NaN;

    /// <summary>[1, 2, 3] at one lag either side: matplotlib's [8/14, 1, 8/14].</summary>
    private static readonly double[] Three = [1, 2, 3];

    private static readonly CorrelationOptions OneLag = new() { MaxLags = 1 };

    private static readonly CorrelationOptions OneLagWithMarkers = new() { MaxLags = 1, Markers = true };

    private static Figure Of(double[] signal, CorrelationOptions options = default, Action<CorrelationParts>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Acorr(signal, options, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The strokes of the stems, each as its points in pixels: the polylines in the series colour at the width of a line
    /// that are visible (a line whose style is none is handed to the context all the same, and draws nothing).</summary>
    private static List<Point[]> Stems(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLines")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == SeriesColour && (double)args.thickness == 1.5 && (LineStyle)args.style != LineStyle.None)
        .Select(args => (Point[])args.points)];

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
        Assert.Equal(ComposedChartShapes.AcorrByHand(false).ToSvg(), ComposedChartShapes.Named("acorr").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.AcorrByHand(true).ToSvg(), ComposedChartShapes.Named("acorr").Build(true).ToSvg());
    }

    [Fact]
    public void TheMarkerVerb_DrawsExactlyWhatItsHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.AcorrMarkersByHand(false).ToSvg(), ComposedChartShapes.Named("acorr-markers").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.AcorrMarkersByHand(true).ToSvg(), ComposedChartShapes.Named("acorr-markers").Build(true).ToSvg());
    }

    // ---- stems: one line and the line at 0 ----------------------------------------------------------------------------

    [Fact]
    public void TheStems_AreOneLineSeries_OfASegmentFromZeroToTheCorrelationAtEachLag()
    {
        var line = OnlySeriesOf(Of(Three, OneLag));

        double top = 8 / 14.0;
        AssertClose([-1, -1, Gap, 0, 0, Gap, 1, 1, Gap], line.XData);
        AssertClose([0, top, Gap, 0, 1, Gap, 0, top, Gap], line.YData);
    }

    [Fact]
    public void TheStems_AreDrawnAsOnePolylineForEachLag_EachUprightOnThePage()
    {
        var stems = Stems(Drawn(Of(Three, OneLag)));

        Assert.Equal(3, stems.Count);
        Assert.All(stems, stem =>
        {
            Assert.Equal(2, stem.Length);
            Assert.Equal(stem[0].X, stem[1].X, 9);   // a stem is vertical
            Assert.True(stem[0].Y > stem[1].Y);       // and rises from the base, since every correlation here is above 0
        });
        Assert.True(stems[1][1].Y < stems[0][1].Y, "the stem at lag 0 is the tallest");
    }

    [Fact]
    public void TheChart_HoldsTheStemsAndAReferenceLineAtZero_AndNothingElse()
    {
        var axes = AxesOf(Of(Three, OneLag));

        Assert.Single(axes.Series);
        var zero = Assert.Single(axes.ReferenceLines);
        Assert.Equal(0.0, zero.Value);
        Assert.Equal(Orientation.Horizontal, zero.Orientation);
        Assert.Equal(LineStyle.Solid, zero.LineStyle);
        Assert.Null(zero.Label);
        Assert.Null(zero.Color);   // the theme's neutral grey, as every reference line of the library
    }

    [Fact]
    public void ThePartsHold_TheVeryLineAndTheVeryReferenceLine_OnTheAxes()
    {
        CorrelationParts parts = default;
        var figure = Of(Three, OneLag, configure: p => parts = p);

        Assert.Same(OnlySeriesOf(figure), parts.Line);
        Assert.Same(Assert.Single(AxesOf(figure).ReferenceLines), parts.Zero);
    }

    [Fact]
    public void TheReferenceLineAtZero_IsDrawnAcrossThePlot_NextToTheStems()
    {
        var drawn = Drawn(Of(Three, OneLag));

        // One DrawLine for the reference line (the frame and the grid are drawn by other calls with other widths and colours).
        Assert.Contains(drawn.OfKind("DrawLine").Select(call => (dynamic)call.Args!),
            args => (Color)args.color == Theme.Default.Neutral && (double)args.thickness == 1.0 && (LineStyle)args.style == LineStyle.Solid);
    }

    // ---- markers: the points alone ------------------------------------------------------------------------------------

    [Fact]
    public void WithMarkers_TheChartIsOneLineSeries_OfTheLagsAndTheCorrelations_AndNoReferenceLine()
    {
        var figure = Of(Three, OneLagWithMarkers);

        var line = OnlySeriesOf(figure);

        AssertClose([-1, 0, 1], line.XData);
        AssertClose([8 / 14.0, 1, 8 / 14.0], line.YData);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
        Assert.Equal(LineStyle.None, line.LineStyle);
        Assert.Empty(AxesOf(figure).ReferenceLines);
    }

    [Fact]
    public void WithMarkers_ThePartsHoldTheLineAndNoReferenceLine()
    {
        CorrelationParts parts = default;
        var figure = Of(Three, OneLagWithMarkers, p => parts = p);

        Assert.Same(OnlySeriesOf(figure), parts.Line);
        Assert.Null(parts.Zero);
    }

    [Fact]
    public void WithMarkers_NoLineIsDrawn_OnlyTheCircles()
    {
        var drawn = Drawn(Of(Three, OneLagWithMarkers));

        Assert.Empty(Stems(drawn));
        Assert.Equal(3, drawn.OfKind("DrawCircle").Count);
        Assert.DoesNotContain("<polyline", Of(Three, OneLagWithMarkers).ToSvg());
    }

    [Fact]
    public void WithoutMarkers_ThereAreNoCircles_OnlyTheStems()
    {
        Assert.Equal(0, Drawn(Of(Three, OneLag)).CountOf("DrawCircle"));
    }

    // ---- one colour, one legend entry ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_IsOneColourSlot_TheNextSeriesTakesTheNextColour(bool markers)
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Acorr(Three, new CorrelationOptions { MaxLags = 1, Markers = markers })
            .Plot([0.0, 1.0], [0.0, 1.0])).Build();

        var drawn = Drawn(figure);

        Assert.Equal(2, AxesOf(figure).Series.Count);
        Assert.Contains(drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color), colour => colour == Theme.Default.CycleColors[1]);
        Assert.DoesNotContain(drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color), colour => colour == Theme.Default.CycleColors[2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_IsOneLegendEntry_NamedByTheLine(bool markers)
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Acorr(Three, new CorrelationOptions { MaxLags = 1, Markers = markers }, parts => parts.Line.Label = "autocorrelation")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("autocorrelation", svg);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid_AndItRunsOnce()
    {
        var seen = new List<CorrelationParts>();

        var figure = Of(Three, OneLag, parts =>
        {
            seen.Add(parts);
            parts.Line.Color = Colors.Red;
            parts.Line.LineWidth = 4;
            parts.Zero!.Color = Colors.Blue;
            parts.Zero.LineStyle = LineStyle.Dashed;
        });

        Assert.Single(seen);
        Assert.Equal(Colors.Red, OnlySeriesOf(figure).Color);
        var drawn = Drawn(figure);
        Assert.Contains(drawn.OfKind("DrawLines"), call => (Color)((dynamic)call.Args!).color == Colors.Red);
        Assert.Contains(drawn.OfKind("DrawLine"), call => (Color)((dynamic)call.Args!).color == Colors.Blue);
    }

    // ---- the options reach the sums -----------------------------------------------------------------------------------

    [Fact]
    public void TheDefaultOptions_DrawTenLagsEitherSide_ThatIsTwentyOneStems()
    {
        double[] signal = [.. Enumerable.Range(1, 12).Select(i => (double)(i * i % 7 + 1))];

        var line = OnlySeriesOf(Of(signal));

        Assert.Equal(21 * 3, line.XData.Length);
        Assert.Equal(-10.0, line.XData[0]);
        Assert.Equal(10.0, line.XData[^3]);
    }

    [Fact]
    public void Unnormalised_DrawsTheRawSums()
    {
        var line = OnlySeriesOf(Of(Three, new CorrelationOptions { MaxLags = 1, Unnormalised = true }));

        Assert.Equal([0.0, 8.0, Gap, 0.0, 14.0, Gap, 0.0, 8.0, Gap], line.YData);
    }

    [Fact]
    public void ADetrendOfTheMean_ReachesTheSums()
    {
        var line = OnlySeriesOf(Of(Three, new CorrelationOptions { MaxLags = 2, Detrend = SpectralDetrend.Mean, Markers = true }));

        AssertClose([-0.5, 0, 1, 0, -0.5], line.YData);
    }

    [Fact]
    public void TheVerb_SetsNoTitleAndNoLimit_AsMatplotlibsAcorrDoesNot()
    {
        var axes = AxesOf(Of(Three, OneLag));

        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
        Assert.False(axes.HasExplicitLimit);
    }

    // ---- a missing value is a gap -------------------------------------------------------------------------------------

    [Fact]
    public void AMissingSample_LeavesNothingToDraw_ButTheReferenceLine()
    {
        var figure = Of([1, Gap, 3, 4], new CorrelationOptions { MaxLags = 2 });

        var line = OnlySeriesOf(figure);

        Assert.Empty(line.XData);
        Assert.Empty(Stems(Drawn(figure)));
        Assert.Single(AxesOf(figure).ReferenceLines);
        Assert.DoesNotContain("NaN", figure.ToSvg());
    }

    [Fact]
    public void AMissingSample_WithMarkers_LeavesTheLagsAndNoMarker()
    {
        var figure = Of([1, double.PositiveInfinity, 3, 4], new CorrelationOptions { MaxLags = 2, Markers = true });

        var line = OnlySeriesOf(figure);

        Assert.Equal([-2.0, -1.0, 0.0, 1.0, 2.0], line.XData);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.Equal(0, Drawn(figure).CountOf("DrawCircle"));
        Assert.DoesNotContain("NaN", figure.ToSvg());
    }

    [Fact]
    public void ASignalWithNoEnergy_HasNoCorrelation_ItIsGapsAndNeverZeros()
    {
        var stems = Of([0, 0, 0], OneLag);
        var markers = Of([0, 0, 0], OneLagWithMarkers);

        Assert.Empty(OnlySeriesOf(stems).XData);
        Assert.All(OnlySeriesOf(markers).YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", stems.ToSvg());
        Assert.DoesNotContain("NaN", markers.ToSvg());
    }

    // ---- the JSON ----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheStems_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(Three, OneLag, parts => parts.Line.Label = "autocorrelation");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Single(back.SubPlots[0].ReferenceLines);
        Assert.Contains("\"NaN\"", json);
    }

    [Fact]
    public void TheMarkers_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of([1, Gap, 3, 4, 5], new CorrelationOptions { MaxLags = 2, Markers = true }, parts => parts.Line.Label = "autocorrelation");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(MarkerStyle.Circle, ((LineSeries)Assert.Single(back.SubPlots[0].Series)).Marker);
        Assert.Empty(back.SubPlots[0].ReferenceLines);
    }

    // ---- what it refuses ---------------------------------------------------------------------------------------------

    [Fact]
    public void WhatCannotBeCorrelated_IsRefusedByName_AndLeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => ax.Acorr(null!)).ParamName);
            Assert.Equal("signal", Assert.Throws<ArgumentException>(() => ax.Acorr([])).ParamName);
            Assert.Equal("options", Assert.Throws<ArgumentException>(() => ax.Acorr(Three)).ParamName);   // ten lags need eleven samples
            Assert.Equal("options", Assert.Throws<ArgumentException>(() => ax.Acorr(Three, new CorrelationOptions { MaxLags = 3 })).ParamName);
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Empty(axes.ReferenceLines);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Acorr(Three, OneLag)).ParamName);
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Same(ax, ax.Acorr(Three, OneLag));
            Assert.Same(ax, ax.Acorr(Three, OneLagWithMarkers));
        });

        Assert.Equal(2, builder.Build().SubPlots[0].Series.Count);
    }
}
