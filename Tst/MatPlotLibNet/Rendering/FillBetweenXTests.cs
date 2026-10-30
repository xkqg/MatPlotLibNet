// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>matplotlib's <c>fill_betweenx</c>: at each position along Y the fill runs between two curves on X. It is an
/// <see cref="AreaSeries"/> that lies down, the data held in axis terms (X is X, Y is Y), and its drawing is the upright
/// fill's on its side: the same rule for gaps, step, smoothing and a <c>Where</c> predicate, through one mapper that tells
/// the two axes apart.</summary>
public class FillBetweenXTests
{
    private static readonly double[] Y = [0, 1, 2, 3, 4];
    private static readonly double[] X1 = [1, 3, 2, 5, 4];
    private static readonly double[] X2 = [0, 1, 0, 1, 0];

    /// <summary>X 0..10 and Y 0..4 exactly, so a data value maps to a known pixel.</summary>
    private static Figure Of(Action<AxesBuilder> fill) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => { fill(ax); ax.SetXLim(0, 10).SetYLim(0, 4); }).Build();

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

    private static double PxX(Rect plot, double x) => plot.X + x / 10 * plot.Width;

    private static double PxY(Rect plot, double y) => plot.Y + plot.Height - y / 4 * plot.Height;

    private static List<Point[]> Polygons(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var polygons = new List<Point[]>();
        foreach (var call in recording.OfKind("DrawPolygon"))
        {
            dynamic args = call.Args!;
            polygons.Add((Point[])args.points);
        }

        return polygons;
    }

    [Fact]
    public void TheDataIsHeldInAxisTerms_ThePositionsAreY()
    {
        var series = new Axes().FillBetweenX(Y, X1, X2);

        Assert.Equal(Y, series.YData);
        Assert.Equal(X1, series.XData);
        Assert.Equal(X2, series.XData2);
        Assert.Equal(BarOrientation.Horizontal, series.Orientation);
    }

    [Fact]
    public void ThePolygon_RunsDownTheFirstCurveAndBackUpTheSecond()
    {
        var figure = Of(ax => ax.FillBetweenX(Y, X1, X2));
        var plot = Plot(figure);

        var polygon = Polygons(figure).Single();

        Point[] expected =
        [
            .. Enumerable.Range(0, 5).Select(i => new Point(PxX(plot, X1[i]), PxY(plot, Y[i]))),
            .. Enumerable.Range(0, 5).Reverse().Select(i => new Point(PxX(plot, X2[i]), PxY(plot, Y[i]))),
        ];
        Assert.Equal(expected.Length, polygon.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].X, polygon[i].X, 9);
            Assert.Equal(expected[i].Y, polygon[i].Y, 9);
        }
    }

    [Fact]
    public void WithoutASecondCurve_TheFillStandsOnTheYAxis_AtXEqualsZero()
    {
        var figure = Of(ax => ax.FillBetweenX(Y, X1));
        var plot = Plot(figure);

        var polygon = Polygons(figure).Single();

        Assert.All(polygon.Skip(5), point => Assert.Equal(PxX(plot, 0), point.X, 9));
    }

    [Fact]
    public void AGapInTheFirstCurve_SplitsTheFill_AsItDoesForAnUprightFill()
    {
        double[] gappy = [1, 3, double.NaN, 5, 4];

        var polygons = Polygons(Of(ax => ax.FillBetweenX(Y, gappy, X2)));

        Assert.Equal(2, polygons.Count);
        Assert.Equal([4, 4], polygons.Select(p => p.Length));
    }

    [Fact]
    public void AWherePredicate_IsAskedWithXFirst_AndFillsWhereItHolds()
    {
        var figure = Of(ax => ax.FillBetweenX(Y, X1, X2, s => s.Where = (x, y) => x > 2));

        var polygons = Polygons(figure);

        // x1 = 1, 3, 2, 5, 4: the predicate holds at positions 1, 3 and 4, which are two runs.
        Assert.Equal(2, polygons.Count);
    }

    [Fact]
    public void TheRange_IsTheCurvesOnX_AndThePositionsOnY()
    {
        var series = new Axes().FillBetweenX(Y, X1);

        var range = series.ComputeDataRange(new RangeContext());

        Assert.Equal(0, range.XMin);
        Assert.Equal(5, range.XMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(4, range.YMax);
        Assert.Equal(0, range.StickyXMin);
    }

    [Fact]
    public void TheTable_IsThePositionsThenTheCurves()
    {
        var table = new Axes().FillBetweenX(Y, X1, X2).ToDataTable()!;

        Assert.Equal(["y", "x1", "x2"], table.Columns.Select(c => c.Header));
        Assert.Equal([0.0, 1.0, 0.0], table.Rows[0].Select(c => c.Number));
        Assert.Equal([3.0, 5.0, 1.0], table.Rows[3].Select(c => c.Number));
    }

    [Fact]
    public void ANearestPointSearch_FindsTheFirstCurveAtItsPosition()
    {
        var figure = Of(ax => ax.FillBetweenX(Y, X1, X2));
        var layout = ChartLayout.Create(figure, new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()));

        var hit = NearestPointFinder.Find(figure, 0, 5.1, 3.1, layout, maxPixelDistance: 60);

        Assert.NotNull(hit);
        Assert.Equal(5.0, hit.DataX);
        Assert.Equal(3.0, hit.DataY);
    }

    [Fact]
    public void ALyingDownFill_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(ax => ax.FillBetweenX(Y, X1, X2));

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var area = Assert.IsType<AreaSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal(BarOrientation.Horizontal, area.Orientation);
        Assert.Equal(X2, area.XData2);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void AnUprightFill_LeavesTheNewFieldsOutOfItsJson()
    {
        string json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2], [3.0, 4])).Build().ToJson();

        Assert.DoesNotContain("orientation", json, StringComparison.Ordinal);
        Assert.DoesNotContain("xData2", json, StringComparison.Ordinal);
    }

    [Fact]
    public void PositionsAndCurvesOfAnotherLength_AreRefusedByName()
    {
        Assert.Throws<ArgumentException>(() => new Axes().FillBetweenX([0.0, 1], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() => new Axes().FillBetweenX([0.0, 1], [1.0, 2], [1.0]));
    }

    [Fact]
    public void TheTable_OfOneCurve_IsThePositionsThenThatCurve_UnderTheLabelOrX()
    {
        var plain = new Axes().FillBetweenX(Y, X1);
        var labelled = new Axes().FillBetweenX(Y, X1);
        labelled.Label = "speed";

        Assert.Equal(["y", "x"], plain.ToDataTable()!.Columns.Select(c => c.Header));
        Assert.Equal(["y", "speed"], labelled.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void TheTable_OfTwoCurves_NamesTheFirstByTheLabel()
    {
        var labelled = new Axes().FillBetweenX(Y, X1, X2);
        labelled.Label = "speed";

        Assert.Equal(["y", "speed", "x2"], labelled.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ASecondCurveOfNoPoints_IsNoSecondCurve_InTheTable()
    {
        var series = new Axes().FillBetweenX(Y, X1);
        series.XData2 = [];

        Assert.Equal(["y", "x"], series.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void TheTableTheAxesNames_OfAFillThatLiesDown_IsThePlainTable()
    {
        var axes = new Axes();
        var series = axes.FillBetweenX(Y, X1, X2);

        var named = ((ITableNamedByAxes)series).ToDataTable(AxisNames.Of(axes, series))!;

        Assert.Equal(series.ToDataTable()!.Columns.Select(c => c.Header), named.Columns.Select(c => c.Header));
        Assert.Equal(series.ToDataTable()!.Rows.Count, named.Rows.Count);
    }

    [Theory]
    [InlineData("horizontal")]
    [InlineData("Horizontal")]
    [InlineData(null)]
    public void ARecordWithoutData_ReadsBackAsAnEmptyFill_OfTheOrientationItNames(string? orientation)
    {
        var read = AreaSeries.FromSeriesDto(new Axes(), new SeriesDto { Type = "area", Orientation = orientation });

        Assert.Empty(read.XData);
        Assert.Empty(read.YData);
        Assert.Equal(orientation is null ? BarOrientation.Vertical : BarOrientation.Horizontal, read.Orientation);
    }

    private sealed class RangeContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
