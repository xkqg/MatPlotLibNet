// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
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
        TestFigures.OfOneVerb(ax => ax.Acorr(signal, options, configure));

    /// <summary>The strokes of the stems, each as its points in pixels: the polylines in the series colour at the width of a line
    /// that are visible (a line whose style is none is handed to the context all the same, and draws nothing).</summary>
    private static List<Point[]> Stems(RecordingRenderContext drawn) => [.. drawn.Of<DrawLinesCall>()
        .Where(args => args.Color == SeriesColour && args.Thickness == Theme.MatplotlibV2.Looks.Lines.Width && args.Style != LineStyle.None)
        .Select(args => args.Points)];

    // ---- stems: one line and the line at 0 ----------------------------------------------------------------------------

    [Fact]
    public void TheStems_AreOneLineSeries_OfASegmentFromZeroToTheCorrelationAtEachLag()
    {
        var line = Of(Three, OneLag).OnlySeries<LineSeries>();

        double top = 8 / 14.0;
        (new double[] { -1, -1, Gap, 0, 0, Gap, 1, 1 }).AssertCloseTo(line.XData, 1e-12);
        (new double[] { 0, top, Gap, 0, 1, Gap, 0, top }).AssertCloseTo(line.YData, 1e-12);
    }

    [Fact]
    public void TheStems_AreDrawnAsOnePolylineForEachLag_EachUprightOnThePage()
    {
        var stems = Stems(Of(Three, OneLag).Drawn());

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
        var axes = (Of(Three, OneLag)).SubPlots[0];

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

        Assert.Same(figure.OnlySeries<LineSeries>(), parts.Line);
        Assert.Same(Assert.Single(figure.SubPlots[0].ReferenceLines), parts.Zero);
    }

    [Fact]
    public void TheReferenceLineAtZero_IsDrawnAcrossThePlot_NextToTheStems()
    {
        var drawn = Of(Three, OneLag).Drawn();

        // One DrawLine for the reference line (the frame and the grid are drawn by other calls with other widths and colours), as wide as
        // the theme's lines: matplotlib 3.11.2's acorr draws its zero line with axhline, printed get_linewidth() 1.5 (pt) under the default style.
        Assert.Contains(drawn.Of<DrawLineCall>(),
            args => args.Color == Theme.Default.Neutral && args.Thickness == Theme.MatplotlibV2.Looks.Lines.Width && args.Style == LineStyle.Solid);
    }

    // ---- markers: the points alone ------------------------------------------------------------------------------------

    [Fact]
    public void WithMarkers_TheChartIsOneLineSeries_OfTheLagsAndTheCorrelations_AndNoReferenceLine()
    {
        var figure = Of(Three, OneLagWithMarkers);

        var line = figure.OnlySeries<LineSeries>();

        (new double[] { -1, 0, 1 }).AssertCloseTo(line.XData, 1e-12);
        (new double[] { 8 / 14.0, 1, 8 / 14.0 }).AssertCloseTo(line.YData, 1e-12);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
        Assert.Equal(LineStyle.None, line.LineStyle);
        Assert.Empty(figure.SubPlots[0].ReferenceLines);
    }

    [Fact]
    public void WithMarkers_ThePartsHoldTheLineAndNoReferenceLine()
    {
        CorrelationParts parts = default;
        var figure = Of(Three, OneLagWithMarkers, p => parts = p);

        Assert.Same(figure.OnlySeries<LineSeries>(), parts.Line);
        Assert.Null(parts.Zero);
    }

    [Fact]
    public void WithMarkers_NoLineIsDrawn_OnlyTheCircles()
    {
        var drawn = Of(Three, OneLagWithMarkers).Drawn();

        Assert.Empty(Stems(drawn));
        Assert.Equal(3, drawn.Of<DrawCircleCall>().Count);
        Assert.DoesNotContain("<polyline", Of(Three, OneLagWithMarkers).ToSvg());
    }

    [Fact]
    public void WithoutMarkers_ThereAreNoCircles_OnlyTheStems()
    {
        Assert.Equal(0, Of(Three, OneLag).Drawn().Count<DrawCircleCall>());
    }

    // ---- the options reach the sums -----------------------------------------------------------------------------------

    [Fact]
    public void TheDefaultOptions_DrawTenLagsEitherSide_ThatIsTwentyOneStems()
    {
        double[] signal = [.. Enumerable.Range(1, 12).Select(i => (double)(i * i % 7 + 1))];

        var line = Of(signal).OnlySeries<LineSeries>();

        Assert.Equal(21 * 3 - 1, line.XData.Length);
        Assert.Equal(-10.0, line.XData[0]);
        Assert.Equal(10.0, line.XData[^2]);
    }

    [Fact]
    public void Unnormalised_DrawsTheRawSums()
    {
        var line = Of(Three, new CorrelationOptions { MaxLags = 1, Unnormalised = true }).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 8.0, Gap, 0.0, 14.0, Gap, 0.0, 8.0], line.YData);
    }

    [Fact]
    public void ADetrendOfTheMean_ReachesTheSums()
    {
        var line = Of(Three, new CorrelationOptions { MaxLags = 2, Detrend = SpectralDetrend.Mean, Markers = true }).OnlySeries<LineSeries>();

        (new double[] { -0.5, 0, 1, 0, -0.5 }).AssertCloseTo(line.YData, 1e-12);
    }

    [Fact]
    public void TheVerb_SetsNoTitleAndNoLimit_AsMatplotlibsAcorrDoesNot()
    {
        var axes = (Of(Three, OneLag)).SubPlots[0];

        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
        Assert.False(axes.HasExplicitLimit);
    }

    // ---- a missing value is a gap -------------------------------------------------------------------------------------

    [Fact]
    public void AMissingSample_LeavesNothingToDraw_ButTheReferenceLine()
    {
        var figure = Of([1, Gap, 3, 4], new CorrelationOptions { MaxLags = 2 });

        var line = figure.OnlySeries<LineSeries>();

        Assert.Empty(line.XData);
        Assert.Empty(Stems(figure.Drawn()));
        Assert.Single(figure.SubPlots[0].ReferenceLines);
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    [Fact]
    public void AMissingSample_WithMarkers_LeavesTheLagsAndNoMarker()
    {
        var figure = Of([1, double.PositiveInfinity, 3, 4], new CorrelationOptions { MaxLags = 2, Markers = true });

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal([-2.0, -1.0, 0.0, 1.0, 2.0], line.XData);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.Equal(0, figure.Drawn().Count<DrawCircleCall>());
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    [Fact]
    public void ASignalWithNoEnergy_HasNoCorrelation_ItIsGapsAndNeverZeros()
    {
        var stems = Of([0, 0, 0], OneLag);
        var markers = Of([0, 0, 0], OneLagWithMarkers);

        Assert.Empty(stems.OnlySeries<LineSeries>().XData);
        Assert.All(markers.OnlySeries<LineSeries>().YData, value => Assert.True(double.IsNaN(value)));
        stems.ToSvg().AssertNoNonFiniteNumber();
        markers.ToSvg().AssertNoNonFiniteNumber();
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

        var back = figure.RoundTripped();

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(MarkerStyle.Circle, ((LineSeries)Assert.Single(back.SubPlots[0].Series)).Marker);
        Assert.Empty(back.SubPlots[0].ReferenceLines);
    }
}
