// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A quiver that names no scale is drawn as matplotlib's <c>Axes.quiver</c> draws one given no <c>scale</c>. matplotlib 3.11.2,
/// <c>quiver.Quiver._init</c> and <c>_make_verts</c>, read with inspect and run: with <c>units='width'</c> the arrows are measured in
/// the width of the axes, <c>span = 1</c>, and with no scale <c>scale = 1.8 * amean * sn / span</c> where <c>sn = max(10, sqrt(N))</c>
/// and <c>amean</c> is the mean length of the vectors that are not masked (a masked U or V is a NaN or an infinity), so an arrow of
/// length <c>a</c> is <c>a / scale</c> of the width of the axes long whatever the data units are; its direction is
/// <c>np.angle(U + iV)</c> (<c>angles='uv'</c>), the direction of the vector on the screen, and the axes are ranged over the tails
/// (<c>get_datalim</c> reads <c>self.XY</c>). Printed for <c>ax.quiver([1, 2, 3], [1, 1, 1], [1, 2, 0], [0, 0, 3])</c> on an axes 400 pixels
/// wide: <c>scale 36.0</c>, the arrows' tips 11.1111, 22.2222 and 33.3333 pixels from their tails (the third straight up); for the
/// rotational field <c>ax.quiver(X, Y, -Y, X)</c> over <c>linspace(-2, 2, 10)</c>: <c>scale 30.49557993397219</c>, xlim (-2.2, 2.2).
/// The arrow is a polygon (<see cref="QuiverArrowShapeTests"/>); here its foot and its tip stand for it. The library's own themes keep the
/// arrows <see cref="QuiverSeries.Scale"/> data units long for each unit of their vector, as the
/// documentation of that property says, and a scale a caller names is honoured under every theme.</summary>
public sealed class QuiverAutoScaleTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static QuiverSeries Three() => new([1.0, 2.0, 3.0], [1.0, 1.0, 1.0], [1.0, 2.0, 0.0], [0.0, 0.0, 3.0]);

    /// <summary>The rotational field the Fidelity row draws: 100 arrows over <c>linspace(-2, 2, 10)</c> squared, U = -Y, V = X.</summary>
    private static QuiverSeries Rotation()
    {
        double[] axis = [.. Enumerable.Range(0, 10).Select(i => -2 + 4.0 * i / 9)];
        var x = new double[100]; var y = new double[100]; var u = new double[100]; var v = new double[100];
        for (int iy = 0; iy < 10; iy++)
            for (int ix = 0; ix < 10; ix++)
            {
                int k = iy * 10 + ix;
                x[k] = axis[ix]; y[k] = axis[iy]; u[k] = -axis[iy]; v[k] = axis[ix];
            }
        return new QuiverSeries(x, y, u, v);
    }

    private static RecordingRenderContext Render(QuiverSeries series, Theme theme, DataTransform? transform = null)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(transform ?? new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            Theme = theme,
            AxesSeries = [series],
        };
        new QuiverSeriesRenderer(context).Render(series);
        return ctx;
    }

    /// <summary>The place of each arrow drawn, in the order the arrows were drawn: its tail, the middle of the polygon's two corners at
    /// its foot (vertices 0 and 6), and its tip (vertex 3), as matplotlib's arrow polygon has them. An arrow in data units is a stroke
    /// from its tail to its tip.</summary>
    private static List<(Point Tail, Point Tip)> Shafts(RecordingRenderContext ctx)
    {
        var polygons = ctx.Polygons().ToList();
        if (polygons.Count > 0)
            return [.. polygons.Select(p => (Tail: new Point((p[0].X + p[6].X) / 2, (p[0].Y + p[6].Y) / 2), Tip: p[3]))];
        return [.. ctx.Of<DrawLineCall>().Chunk(3).Select(c => (Tail: c[0].P1, Tip: c[0].P2))];
    }

    private static double Length((Point Tail, Point Tip) shaft) => Math.Sqrt(Math.Pow(shaft.Tip.X - shaft.Tail.X, 2) + Math.Pow(shaft.Tip.Y - shaft.Tail.Y, 2));

    // ── the length ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The mean length is 2 and N is 3, so <c>sn</c> is 10 and the scale 36: an arrow of length a is a / 36 of the 400 pixels.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void AnArrow_IsItsLengthOverTheScaleOfTheAxesWidth(string themeId)
    {
        var shafts = Shafts(Render(Three(), themeId.AsTheme()));

        Assert.Equal(3, shafts.Count);
        Assert.Equal(400 * 1.0 / 36, Length(shafts[0]), 6);
        Assert.Equal(400 * 2.0 / 36, Length(shafts[1]), 6);
        Assert.Equal(400 * 3.0 / 36, Length(shafts[2]), 6);
    }

    /// <summary>The axes' range is not part of it: the same arrows over 0 to 10 and over 0 to 1000 are as long.</summary>
    [Fact]
    public void AnArrow_DoesNotDependOnTheRangeOfTheAxes()
    {
        var narrow = Shafts(Render(Three(), Theme.MatplotlibV2, new DataTransform(0, 10, 0, 10, Bounds)));
        var wide = Shafts(Render(Three(), Theme.MatplotlibV2, new DataTransform(0, 10, 0, 1000, Bounds)));

        for (int i = 0; i < 3; i++) Assert.Equal(Length(narrow[i]), Length(wide[i]), 6);
    }

    /// <summary>matplotlib's <c>angles='uv'</c>: the arrow points along the vector as it stands on the screen, whatever the two axes' units are.</summary>
    [Fact]
    public void AnArrow_PointsAlongTheVectorOnTheScreen_NotAlongItInDataUnits()
    {
        var diagonal = new QuiverSeries([5.0], [500.0], [1.0], [1.0]);

        var shaft = Shafts(Render(diagonal, Theme.MatplotlibV2, new DataTransform(0, 10, 0, 1000, Bounds)))[0];

        Assert.Equal(shaft.Tip.X - shaft.Tail.X, shaft.Tail.Y - shaft.Tip.Y, 9);   // 45 degrees up and to the right
        Assert.True(shaft.Tip.X > shaft.Tail.X);
    }

    [Fact]
    public void AVerticalVector_PointsStraightUp()
    {
        var shaft = Shafts(Render(Three(), Theme.MatplotlibV2))[2];

        Assert.Equal(shaft.Tail.X, shaft.Tip.X, 9);
        Assert.Equal(400 * 3.0 / 36, shaft.Tail.Y - shaft.Tip.Y, 6);
    }

    /// <summary>The rotational field of the Fidelity row: matplotlib prints <c>scale 30.49557993397219</c> for it (N = 100, so
    /// <c>sn = 10</c>); the arrow at the point (-2, 2) has the vector (-2, -2), 2.828 long.</summary>
    [Fact]
    public void AField_IsScaledByOneAndAHalfTimesItsMeanLengthAndTheNumberOfArrows()
    {
        var shafts = Shafts(Render(Rotation(), Theme.MatplotlibV2, new DataTransform(-2.2, 2.2, -2.2, 2.2, Bounds)));

        Assert.Equal(100, shafts.Count);
        Assert.Equal(Math.Sqrt(8) / 30.49557993397219 * 400, Length(shafts[90]), 6);
    }

    /// <summary>More than a hundred arrows count in <c>sn = max(10, sqrt(N))</c>: 400 arrows of one unit are 1 / (1.8 * 20) of the width.</summary>
    [Fact]
    public void AMoreCrowdedField_ShortensItsArrows()
    {
        double[] x = [.. Enumerable.Range(0, 400).Select(i => i % 20 * 0.5)];
        double[] y = [.. Enumerable.Range(0, 400).Select(i => i / 20 * 0.5)];
        var field = new QuiverSeries(x, y, [.. x.Select(_ => 1.0)], [.. x.Select(_ => 0.0)]);

        var shafts = Shafts(Render(field, Theme.MatplotlibV2));

        Assert.Equal(400 / (1.8 * 20), Length(shafts[0]), 6);
    }

    /// <summary>matplotlib masks a vector that is not a number: it takes no part in the mean and its arrow is not drawn.</summary>
    [Fact]
    public void AVectorThatIsNotANumber_TakesNoPartInTheMean_AndIsNotDrawn()
    {
        var series = new QuiverSeries([1.0, 2.0, 3.0, 4.0], [1.0, 1.0, 1.0, 1.0], [1.0, 2.0, 0.0, double.NaN], [0.0, 0.0, 3.0, 1.0]);

        var shafts = Shafts(Render(series, Theme.MatplotlibV2));

        Assert.Equal(3, shafts.Count);
        Assert.Equal(400 * 2.0 / 36, Length(shafts[1]), 6);   // the mean of 1, 2 and 3 is 2, as without the fourth
    }

    /// <summary>A field in which no vector is a number has no mean to scale by: nothing is drawn, as matplotlib's <c>amean</c> of nothing is
    /// not a number.</summary>
    [Fact]
    public void AFieldOfVectorsThatAreNotNumbers_HasNoScale_AndDrawsNothing()
    {
        var series = new QuiverSeries([1.0, 2.0], [1.0, 1.0], [double.NaN, double.PositiveInfinity], [1.0, 1.0]);

        Assert.Equal(0, series.AutoScaleShare());
        Assert.Empty(Shafts(Render(series, Theme.MatplotlibV2)));
    }

    // ── who is autoscaled ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The library's own themes keep what the documentation of <c>Scale</c> says: the arrow stands from its tail by the vector
    /// times the scale, in data units along each axis (40 pixels a unit across, 30 up).</summary>
    [Fact]
    public void TheLibrarysTheme_DrawsTheArrowsInDataUnits_AsItAlwaysDid()
    {
        var shafts = Shafts(Render(Three(), Theme.Default));

        Assert.Equal(40, Length(shafts[0]), 6);
        Assert.Equal(80, Length(shafts[1]), 6);
        Assert.Equal(90, Length(shafts[2]), 6);
    }

    /// <summary>A scale the caller names is a scale, under every theme: <see cref="QuiverSeries.Scale"/> documents the factor that turns
    /// the components into data units.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void ANamedScale_IsHonouredUnderTheMatplotlibThemes(string themeId)
    {
        var series = Three();
        series.Scale = 0.5;

        var shafts = Shafts(Render(series, themeId.AsTheme()));

        Assert.Equal(20, Length(shafts[0]), 6);
        Assert.Equal(40, Length(shafts[1]), 6);
    }

    [Fact]
    public void AutoScaleOff_KeepsTheDataUnits_EvenAtTheRestingScale()
    {
        var series = Three();
        series.AutoScale = false;

        Assert.Equal(40, Length(Shafts(Render(series, Theme.MatplotlibV2))[0]), 6);
    }

    [Fact]
    public void AutoScaleOn_ScalesUnderTheLibrarysTheme_AndOverANamedScale()
    {
        var series = Three();
        series.AutoScale = true;
        series.Scale = 0.5;

        Assert.Equal(400 * 1.0 / 36, Length(Shafts(Render(series, Theme.Default))[0]), 6);
    }

    [Fact]
    public void TheRule_IsOnTheSeries_AndTheFlagOnTheTheme()
    {
        var resting = Three();
        var named = Three();
        named.Scale = 2;
        var off = Three();
        off.AutoScale = false;
        var on = Three();
        on.AutoScale = true;
        on.Scale = 2;

        Assert.True(resting.AutoScaledUnder(Theme.MatplotlibV2));
        Assert.True(resting.AutoScaledUnder(Theme.MatplotlibClassic));
        Assert.False(resting.AutoScaledUnder(Theme.Default));
        Assert.False(named.AutoScaledUnder(Theme.MatplotlibV2));
        Assert.False(off.AutoScaledUnder(Theme.MatplotlibV2));
        Assert.True(on.AutoScaledUnder(Theme.Default));
        Assert.True(Theme.MatplotlibV2.Looks.VectorField.QuiverAutoScale && Theme.MatplotlibClassic.Looks.VectorField.QuiverAutoScale && !Theme.Default.Looks.VectorField.QuiverAutoScale);
    }

    /// <summary>matplotlib clips a quiver's arrows to the axes (a collection is clipped by default), and an autoscaled arrow's length is no
    /// part of the range, so the arrows of the rotational field stand out past the top and the right of the axes unless they are cut
    /// there: every arrow is drawn inside one clip of the plot area. Arrows in data units are inside the range and are drawn as before.</summary>
    [Fact]
    public void AnAutoscaledQuiver_IsClippedToTheAxes_OneInDataUnitsIsNot()
    {
        var inDataUnits = Rotation();
        inDataUnits.AutoScale = false;
        Assert.DoesNotContain("PushClip", Render(Rotation(), Theme.MatplotlibV2).Calls.Select(c => c.Kind));

        var autoscaled = Rotation();
        autoscaled.AutoScale = true;
        var scaled = Passed(autoscaled, Theme.Default);
        Assert.Equal("PushClip", scaled[0]);
        Assert.Equal("PopClip", scaled[^1]);
        Assert.Equal(1, scaled.Count(k => k == "PushClip"));
        Assert.DoesNotContain("PushClip", Passed(inDataUnits, Theme.Default));
        // matplotlib cuts every quiver off at the axes (a collection, clip_on True), in data units too.
        Assert.Equal(1, Passed(inDataUnits, Theme.MatplotlibV2).Count(k => k == "PushClip"));
    }

    /// <summary>The calls a series pass of the plot area makes for <paramref name="series"/>: the pass decides the clip (the one clip rule).</summary>
    private static List<string> Passed(QuiverSeries series, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        var axes = new Axes();
        axes.AddSeries(series);
        var host = new TestLayerHost([0]);
        SeriesPass.OverPlot(axes, new RenderPass(Bounds, ctx, theme), new DataTransform(-2.2, 2.2, -2.2, 2.2, Bounds))
            .Draw(ref host);
        return [.. ctx.Calls.Select(c => c.Kind)];
    }

    // ── the range ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The axes of an autoscaled quiver are ranged over its tails, as matplotlib's are: the rotational field's tails are
    /// <c>linspace(-2, 2, 10)</c>, so the default style prints xlim and ylim (-2.2, 2.2); the heads, which stand at 4 and further, are no
    /// part of it as an arrow's length is a share of the axes and not data units.</summary>
    [Fact]
    public void TheAxes_AreRangedOverTheTails()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.AddSeries(Rotation())).Build();

        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.Equal(-2.2, range.XMin, 9);
        Assert.Equal(2.2, range.XMax, 9);
        Assert.Equal(-2.2, range.YMin, 9);
        Assert.Equal(2.2, range.YMax, 9);
    }

    /// <summary>A tail that cannot be placed adds nothing to the range of the tails, as a point that cannot be placed adds nothing to a
    /// line's.</summary>
    [Fact]
    public void ATailThatCannotBePlaced_AddsNothingToTheRange()
    {
        var series = new QuiverSeries([1.0, double.NaN, 3.0], [1.0, 2.0, 5.0], [1.0, 1.0, 1.0], [0.0, 0.0, 0.0]);
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();

        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.Equal(1 - 0.05 * 2, range.XMin, 9);   // 1 to 3 and the 5 % margin
        Assert.Equal(3 + 0.05 * 2, range.XMax, 9);
    }

    /// <summary>The same arrows in data units reach as far as their heads, whatever the series says of its own range.</summary>
    [Fact]
    public void TheAxes_AreRangedOverTheHeadsToo_WhenTheArrowsAreInDataUnits()
    {
        var series = Rotation();
        series.AutoScale = false;
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();

        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.True(range.XMax > 3.9, $"the range ends at {range.XMax}");
    }

    /// <summary>What the series reports of itself is what it always reported: tails and heads. The tails alone is the axes' rule for an
    /// arrow that is not in data units.</summary>
    [Fact]
    public void TheSeriesOwnRange_StillHoldsTailsAndHeads()
    {
        var range = Rotation().ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.True(range.XMax > 3.9, $"the series reports up to {range.XMax}");
    }

    // ── the key ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The key is drawn by the quiver's own arrow (<c>self.Q._make_verts([[0., 0.]], u, v, 'uv')</c>), so a key of 1 for the
    /// rotational field is 1 / 30.49557993397219 of the width of the axes long: the printed tip of the key's arrow with the label to the
    /// east stands 20.331 pixels left of its point on a 620-pixel axes.</summary>
    [Fact]
    public void AKey_IsAsLongAsAnArrowOfItsLengthIsInTheQuiver()
    {
        var quiver = Rotation();
        var key = new QuiverKeySeries(0.9, 0.9, 1.0, "1 unit");
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(-2.2, 2.2, -2.2, 2.2, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            Theme = Theme.MatplotlibV2,
            AxesSeries = [quiver, key],
        };

        new QuiverKeySeriesRenderer(context).Render(key);

        var shaft = Shafts(ctx)[0];
        Assert.Equal(400 / 30.49557993397219, Length(shaft), 6);
    }

    // ── the JSON, and the chart that relies on a scale of one ─────────────────────────────────────────────────

    [Fact]
    public void TheChoice_IsLeftOutOfTheJsonAtRest_AndTravelsWhenMade()
    {
        Assert.Null(Three().ToSeriesDto().QuiverAutoScale);

        foreach (bool choice in new[] { true, false })
        {
            var series = Three();
            series.AutoScale = choice;
            var back = (QuiverSeries)Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build().RoundTripped()
                .SubPlots[0].Series[0];
            Assert.Equal(choice, back.AutoScale);
        }
    }

    /// <summary>A connected scatter's arrow is the step to the next point at a scale of one, so its head lands on that point: it asks
    /// for data units under every theme.</summary>
    [Fact]
    public void AConnectedScatter_AsksForDataUnits_SoItsHeadsLandOnThePoints()
    {
        QuiverSeries? arrows = null;
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.ConnectedScatter([1.0, 2.0, 4.0], [1.0, 3.0, 2.0], parts => arrows = parts.Arrows)).Build();

        Assert.False(arrows!.AutoScaledUnder(figure.Theme));
        Assert.Equal(false, arrows.AutoScale);
    }

    // ── a figure ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The default figure is drawn under matplotlib's default style, 800 x 600 with its axes 620 pixels wide: the arrows of the
    /// three are 620 / 36 pixels per unit of their length, black, as matplotlib draws them.</summary>
    [Fact]
    public void AFigure_DrawsItsArrowsAutoscaled_UnderTheDefaultStyle()
    {
        var svg = Plt.Create().WithSize(800, 600).AddSubPlot(1, 1, 1, ax => ax
            .AddSeries(Three()).SetXLim(0, 10).SetYLim(0, 10)).Build().ToSvg().Xml();

        var arrows = svg.Each("polygon").Where(e => (string?)e.Attribute("fill") == "#000000").ToList();
        Point[] Vertices(XElement e) => [.. ((string)e.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(',')).Select(xy => new Point(double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture)))];

        Assert.Equal(3, arrows.Count);
        var first = Vertices(arrows[0]);
        var third = Vertices(arrows[2]);
        // the tip, from the middle of the foot; the SVG writes two decimals of a pixel
        Assert.InRange(first[3].X - (first[0].X + first[6].X) / 2, 620 * 1.0 / 36 - 0.02, 620 * 1.0 / 36 + 0.02);
        Assert.InRange((third[0].Y + third[6].Y) / 2 - third[3].Y, 620 * 3.0 / 36 - 0.02, 620 * 3.0 / 36 + 0.02);
    }
}
