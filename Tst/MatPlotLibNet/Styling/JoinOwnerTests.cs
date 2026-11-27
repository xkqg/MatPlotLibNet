// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The corner of a stroke has ONE owner for every kind of line and one value for every kind of collection. matplotlib 3.11.2
/// (python): a <c>Line2D</c> and a <c>LineCollection</c> are joined 'round' (<c>lines.solid_joinstyle</c>, <c>lines.dash_joinstyle</c>, and the
/// graphics context default that a collection with no join of its own draws with); a <c>PathCollection</c> (a scatter), a
/// <c>FillBetweenPolyCollection</c> (fill_between, stackplot, the bodies of a violin), a <c>PolyCollection</c> (hexbin, quiver, barbs, broken_barh) and a
/// <c>Poly3DCollection</c> (surface, trisurf, bar3d, voxels) all print <c>get_joinstyle()</c> as None, which is that same 'round'; a
/// <c>Rectangle</c>, <c>Polygon</c> and <c>Wedge</c> (every patch) print 'miter'. In pixels (python, Agg, dpi 100): a square scatter marker
/// 35 px across with an edge 16.7 px wide has its outer corner on the diagonal at 26.4 px from the centre: the rounded one, where the
/// mitre would have reached 24.0.
/// <para>The line is told to the drawing surface, which gives a stroke that names no join the join of the theme it was told
/// (<see cref="StrokeLook.JoinOf"/>), so every polyline of every kind, the 60 sites that build a stroke of their own among them, is joined as
/// the theme says; a producer of a shape that stands for a collection says so once (<see cref="StrokeLook.OfCollection"/>).</para></summary>
public class JoinOwnerTests
{
    private static readonly Point[] Corner = [new(0, 0), new(5, 5), new(10, 0)];

    private static string Svg(Theme theme, Action<SvgRenderContext> draw)
    {
        var ctx = new SvgRenderContext();
        ctx.UseLooksOf(theme);
        draw(ctx);
        return ctx.GetOutput();
    }

    private static string FigureSvg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg();

    // ── the stroke names no join: the surface gives it the theme's ─────────────────────────────────

    [Fact]
    public void AStrokeThatNamesNoJoin_TakesTheJoinOfTheLook_AndAGivenJoinWins()
    {
        var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);

