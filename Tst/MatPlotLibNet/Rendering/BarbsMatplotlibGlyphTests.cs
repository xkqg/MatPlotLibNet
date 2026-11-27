// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes a wind barb is matplotlib's <c>Barbs</c> glyph. matplotlib 3.11.2's <c>quiver.Barbs</c> builds each
/// barb as one polygon: a staff <c>length</c> = 7 long, then from its far end a flag (a triangle) for each 50, a full barb for each 10 and a
/// half barb for a remaining 5, spaced 0.125 of the staff, a barb 0.4 of the staff high and a flag 0.25 of it wide; the magnitude is first
/// rounded to the nearest 5 (<c>np.around</c>, half to even), and one that rounds to 0 is a circle 0.15 of the staff across the middle. The
/// polygon is drawn at <c>sizes = length ** 2 / 4</c>, so one unit of it is <c>sqrt(length ** 2 / 4)</c> = 3.5 pt (printed:
/// <c>get_transforms()[0][0, 0]</c> = 4.8611 px at 100 dpi), and filled and edged black at a line width of 1 pt. The vertices below are
/// matplotlib's own, printed from <c>ax.barbs([0], [0], [u], [v]).get_paths()[0].vertices</c>, in those units with Y up; the staff points
/// to where the wind comes from, which the series' direction names (degrees clockwise from north).</summary>
public class BarbsMatplotlibGlyphTests
{
    private const double Unit = 3.5 * 100 / 72;
    private static readonly Point Base = new(200, 150);
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(BarbsSeries series, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(0, 10, 0, 10, Bounds);
        new BarbsSeriesRenderer(new SeriesRenderContext(transform, ctx, theme.CycleColors[0], new RenderArea(Bounds, ctx)) { Theme = theme }).Render(series);
        return ctx;
    }

    /// <summary>One barb at the middle of the axes (data (5, 5), pixel (200, 150)).</summary>
    private static BarbsSeries One(double speed, double direction) =>
        new(new double[] { 5.0 }, new double[] { 5.0 }, new double[] { speed }, new double[] { direction });

    /// <summary>matplotlib's vertices (Y up, in its units) as the pixels of the barb at <see cref="Base"/>.</summary>
    private static Point[] Pixels(params double[] xy) =>
        [.. Enumerable.Range(0, xy.Length / 2).Select(i => new Point(Base.X + xy[2 * i] * Unit, Base.Y - xy[(2 * i) + 1] * Unit))];

