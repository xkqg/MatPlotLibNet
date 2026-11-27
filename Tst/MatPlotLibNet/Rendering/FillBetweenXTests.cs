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

    private static double PxX(Rect plot, double x) => plot.X + x / 10 * plot.Width;

    private static double PxY(Rect plot, double y) => plot.Y + plot.Height - y / 4 * plot.Height;

    private static List<Point[]> Polygons(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var polygons = new List<Point[]>();
        foreach (var call in recording.Of<DrawPolygonCall>())
        {
            polygons.Add(call.Points);
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
        var plot = figure.FirstPlotArea();

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
        var plot = figure.FirstPlotArea();

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

        var range = series.ComputeDataRange(new TestAxesContext());

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

        var back = figure.RoundTripped();

        var area = Assert.IsType<AreaSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal(BarOrientation.Horizontal, area.Orientation);
        Assert.Equal(X2, area.XData2);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnUprightFill_LeavesTheNewFieldsOutOfItsJson", () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2], [3.0, 4])).Build().ToJson()) { Absent = ["orientation", "xData2"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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

    /// <summary>A record read back is refused as its verb refuses it: positions and a first curve of other lengths, or a second curve the
    /// fill draws of another length than the positions, with the counts in the order the verb names them. A second curve the fill does
    /// not draw (the Y one of a fill that lies down) is carried, not checked.</summary>
    [Theory]
    [InlineData("horizontal", new[] { 1.0, 2 }, new[] { 1.0, 2, 3 }, null, null, "Got 3 and 2.")]
    [InlineData("horizontal", new[] { 1.0, 2 }, new[] { 1.0, 2 }, new[] { 1.0 }, null, "Got 2 and 1.")]
    [InlineData(null, new[] { 1.0, 2 }, new[] { 1.0, 2, 3 }, null, null, "Got 2 and 3.")]
    [InlineData(null, new[] { 1.0, 2 }, new[] { 1.0, 2 }, null, new[] { 1.0 }, "Got 2 and 1.")]
    public void ARecordOfCurvesOfAnotherLength_IsRefusedAsItsVerbRefusesIt(
        string? orientation, double[] x, double[] y, double[]? x2, double[]? y2, string message)
    {
        var axes = new Axes();

        var refused = Assert.Throws<ArgumentException>(() => AreaSeries.FromSeriesDto(axes,
            new SeriesDto { Type = "area", Orientation = orientation, XData = x, YData = y, XData2 = x2, YData2 = y2 }));

        Assert.EndsWith(message, refused.Message, StringComparison.Ordinal);
        Assert.Empty(axes.Series);
    }

    [Theory]
    [InlineData("horizontal", null, new[] { 9.0 })]
    [InlineData(null, new[] { 9.0 }, null)]
    public void ASecondCurveTheFillDoesNotDraw_IsCarried_NotChecked(string? orientation, double[]? x2, double[]? y2)
    {
        var read = AreaSeries.FromSeriesDto(new Axes(),
            new SeriesDto { Type = "area", Orientation = orientation, XData = [1.0, 2], YData = [3.0, 4], XData2 = x2, YData2 = y2 });

        Assert.Equal(x2, read.XData2);
        Assert.Equal(y2, read.YData2);
    }

    [Fact]
    public void TheCurvesOfAFill_RefuseAFirstOrASecondCurveOfAnotherLengthThanThePositions()
    {
        new AreaCurves([1.0, 2], [3.0, 4], [5.0, 6]).ThrowIfMisaligned();
        new AreaCurves([1.0, 2], [3.0, 4], null).ThrowIfMisaligned();

        Assert.EndsWith("Got 2 and 3.", Assert.Throws<ArgumentException>(() => new AreaCurves([1.0, 2], [3.0, 4, 5], null).ThrowIfMisaligned()).Message, StringComparison.Ordinal);
        Assert.EndsWith("Got 2 and 1.", Assert.Throws<ArgumentException>(() => new AreaCurves([1.0, 2], [3.0, 4], [5.0]).ThrowIfMisaligned()).Message, StringComparison.Ordinal);
    }
}