        Assert.Null(plain.Join);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.Looks.Strokes.JoinOf(plain));
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibClassic.Looks.Strokes.JoinOf(plain));
        Assert.Equal(JoinStyle.Miter, Theme.Default.Looks.Strokes.JoinOf(plain));
        Assert.Equal(JoinStyle.Miter, Theme.MatplotlibV2.Looks.Strokes.JoinOf(plain with { Join = JoinStyle.Miter }));
        Assert.Equal(JoinStyle.Bevel, Theme.Default.Looks.Strokes.JoinOf(plain with { Join = JoinStyle.Bevel }));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ASvgSurface_JoinsAPolylineByTheLookItWasTold (#1)", () =>
        {
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.MatplotlibV2, c => c.DrawLines(Corner, plain));
        }, "stroke-linejoin=\"round\""),
        new DrawnToken("ASvgSurface_JoinsAPolylineByTheLookItWasTold (#2)", () =>
        {
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.MatplotlibClassic, c => c.DrawLines(Corner, plain));
        }, "stroke-linejoin=\"round\""),
        new DrawnToken("ASvgSurface_JoinsAPolylineByTheLookItWasTold (#3)", () =>
        {
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.Default, c => c.DrawLines(Corner, plain));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_JoinsAPolylineByTheLookItWasTold (#4)", () =>
        {
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return new SvgRenderContext().Also(c => c.DrawLines(Corner, plain)).GetOutput();
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_KeepsAJoinTheStrokeGave_AndWritesNoneForALineOfOneSegment (#1)", () =>
        {
            var mitred = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Miter };
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.MatplotlibV2, c => c.DrawLines(Corner, mitred));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_KeepsAJoinTheStrokeGave_AndWritesNoneForALineOfOneSegment (#2)", () =>
        {
            var mitred = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Miter };
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.MatplotlibV2, c => c.DrawLine(new Point(0, 0), new Point(9, 9), plain));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_KeepsAJoinTheStrokeGave_AndWritesNoneForALineOfOneSegment (#3)", () =>
        {
            var mitred = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Miter };
            var plain = new StrokeStyle(Colors.Black, 2, LineStyle.Solid);
            return Svg(Theme.MatplotlibV2, c => c.DrawLines([new Point(0, 0), new Point(9, 9)], plain));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_WritesNoJoinForACircleOrAnEllipse_ForNoCornerShowsOnOne (#1)", () =>
        {
            var rounded = new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeJoin = JoinStyle.Round };
            return Svg(Theme.MatplotlibV2, c => c.DrawCircle(new Point(5, 5), 4, rounded));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_WritesNoJoinForACircleOrAnEllipse_ForNoCornerShowsOnOne (#2)", () =>
        {
            var rounded = new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeJoin = JoinStyle.Round };
            return Svg(Theme.MatplotlibV2, c => c.DrawEllipse(new Rect(0, 0, 8, 4), rounded));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ASvgSurface_WritesNoJoinForACircleOrAnEllipse_ForNoCornerShowsOnOne (#3)", () =>
        {
            var rounded = new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeJoin = JoinStyle.Round };
            return Svg(Theme.MatplotlibV2, c => c.DrawPolygon(Corner, rounded));
        }, "stroke-linejoin=\"round\""),
        new DrawnToken("ASvgSurface_WritesNoJoinForACircleOrAnEllipse_ForNoCornerShowsOnOne (#4)", () =>
        {
            var rounded = new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeJoin = JoinStyle.Round };
            return Svg(Theme.MatplotlibV2, c => c.DrawRectangle(new Rect(0, 0, 8, 4), rounded));
        }, "stroke-linejoin=\"round\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A shape is joined as its artist is to matplotlib: one that stands for a collection takes the join of the theme's look,
    /// a patch keeps its mitre, under every theme. The artist says which; the theme says what a collection's join is.</summary>
    [Fact]
    public void AShapeIsJoinedAsItsArtist_ACollectionTakesTheJoinOfTheLook_AndAPatchKeepsItsMitre()
    {
        var shape = ShapeStyle.Edged(Colors.Red, Colors.Black, 1);

        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.JoinedAs(MatplotlibArtist.Mesh, shape).StrokeJoin);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibClassic.JoinedAs(MatplotlibArtist.Scatter, shape).StrokeJoin);
        Assert.Equal(JoinStyle.Miter, Theme.Default.JoinedAs(MatplotlibArtist.Mesh, shape).StrokeJoin);
        Assert.Equal(shape with { StrokeJoin = JoinStyle.Round }, Theme.MatplotlibV2.JoinedAs(MatplotlibArtist.Mesh, shape));
        Assert.Equal(shape, Theme.MatplotlibV2.JoinedAs(MatplotlibArtist.Patch, shape));
        Assert.Equal(JoinStyle.Miter, shape.StrokeJoin);
    }

    /// <summary>The join itself, for a renderer that makes every face of a series in one loop (a 3-D scene's faces): what
    /// <see cref="Theme.JoinedAs"/> gives a fresh shape of that artist, asked once, and laid on each face as it is made.</summary>
    [Fact]
    public void TheShapeJoinOfAnArtist_IsTheLooksForACollection_AndTheMitreForAPatch()
    {
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.ShapeJoinOf(MatplotlibArtist.PatchFaces3D));
        Assert.Equal(JoinStyle.Miter, Theme.Default.ShapeJoinOf(MatplotlibArtist.Mesh));
        Assert.Equal(JoinStyle.Miter, Theme.MatplotlibV2.ShapeJoinOf(MatplotlibArtist.Patch));
        var face = ShapeStyle.Edged(Colors.Red, Colors.Black, 0.5, Theme.MatplotlibV2.ShapeJoinOf(MatplotlibArtist.Mesh));
        Assert.Equal(Theme.MatplotlibV2.JoinedAs(MatplotlibArtist.Mesh, ShapeStyle.Edged(Colors.Red, Colors.Black, 0.5)), face);
    }

    /// <summary>What a 3-D series is to matplotlib (printed with 3.11.2): <c>bar3d</c>, <c>voxels</c>, <c>plot_surface</c> and
    /// <c>plot_trisurf</c> make a <c>Poly3DCollection</c>, <c>Axes3D.bar</c> a <c>Patch3D</c> (a patch, joined 'miter'); a scatter, a hexbin
    /// and barbs are collections too. Their artists say so, which is what joins their shapes.</summary>
    [Theory]
    [InlineData(nameof(Bar3DSeries), true)]
    [InlineData(nameof(VoxelSeries), true)]
    [InlineData(nameof(SurfaceSeries), true)]
    [InlineData(nameof(Trisurf3DSeries), true)]
    [InlineData(nameof(ScatterSeries), true)]
    [InlineData(nameof(HexbinSeries), true)]
    [InlineData(nameof(BarbsSeries), true)]
    [InlineData(nameof(PlanarBar3DSeries), false)]
    public void TheArtistOfAKind_SaysWhetherItIsACollection(string kind, bool collection)
    {
        var series = (ChartSeries)DrawnSeriesFixtures.Instance(kind);

        Assert.Equal(collection, series.Artist.Collection);
    }

    [Fact]
    public void TheStrokeOfASeriesSlot_NamesNoJoin_TheSurfaceGivesItItsLooks()
    {
        Assert.Null(Theme.MatplotlibV2.SeriesSlot(0).StrokeOf(Colors.Black, default(GivenStroke), LineWidthRule.Line).Join);
        Assert.Null(Theme.MatplotlibClassic.SeriesSlot(0).StrokeOf(Colors.Black, default(GivenStroke), LineWidthRule.Line).Join);
        Assert.Null(Theme.Default.SeriesSlot(0).StrokeOf(Colors.Black, default(GivenStroke), LineWidthRule.Line).Join);
    }

    // ── every kind of line of a figure is joined by the figure's theme ─────────────────────────────

    /// <summary>The polylines of a drawing that have a corner (three points or more), each with whether its element names a join.</summary>
    private static List<(int Points, string Join)> PolylinesWithACorner(string svg) =>
        [.. svg.Elements("polyline").Where(polyline => polyline["points"] is not null)
            .Select(polyline => (polyline["points"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, polyline["stroke-linejoin"] ?? ""))
            .Where(p => p.Item1 >= 3)];

    private static readonly double[] Axis = [0.0, 1, 2, 3, 4];
    private static readonly double[,] Hill = new double[5, 5];
    private static readonly double[,] Along = new double[5, 5];
    private static readonly double[,] Around = new double[5, 5];

    static JoinOwnerTests()
    {
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                Hill[i, j] = Math.Round(4 - (i - 2) * (i - 2) * 0.5 - (j - 2) * (j - 2) * 0.5, 3);
                Along[i, j] = 1.0;
                Around[i, j] = j - 2.0;
            }
    }

    public static TheoryData<string> LineKinds => new()
    {
        "plot", "contour", "streamplot", "contourf", "sparkline", "wireframe",
    };

    private static void LineKind(string kind, AxesBuilder ax)
    {
        switch (kind)
        {
            case "plot": ax.Plot([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 0.0, 1.0]); break;
            case "contour": ax.Contour(Axis, Axis, Hill); break;
            case "streamplot": ax.Streamplot(Axis, Axis, Along, Around); break;
            case "contourf": ax.Contourf(Axis, Axis, Hill, s => { s.ShowLines = true; s.LineWidth = 1; }); break;
            case "sparkline": ax.Sparkline([1.0, 3.0, 2.0, 5.0, 4.0]); break;
            case "wireframe": ax.Wireframe(Axis, Axis, Hill); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    [Theory]
    [MemberData(nameof(LineKinds))]
    public void EveryPolylineOfAFigure_IsRoundedUnderTheMatplotlibThemes_AndMitredUnderALibraryTheme(string kind)
    {
        var v2 = PolylinesWithACorner(FigureSvg(Theme.MatplotlibV2, ax => LineKind(kind, ax)));
        var classic = PolylinesWithACorner(FigureSvg(Theme.MatplotlibClassic, ax => LineKind(kind, ax)));
        var library = PolylinesWithACorner(FigureSvg(Theme.Default, ax => LineKind(kind, ax)));

        Assert.NotEmpty(v2);
        Assert.All(v2, p => Assert.Equal("round", p.Join));
        Assert.NotEmpty(classic);
        Assert.All(classic, p => Assert.Equal("round", p.Join));
        Assert.NotEmpty(library);
        Assert.All(library, p => Assert.Equal("", p.Join));
    }

    // ── every kind of collection of a figure takes the join of the theme on its edge ────────────────

    private static readonly double[] Spread = [1.0, 2, 3, 4, 5, 6, 7, 8];
    private static readonly double[] Wave = [2.0, 5, 3, 6, 4, 7, 5, 8];

    /// <summary>The elements of <paramref name="tag"/> that are stroked, each with the join it names.</summary>
    private static List<string> StrokedElements(string svg, string tag) =>
        [.. svg.Elements(tag).Where(element => element["stroke"]?.StartsWith('#') == true).Select(element => element["stroke-linejoin"] ?? "")];

    /// <summary>Each kind, the element its edge is stroked on, and whether the matplotlib default style strokes it (the kinds that edge as a
    /// patch is edged under the classic style only draw no edge under the default one: stackplot, the bodies of a violin, a broken bar, and a
    /// 3-D bar and a voxel, whose <c>Poly3DCollection</c> has no edge under the default style and a black one under classic (printed with
    /// matplotlib 3.11.2: <c>ax.bar3d(...).get_edgecolor()</c> is empty under 'default', <c>[[0, 0, 0, 1]]</c> under 'classic', and the
    /// same for a face of <c>ax.voxels(...)</c>).</summary>
    public static TheoryData<string, string, bool> CollectionKinds => new()
    {
        { "scatter-square", "rect", true },
        { "scatter-triangle", "polygon", true },
        { "fill_between", "polygon", true },
        { "stackplot", "polygon", false },
        { "violin", "polygon", false },
        { "hexbin", "polygon", true },
        { "broken_barh", "rect", false },
        { "surface", "polygon", true },
        { "trisurf", "polygon", true },
        { "bar3d", "polygon", false },
        { "voxels", "polygon", false },
    };

    private static void CollectionKind(string kind, AxesBuilder ax)
    {
        switch (kind)
        {
            case "scatter-square": ax.Scatter(Spread, Wave, s => { s.Marker = MarkerStyle.Square; s.EdgeColors = [Colors.Black]; s.LineWidths = [2.0]; }); break;
            case "scatter-triangle": ax.Scatter(Spread, Wave, s => { s.Marker = MarkerStyle.Triangle; s.EdgeColors = [Colors.Black]; s.LineWidths = [2.0]; }); break;
            case "fill_between": ax.FillBetween(Spread, Wave, [1.0, 1, 1, 1, 1, 1, 1, 1], s => s.EdgeColor = Colors.Black); break;
            case "stackplot": ax.StackPlot(Spread, [Wave, Spread]); break;
            case "violin": ax.Violin([[1.0, 2.0, 2.5, 3.0, 4.0, 4.2, 5.0], [2.0, 3.0, 3.5, 4.0, 5.0, 6.0, 6.5]]); break;
            case "hexbin": ax.Hexbin([1.0, 1.2, 2, 2.1, 3, 3.3, 4, 4.4, 5, 5.5], [1.0, 1.1, 2, 2.4, 3, 3.1, 4, 4.9, 5, 5.2], s => s.GridSize = 3); break;
            case "broken_barh": ax.BrokenBarH([[new BarRange(0.0, 1.0), new BarRange(2.0, 1.0)], [new BarRange(1.0, 2.0)]]); break;
            case "surface": ax.Surface(Axis, Axis, Hill, s => s.EdgeColor = Colors.Black); break;
            case "trisurf": ax.Trisurf([0.0, 1, 0, 1, 0.5], [0.0, 0, 1, 1, 0.5], [0.0, 1, 1, 0, 2], s => s.EdgeColor = Colors.Black); break;
            case "bar3d": ax.Bar3D([1.0, 2, 3], [1.0, 2, 3], [1.0, 2, 3]); break;
            case "voxels": ax.Voxels(new bool[2, 2, 2] { { { true, false }, { false, true } }, { { false, true }, { true, false } } }); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void TheEdgeOfACollection_IsRoundedUnderTheMatplotlibThemes_AndMitredUnderALibraryTheme(string kind, string tag, bool v2Strokes)
    {
        var v2 = StrokedElements(FigureSvg(Theme.MatplotlibV2, ax => CollectionKind(kind, ax)), tag);
        var classic = StrokedElements(FigureSvg(Theme.MatplotlibClassic, ax => CollectionKind(kind, ax)), tag);
        var library = StrokedElements(FigureSvg(Theme.Default, ax => CollectionKind(kind, ax)), tag);

        if (v2Strokes) Assert.NotEmpty(v2);
        Assert.All(v2, join => Assert.Equal("round", join));
        Assert.NotEmpty(classic);
        Assert.All(classic, join => Assert.Equal("round", join));
        Assert.All(library, join => Assert.Equal("", join));
    }

    [Fact]
    public void TheMarkersOfAScatter_AreStrokedByTheCollectionJoin_AndACircleWritesNone()
    {
        string Circles(Theme theme) => FigureSvg(theme, ax => ax.Scatter(Spread, Wave, s => { s.EdgeColors = [Colors.Black]; s.LineWidths = [2.0]; }));

        Assert.NotEmpty(StrokedElements(Circles(Theme.MatplotlibV2), "circle"));
        Assert.All(StrokedElements(Circles(Theme.MatplotlibV2), "circle"), join => Assert.Equal("", join));
    }

    // ── a line's marker is joined as matplotlib's marker of its shape is ─────────────────────────────

    /// <summary>matplotlib 3.11.2 (python): a <c>Line2D</c> draws its markers with the join of the marker, not of the line
    /// (<c>MarkerStyle(m).get_joinstyle()</c>, and <c>line._marker.get_joinstyle()</c> on the plot and on its legend handle): 'bevel' for
    /// <c>'*'</c>, 'round' for <c>'o'</c>, <c>'x'</c> and <c>'+'</c>, 'miter' for <c>'s'</c>, <c>'^'</c>, <c>'v'</c>, <c>'&lt;'</c>,
    /// <c>'&gt;'</c>, <c>'D'</c>, <c>'p'</c> and <c>'h'</c>, under both styles. The library's own themes mitre every marker.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheMarkersOfALine_AreJoinedAsMatplotlibsMarkerOfTheirShape_AndALibraryThemeMitresThem(string themeName)
    {
        var theme = themeName.AsTheme();
        string Line(Theme t, MarkerStyle marker) => FigureSvg(t, ax => ax.Plot([0.0, 1, 2], [0.0, 1, 0], s => { s.Marker = marker; s.Label = "l"; }).WithLegend());
        string Step(Theme t) => FigureSvg(t, ax => ax.Step([0.0, 1, 2], [0.0, 1, 0], s => s.Marker = MarkerStyle.Star));

        var stars = StrokedElements(Line(theme, MarkerStyle.Star), "polygon");
        Assert.Equal(themeName == "v2" ? 4 : 5, stars.Count);   // three on the line, legend.numpoints on its handle (1 default, 2 classic)
        Assert.All(stars, join => Assert.Equal("bevel", join));
        Assert.NotEmpty(StrokedElements(Step(theme), "polygon"));
        Assert.All(StrokedElements(Step(theme), "polygon"), join => Assert.Equal("bevel", join));
        Assert.NotEmpty(StrokedElements(Line(theme, MarkerStyle.Triangle), "polygon"));
        Assert.All(StrokedElements(Line(theme, MarkerStyle.Triangle), "polygon"), join => Assert.Equal("", join));
        Assert.All(StrokedElements(Line(Theme.Default, MarkerStyle.Star), "polygon"), join => Assert.Equal("", join));
    }

    [Fact]
    public void AMarkerShape_HoldsTheJoinOfMatplotlibsMarker()
    {
        Assert.Equal(JoinStyle.Bevel, MarkerShape.Of(MarkerStyle.Star).Join);
        Assert.Equal(JoinStyle.Round, MarkerShape.Of(MarkerStyle.Circle).Join);
        Assert.Equal(JoinStyle.Round, MarkerShape.Of(MarkerStyle.Cross).Join);
        Assert.Equal(JoinStyle.Round, MarkerShape.Of(MarkerStyle.Plus).Join);
        foreach (var mitred in new[] { MarkerStyle.Square, MarkerStyle.Triangle, MarkerStyle.TriangleDown, MarkerStyle.TriangleLeft, MarkerStyle.TriangleRight,
                     MarkerStyle.Diamond, MarkerStyle.Pentagon, MarkerStyle.Hexagon, MarkerStyle.None })
        {
            Assert.Equal(JoinStyle.Miter, MarkerShape.Of(mitred).Join);
            Assert.Equal(JoinStyle.Miter, MarkerShape.Of(mitred, Theme.MatplotlibV2).Join);
        }
    }

    [Fact]
    public void ABar_APieWedgeAndABox_KeepTheirMitre_UnderTheMatplotlibThemes()
    {
        // matplotlib's Rectangle, Wedge and PathPatch are joined 'miter': only lines and collections are rounded.
        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.MatplotlibClassic })
        {
            Assert.DoesNotContain("stroke-linejoin=\"round\"", FigureSvg(theme, ax => ax.Bar(["a", "b"], [1.0, 2.0])));
            Assert.DoesNotContain("stroke-linejoin=\"round\"", FigureSvg(theme, ax => ax.Pie([1.0, 2.0, 3.0])));
            Assert.DoesNotContain("stroke-linejoin=\"round\"", FigureSvg(theme, ax => ax.Hist([1.0, 2, 2, 3, 3, 3, 4])));
        }
    }

    [Fact]
    public void TheOutlineOfAZoomIndicator_IsAPatch_AndKeepsItsMitreUnderTheMatplotlibThemes()
    {
        // matplotlib's indicate_inset draws a Rectangle patch (joined 'miter') beside its connector lines.
        var corners = new ChartRenderer.ZoomIndicatorPath.ZoomCorners(new Point(10, 40), new Point(10, 10), new Point(50, 40), new Point(50, 10));
        var zoom = new ChartRenderer.ZoomIndicatorPath(corners, new Rect(120, 20, 60, 60));
        var ctx = new RecordingRenderContext();

        zoom.Draw(ctx, new ZoomIndicator { LineWidth = 2 }, Theme.MatplotlibV2);

        var outline = Assert.Single(ctx.Of<DrawLinesCall>(), c => c.Points.Length == 5);
        Assert.Equal(JoinStyle.Miter, outline.Join);
    }
}

file static class SvgRenderContextTestExtensions
{
    extension(SvgRenderContext context)
    {
        internal SvgRenderContext Also(Action<SvgRenderContext> draw)
        {
            draw(context);
            return context;
        }
    }
}