    private static void AssertPolygon(Point[] expected, CallRecord call)
    {
        var drawn = Assert.IsType<DrawPolygonCall>(call).Points;
        Assert.Equal(expected.Length, drawn.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].X, drawn[i].X, 6);
            Assert.Equal(expected[i].Y, drawn[i].Y, 6);
        }
    }

    /// <summary><c>(u, v) = (0, 65)</c>: a flag, a full barb and a half barb on a staff pointing south (the wind comes from the south: a
    /// direction of 180), the features on the west.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void SixtyFive_IsAFlagAFullBarbAndAHalfBarb_AsMatplotlibsPolygon(string theme)
    {
        var call = Assert.Single(Render(One(65, 180), theme.AsTheme()).Of<DrawPolygonCall>());

        AssertPolygon(Pixels(0, 0, 0, -7, -2.8, -6.125, 0, -5.25, 0, -4.375, -2.8, -5.25, 0, -4.375, 0, -3.5, -1.4, -3.9375, 0, -3.5), call);
        Assert.Equal(Colors.Black, call.Fill);
        Assert.Equal(Colors.Black, call.Stroke);
        Assert.Equal(1.0.PointsInPixels, call.StrokeThickness, 9);
    }

    /// <summary><c>(0, 5)</c>: a half barb alone stands 1.5 spacings in from the end of the staff, so it is not read as a full one.</summary>
    [Fact]
    public void Five_IsAHalfBarbSetInFromTheEnd()
    {
        var call = Assert.Single(Render(One(5, 180), Theme.MatplotlibV2).Of<DrawPolygonCall>());

        AssertPolygon(Pixels(0, 0, 0, -7, 0, -5.6875, -1.4, -6.125, 0, -5.6875), call);
    }

    /// <summary><c>(0, 110)</c>: two flags, the second half a spacing nearer the first, then a full barb.</summary>
    [Fact]
    public void AHundredAndTen_IsTwoFlagsAndAFullBarb()
    {
        var call = Assert.Single(Render(One(110, 180), Theme.MatplotlibV2).Of<DrawPolygonCall>());

        AssertPolygon(Pixels(0, 0, 0, -7, -2.8, -6.125, 0, -5.25, 0, -4.8125, -2.8, -3.9375, 0, -3.0625, 0, -2.1875, -2.8, -3.0625, 0, -2.1875), call);
    }

    /// <summary><c>(15, 0)</c>: the wind comes from the west (a direction of 270), the staff points west, the features stand north of it.</summary>
    [Fact]
    public void Fifteen_FromTheWest_PointsWest_WithItsBarbsOnTheNorth()
    {
        var call = Assert.Single(Render(One(15, 270), Theme.MatplotlibV2).Of<DrawPolygonCall>());

        AssertPolygon(Pixels(0, 0, -7, 0, -7.875, 2.8, -7, 0, -6.125, 0, -6.5625, 1.4, -6.125, 0), call);
    }

    /// <summary>The magnitude is rounded to the nearest 5 with numpy's <c>around</c>, half to even: 7.5 and 12.5 both draw one full barb.</summary>
    [Theory]
    [InlineData(7.5)]
    [InlineData(12.5)]
    public void AMagnitudeHalfwayBetweenTwoFives_RoundsToTheEvenOne(double speed)
    {
        var call = Assert.Single(Render(One(speed, 180), Theme.MatplotlibV2).Of<DrawPolygonCall>());

        AssertPolygon(Pixels(0, 0, 0, -7, -2.8, -7.875, 0, -7), call);
    }

    /// <summary><c>(0, 2)</c>: a magnitude that rounds to 0 draws a circle 0.15 of the staff across the middle (printed: 1.05 units) and
    /// nothing else: an outline, not filled.</summary>
    [Fact]
    public void ACalm_IsAnEmptyCircle()
    {
        var ctx = Render(One(2, 180), Theme.MatplotlibV2);

        var circle = Assert.Single(ctx.Of<DrawCircleCall>());
        Assert.Equal(0, ctx.Count<DrawPolygonCall>());
        Assert.Equal(Base, circle.Center);
        Assert.Equal(1.05 * Unit, circle.Radius, 9);
        Assert.Null(circle.Fill);
        Assert.Equal(Colors.Black, circle.Stroke);
    }

    /// <summary>A barb whose speed or direction is missing is left out, as matplotlib deletes a masked point.</summary>
    [Theory]
    [InlineData(double.NaN, 180)]
    [InlineData(65, double.NaN)]
    [InlineData(double.PositiveInfinity, 180)]
    public void ABarbWithAMissingSpeedOrDirection_IsLeftOut(double speed, double direction)
    {
        Assert.DoesNotContain(Render(One(speed, direction), Theme.MatplotlibV2).Calls, c => c.Kind.StartsWith("Draw", StringComparison.Ordinal));
    }

    /// <summary>A staff length the author names is drawn at that length, in pixels, with the features in matplotlib's proportions.</summary>
    [Fact]
    public void AGivenBarbLength_IsTheStaffInPixels()
    {
        var series = One(65, 180);
        series.BarbLength = 28;

        var drawn = (Assert.Single(Render(series, Theme.MatplotlibV2).Of<DrawPolygonCall>())).Points;

        Assert.Equal(Base.Y + 28, drawn[1].Y, 9);
        Assert.Equal(Base.X - (0.4 * 28), drawn[2].X, 9);
    }

    /// <summary>The length of a barb is no part of the axes' range, so a barb at the edge stands out past the plot, which matplotlib's
    /// collection clips to the axes (<c>clip_on</c>): every barb is drawn inside one clip of the plot area.</summary>
    [Fact]
    public void UnderAMatplotlibTheme_TheBarbsAreCutOffAtTheAxes()
    {
        Assert.DoesNotContain(Render(One(65, 180), Theme.MatplotlibV2).Calls, c => c is PushClipCall);

        var ctx = new RecordingRenderContext();
        var series = One(65, 180);
        var axes = new Axes();
        axes.AddSeries(series);
        var host = new TestLayerHost([0]);
        SeriesPass.OverPlot(axes, new RenderPass(Bounds, ctx, Theme.MatplotlibV2), new DataTransform(0, 10, 0, 10, Bounds))
            .Draw(ref host);
        var calls = ctx.Calls;

        Assert.IsType<PushClipCall>(calls[0]);
        Assert.Equal(Bounds, ((PushClipCall)calls[0]).ClipRect);
        Assert.IsType<PopClipCall>(calls[^1]);
    }

    /// <summary>The library's own themes draw the barb they always drew: a staff 15 pixels long in lines.</summary>
    [Fact]
    public void TheLibrarysThemes_DrawTheirOwnBarb()
    {
        var ctx = Render(One(65, 180), Theme.Default);

        Assert.Equal(1, ctx.Count<DrawPolygonCall>());   // the flag
        var staff = ctx.Of<DrawLineCall>()[0];
        Assert.Equal(Base.Y + 15, staff.P2.Y, 9);
    }

    [Fact]
    public void ABarbsSeries_ReadsTheRestingLength_AndWritesAGivenLengthOnly()
    {
        var resting = One(10, 0);
        var given = One(10, 0);
        given.BarbLength = 15;

        Assert.Equal(15, resting.BarbLength);
        Assert.Null(resting.ToSeriesDto().BarbLength);
        Assert.Equal(15, given.ToSeriesDto().BarbLength);
    }

    /// <summary>The theme flag of each vector-field drawing that follows matplotlib's own: the barbs and the arrows of a stream plot.</summary>
    public static TheoryData<Func<Theme, bool>> VectorFieldFlags => new()
    {
        theme => theme.Looks.VectorField.BarbsLikeMatplotlib,
        theme => theme.Looks.VectorField.StreamArrowsLikeMatplotlib,
    };

    [Theory]
    [MemberData(nameof(VectorFieldFlags))]
    public void TheThemeFlag_IsOnUnderTheMatplotlibThemes_AndSurvivesAClone_AndTheJson(Func<Theme, bool> flag)
    {
        Assert.True(flag(Theme.MatplotlibV2) && flag(Theme.MatplotlibClassic) && !flag(Theme.Default));
        Assert.True(flag(Theme.MatplotlibV2.Cloned()));
        Assert.True(flag(Theme.MatplotlibClassic.ThroughJson()));
        Assert.False(flag(Theme.Dark.ThroughJson()));
    }
}
