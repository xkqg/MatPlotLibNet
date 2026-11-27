// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one way a series is drawn by a series pass (<see cref="SeriesPass"/>), made by its factories: in its group, through the
/// pass' transform, with its marks cut to the pass' clip region when the axes has an explicit limit, and for a pass over a Cartesian plot
/// with its group carrying that transform; the one loop (<see cref="SeriesPass.DrawSteps"/>) every renderer and every axis part draws its
/// series by, numbering each by its seat and handing the host what belongs inside its group and every step that is not a series.</summary>
public class SeriesPassTests
{
    private static readonly Rect Plot = new(10, 10, 200, 100);

    private static Axes LimitedAxes(string? label)
    {
        var axes = new Axes { EnableInteractiveAttributes = true };
        var line = axes.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]);
        line.Label = label;
        axes.XAxis.Min = 0.5;
        return axes;
    }

    /// <summary>Draws every series of <paramref name="pass"/> through its one loop for a host that holds colour slot
    /// <paramref name="colorSlot"/> for each.</summary>
    private static void Draw(in SeriesPass pass, int colorSlot = 0)
    {
        var host = new TestLayerHost([.. Enumerable.Repeat(colorSlot, pass.Series.Count + 4)]);
        pass.Draw(ref host);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A Cartesian pass with its plot area as clip region (as the Cartesian renderer builds it) clips the marks of an axes with a set
        // limit, and its series group carries the pass' transform when the draw writes the data-cursor mapping.
        new DrawnToken("ACartesianPass_ClipsToAnExplicitLimit_AndHandsItsTransformToTheGroup", () =>
        {
            var axes = LimitedAxes("a");
            var svg = new SvgRenderContext { Emit = new SvgEmit(axes, true, true, null, false) };
            var pass = SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, Theme.Default), Plot.Linear(xMax: 2, yMax: 4));
            Draw(pass);
            return svg.GetOutput();
        }, "class=\"series\"", "clip-path", "data-xmin=\"0\"", "<polyline"),
        // The clip and the group's transform are two decisions: a pass that is not over a Cartesian plot but carries a clip region (a
        // polar pass with an explicit limit) clips its marks and still hands no transform to the group.
        new DrawnToken("APassWithAClipThatIsNotCartesian_ClipsButHandsNoTransformToTheGroup", () =>
        {
            var axes = LimitedAxes("a");
            var svg = new SvgRenderContext { Emit = new SvgEmit(axes, true, true, null, false) };
            var pass = SeriesPass.OnUnitSquare(axes, new RenderPass(Plot, svg, Theme.Default), null) with { Clip = new ClipRegion(Plot) };
            Draw(pass);
            return svg.GetOutput();
        }, "clip-path", "<polyline") { Absent = ["data-xmin"] },
        // A pass with no clip region that is not Cartesian (ternary, 3-D) neither clips nor hands its transform to the group, whatever
        // limit the axes carries.
        new DrawnToken("APassThatIsNotCartesian_NeitherClipsNorHandsItsTransformToTheGroup", () =>
        {
            var axes = LimitedAxes("a");
            var svg = new SvgRenderContext { Emit = new SvgEmit(axes, true, true, null, false) };
            var pass = SeriesPass.OnUnitSquare(axes, new RenderPass(Plot, svg, Theme.Default), null);
            Draw(pass);
            return svg.GetOutput();
        }, "class=\"series\"", "<polyline") { Absent = ["clip-path", "data-xmin"] },
        // A series that gets no group (unlabelled, on a draw without interactive attributes) is still drawn, and the pass says no group is
        // open.
        new DrawnToken("ASeriesThatGetsNoGroup_IsStillDrawn", () =>
        {
            var axes = LimitedAxes(null);
            axes.EnableInteractiveAttributes = false;
            var svg = new SvgRenderContext();
            var pass = SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, Theme.Default), Plot.Linear(xMax: 2, yMax: 4));
            Draw(pass);
            return svg.GetOutput();
        }, "<polyline") { Absent = ["class=\"series\""] },
        // The library's own themes keep their rule: an axes that scaled itself encloses every series, so it draws no clip.
        new DrawnToken("UnderTheLibrarysThemes_ACartesianPassWithoutALimit_DrawsNoClip", () =>
        {
            var axes = new Axes();
            axes.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]);
            var svg = new SvgRenderContext();
            Draw(SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, Theme.Default), Plot.Linear(xMax: 2, yMax: 4)));
            return svg.GetOutput();
        }) { Absent = ["clip-path"] },
        // The colour slot the series is drawn in is the one handed to the pass, not its own index.
        new DrawnToken("TheSeries_WearsTheColourSlotItIsHanded", () =>
        {
            var axes = LimitedAxes(null);
            var svg = new SvgRenderContext();
            var pass = SeriesPass.OnUnitSquare(axes, new RenderPass(Plot, svg, Theme.Default), null);
            Draw(pass, colorSlot: 1);
            return svg.GetOutput();
        }, Theme.Default.SeriesSlot(1).CycleColor.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A pass over a Cartesian plot cuts its marks off at the plot area of the pass it is made in, the one default: no caller
    /// hands it the plot again; a map's outline is the only other clip it is told, inside the same plot.</summary>
    [Fact]
    public void APassOverThePlot_ClipsToThePlotOfItsRender_OrToTheOutlineItIsTold()
    {
        var render = new RenderPass(Plot, new SvgRenderContext(), Theme.Default);
        PathSegment[] outline = [new MoveToSegment(new Point(10, 10)), new LineToSegment(new Point(90, 10)), new CloseSegment()];

        Assert.Equal(new ClipRegion(Plot), SeriesPass.OverPlot(new Axes(), render, Plot.Linear(xMax: 2, yMax: 4)).Clip);
        Assert.Equal(new ClipRegion(Plot, outline), SeriesPass.OverPlot(new Axes(), render, Plot.Linear(xMax: 2, yMax: 4), outline).Clip);
    }

    /// <summary>Every part of a Cartesian render draws into the render's own pass: the plot, the context and the theme are one value
    /// the part context carries, and the part reads them from it.</summary>
    [Fact]
    public void ThePartContext_CarriesTheRenderPass_ItsPartsDrawInto()
    {
        var render = new RenderPass(Plot, new SvgRenderContext(), Theme.MatplotlibV2);
        var context = new MatPlotLibNet.Rendering.CartesianParts.CartesianPartContext(new Axes(), render, [], Plot.Linear(xMax: 2, yMax: 4));

        Assert.Equal(render, context.Pass);
        Assert.Equal(Plot, context.PlotArea);
        Assert.Same(render.Context, context.Ctx);
        Assert.Same(Theme.MatplotlibV2, context.Theme);
    }

    // ── the clip under the matplotlib themes ──────────────────────────────────────────────────────────────────────────────

    /// <summary>matplotlib clips every artist to the axes, with a limit or without one. Printed with matplotlib 3.11.2 under both styles, for
    /// <c>ax.plot</c>, <c>ax.bar</c>, <c>ax.scatter</c> and <c>ax.fill_between</c> on an axes that scaled itself: <c>clip_on True</c>, the clip
    /// box the axes' bbox (<c>add_line</c>: <c>line.set_clip_path(self.patch)</c>). A bar's edge, a marker or a thick line at the end of the
    /// data reaches past the spine otherwise. Under the matplotlib themes a Cartesian pass clips every series to its plot without a limit.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibThemes))]
    public void UnderTheMatplotlibThemes_ACartesianPassClipsEverySeries_WithoutALimit(Theme theme)
    {
        var axes = new Axes();
        axes.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]);
        var svg = new SvgRenderContext();
        Draw(SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, theme), Plot.Linear(xMax: 2, yMax: 4)));

        Assert.Contains("clip-path", svg.GetOutput());
    }

    /// <summary>The artists matplotlib draws with <c>clip_on=False</c> are never cut off at the axes, a limit set or not. Printed with
    /// matplotlib 3.11.2 under both styles: <c>ax.pie(...)</c> gives its wedges and texts <c>clip_on False</c> (a donut is a pie with a
    /// <c>wedgeprops</c> width), <c>ax.table(...)</c> its table and cells <c>clip_on False</c>. Under the matplotlib themes a pie, a donut and a
    /// table are drawn whole even under an explicit limit; the library's own themes keep their rule and clip them there.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibThemes))]
    public void UnderTheMatplotlibThemes_APieADonutAndATable_AreNeverClipped_ALimitSetOrNot(Theme theme)
    {
        foreach (var series in new ISeries[]
                 {
                     new PieSeries([1.0, 2.0]), new DonutSeries([1.0, 2.0]),
                     new TableSeries([["a", "b"]]),
                 })
        {
            Assert.DoesNotContain("clip-path", DrawnUnderALimit(series, theme));
            Assert.Contains("clip-path", DrawnUnderALimit(series, Theme.Default));
        }
    }

    /// <summary>A kind of the library's own, which no matplotlib verb draws (a gauge, a relative rotation graph, a Sankey diagram), is cut
    /// off at the axes under the matplotlib themes as every artist an axes adds, a limit set or not: its names and values are text, which
    /// no clip cuts (<see cref="SeriesTextClipTests"/>). A stacked area is matplotlib's <c>stackplot</c> (printed with 3.11.2 under both
    /// styles: each layer's <c>fill_between</c> collection <c>clip_on True</c>). The library's own themes keep their rule for them.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibThemes))]
    public void UnderTheMatplotlibThemes_AKindOfTheLibrarysOwn_IsClippedAsEveryArtistIs(Theme theme)
    {
        ISeries[] own =
        [
            new GaugeSeries(42),
            new RelativeRotationSeries([[1.0, 2, 3, 4, 5, 6]], [1.0, 1, 1, 1, 1, 1], ["a"]),
            new SankeySeries([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 5)]),
            new StackedAreaSeries([0.0, 1.0], [[1.0, 2.0]]),
        ];
        foreach (var series in own)
        {
            Assert.Contains("clip-path", Drawn(series, theme, limited: false));
            Assert.Contains("clip-path", Drawn(series, theme, limited: true));
            Assert.DoesNotContain("clip-path", Drawn(series, Theme.Default, limited: false));
        }
    }

    /// <summary>Under the library's own themes a kind whose marks stand past the range it reports is cut off at the plot without a limit
    /// too, by the series pass, once: an autoscaled quiver (an arrow's length is no part of the range), a streamplot (the arrow on a line),
    /// a hexbin (the hexagons at the edge reach half a step past the extent). A quiver given a scale ranges over its heads and is not.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_AKindWhoseMarksStandPastItsRange_IsClippedOnce_WithoutALimit()
    {
        double[] x = [0.0, 1.0, 2.0], y = [0.0, 1.0, 2.0];
        ISeries[] reaching =
        [
            new QuiverSeries(x, y, [1.0, 1.0, 1.0], [1.0, 0.0, -1.0]) { AutoScale = true },
            new StreamplotSeries(x, y, new double[,] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } }, new double[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }),
            new HexbinSeries([0.0, 1.0, 2.0, 0.5], [0.0, 1.0, 2.0, 1.5]),
        ];
        foreach (var series in reaching)
        {
            string svg = Drawn(series, Theme.Default, limited: false);
            Assert.Equal(1, svg.Split("<clipPath").Length - 1);
        }

        Assert.DoesNotContain("clip-path", Drawn(new QuiverSeries(x, y, [1.0, 1.0, 1.0], [1.0, 0.0, -1.0]) { AutoScale = false }, Theme.Default, limited: false));
    }

    /// <summary>The SVG of <paramref name="series"/> drawn by a Cartesian pass of an axes with an explicit limit.</summary>
    private static string DrawnUnderALimit(ISeries series, Theme theme) => Drawn(series, theme, limited: true);

    /// <summary>The SVG of <paramref name="series"/> drawn by a Cartesian pass of an axes with an explicit limit or without one.</summary>
    private static string Drawn(ISeries series, Theme theme, bool limited)
    {
        var axes = new Axes();
        axes.AddSeries(series);
        if (limited) axes.XAxis.Min = 0.5;
        var svg = new SvgRenderContext();
        Draw(SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, theme), Plot.Linear(xMax: 2, yMax: 4)));
        return svg.GetOutput();
    }

    public static TheoryData<Theme> MatplotlibThemes => new() { Theme.MatplotlibV2, Theme.MatplotlibClassic };

    // ── the factories and the one loop ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFactories_SayWhatKindOfPassItIs()
    {
        var axes = new Axes();
        var render = new RenderPass(Plot, new RecordingRenderContext(), Theme.MatplotlibV2);
        var transform = Plot.Linear(xMax: 2, yMax: 4);
        var patches = new List<MatPlotLibNet.Models.Patches.Patch> { new MatPlotLibNet.Models.Patches.Rectangle(new(0, 0), 1, 1) };

        var over = SeriesPass.OverPlot(axes, render, transform);
        var unit = SeriesPass.OnUnitSquare(axes, render, patches);
        var layered = SeriesPass.Layered(axes, render, patches);

        Assert.Same(transform, over.ClickMapping);
        Assert.Same(Theme.MatplotlibV2, over.OrderedBy);
        Assert.Null(over.Patches);
        Assert.Null(over.GridZ);
        Assert.NotNull(over.Clip);
        Assert.Null(unit.ClickMapping);
        Assert.Null(unit.Clip);
        Assert.Same(Theme.Default, unit.OrderedBy);
        Assert.Same(patches, unit.Patches);
        Assert.Null(unit.GridZ);
        var square = SeriesPass.UnitSquare(Plot);
        Assert.Equal(new Point(Plot.X, Plot.Y + Plot.Height), square.DataToPixel(0, 0));
        Assert.Equal(new Point(Plot.X + Plot.Width, Plot.Y), square.DataToPixel(1, 1));
        Assert.Equal(square.DataToPixel(0.5, 0.5), unit.Transform.DataToPixel(0.5, 0.5));
        Assert.Same(Theme.MatplotlibV2, layered.OrderedBy);
        Assert.Equal(Theme.MatplotlibV2.GridZ, layered.GridZ);
        Assert.Same(patches, layered.Patches);
        Assert.Same(axes.Series, over.Series);
        Assert.Equal(SeriesAxisPair.Primary, over.Pair);
    }

    /// <summary>The loop numbers each series by its seat, draws it in the colour of that slot, hands the host the slot for what belongs
    /// inside the group, skips a hidden series, and hands the host every step that is not a series, once.</summary>
    [Fact]
    public void TheOneLoop_NumbersEachSeriesByItsSeat_SkipsAHiddenOne_AndHandsTheHostTheRest()
    {
        var axes = new Axes { EnableInteractiveAttributes = true };
        axes.Plot([0.0, 1.0], [0.0, 1.0]);
        var slot = axes.YAxes.Right.Add();
        var shown = slot.AddSeries(new LineSeries([0.0, 1.0], [0.0, 1.0]) { Label = "shown" });
        slot.AddSeries(new LineSeries([0.0, 1.0], [0.0, 1.0]) { Label = "hidden", Visible = false });
        var svg = new SvgRenderContext { Emit = new SvgEmit(axes, true, true, null, false) };
        var inside = new List<int>();
        var host = new TestLayerHost(axes.ColorSlots(Theme.Default), inside.Add);
        var pass = SeriesPass.OverPlot(axes, new RenderPass(Plot, svg, Theme.Default), Plot.Linear(xMax: 2, yMax: 4))
            with { Series = slot.Series, Pair = SeriesAxisPair.SecondaryY, FirstIndexInPair = 0 };
        LayerStep[] steps = [new(0, 0, DataLayer.Grid, 0), new(0, 1, DataLayer.Series, 0), new(0, 2, DataLayer.Series, 1), new(0, 3, DataLayer.Spans, 0)];

        pass.DrawSteps(steps, ref host);

        Assert.Equal([axes.SlotIndex(SeriesAxisPair.SecondaryY, 0)], inside);
        Assert.Equal([DataLayer.Grid, DataLayer.Spans], host.Layers.Select(step => step.Layer));
        Assert.Contains($"data-series-index=\"{axes.SlotIndex(SeriesAxisPair.SecondaryY, 0)}\"", svg.GetOutput());
        Assert.DoesNotContain("hidden", svg.GetOutput());
        Assert.Equal("shown", shown.Label);
    }
}
