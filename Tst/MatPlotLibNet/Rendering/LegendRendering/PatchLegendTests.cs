// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>A labelled patch is a row of the legend, as matplotlib lists one: a rectangle swatch in the patch's own style, after the
/// rows of the series (a recorded departure: matplotlib lists in the order things were added), never toggled. A patch without a label,
/// or with one starting with an underscore, has no row. The legend of the axes and the legend of the figure list it, and the box is
/// measured with it.</summary>
public class PatchLegendTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static Rectangle Region(string? label) => new(new(0.2, 1.2), 1.0, 1.0) { Label = label };

    /// <summary>The texts of the first legend group drawn: the legend of the first axes, or the figure's when the axes draw none.</summary>
    private static string[] LegendTexts(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int begin = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "legend");
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c.Kind == "EndGroup");
        return [.. ctx.Calls.GetRange(begin, end - begin + 1).Where(c => c.Kind == "DrawText").Select(c => (string)((dynamic)c.Args!).text)];
    }

    /// <summary>The texts of the last legend group drawn: the figure's legend, drawn after every subplot.</summary>
    private static string[] FigureLegendTexts(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int begin = ctx.Calls.FindLastIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "legend");
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c.Kind == "EndGroup");
        return [.. ctx.Calls.GetRange(begin, end - begin + 1).Where(c => c.Kind == "DrawText").Select(c => (string)((dynamic)c.Args!).text)];
    }

    private static List<(int Index, string Label)> ToggledEntries(string svg) =>
        [.. Regex.Matches(svg, "<g data-legend-index=\"(?<i>\\d+)\"[^>]*aria-label=\"(?<l>[^\"]*)\"")
            .Select(m => (int.Parse(m.Groups["i"].Value, CultureInfo.InvariantCulture), m.Groups["l"].Value))];

    // ── Which patches have a row ───────────────────────────────────────────

    [Fact]
    public void ALabelledPatch_IsARowOfTheLegend()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "line").AddPatch(Region("region"))).Build();

        Assert.Equal(["line", "region"], LegendTexts(figure));
    }

    /// <summary>matplotlib's rule (<c>_get_legend_handles_labels</c>): an empty label, or one starting with an underscore, is left out.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("_")]
    [InlineData("_hidden")]
    public void APatchWithoutALabel_OrWithOneStartingWithAnUnderscore_HasNoRow(string? label)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "line").AddPatch(Region(label))).Build();

        Assert.Equal(["line"], LegendTexts(figure));
    }

    [Fact]
    public void AnUnderscoreAnywhereButFirst_KeepsTheRow()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("a_b"))
            .AddPatch(Region(" _lead"))).Build();

        Assert.Equal(["line", "a_b", " _lead"], LegendTexts(figure));
    }

    /// <summary>Whether a patch is listed is its label's say alone, as in matplotlib: a patch of no opacity keeps its row.</summary>
    [Fact]
    public void AClearPatch_IsStillListed()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(new Circle(new(1, 2), 0.5) { Label = "clear", Alpha = 0 })).Build();

        Assert.Equal(["clear"], LegendTexts(figure));
    }

    /// <summary>A recorded departure: matplotlib lists in the order things were added; here the series come first, then the patches in
    /// the order they were added.</summary>
    [Fact]
    public void ThePatchRows_ComeAfterTheSeriesRows_InTheOrderThePatchesWereAdded()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .AddPatch(Region("first"))
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(new Wedge(new(1, 2), 0.5, 0, 90) { Label = "second" })
            .Scatter(X, Y, s => s.Label = "points")).Build();

        Assert.Equal(["line", "points", "first", "second"], LegendTexts(figure));
    }

    [Fact]
    public void APatchRow_TakesNoLegendValue()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithLegendValues()
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();

        Assert.Equal(["line = 2.00", "region"], LegendTexts(figure));
    }

    [Fact]
    public void ALegendOfPatchesOnly_ListsThem()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .AddPatch(Region("region"))
            .AddPatch(new Arrow(new(0, 0), 1, 1) { Width = 0.05, Label = "arrow" })
            .AddPatch(new Arc(new(1, 1), 1, 1) { Label = "arc" })).Build();

        Assert.Equal(["region", "arrow", "arc"], LegendTexts(figure));
    }

    /// <summary>Only a Cartesian axes draws patches; a legend never names a shape that is not drawn.</summary>
    [Fact]
    public void APatchOnPolarAxes_IsNotDrawn_AndHasNoRow()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0], s => s.Label = "P")
            .AddPatch(Region("region"))).Build();

        Assert.Equal(["P"], LegendTexts(figure));
    }

    /// <summary>The renderer that draws an axes says whether it draws patches, and its legend lists them only then: a Cartesian axes
    /// and an inset do, a polar, 3-D or ternary axes does not.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Cartesian, false, true)]
    [InlineData(CoordinateSystem.Cartesian, true, true)]
    [InlineData(CoordinateSystem.Polar, false, false)]
    [InlineData(CoordinateSystem.ThreeD, false, false)]
    [InlineData(CoordinateSystem.Ternary, false, false)]
    public void OnlyARendererThatDrawsPatches_ListsThem(CoordinateSystem system, bool inset, bool listed)
    {
        var parent = new Axes { CoordinateSystem = system };
        var axes = inset ? parent.AddInset(new InsetBounds(0.5, 0.5, 0.4, 0.4)) : parent;
        axes.Plot(X, Y).Label = "line";
        axes.AddPatch(Region("region"));

        var renderer = AxesRenderer.Create(axes, new Rect(0, 0, 400, 300), new SvgRenderContext(), Theme.Default);

        string[] expected = listed ? ["line", "region"] : ["line"];
        Assert.Equal(listed, renderer.DrawsPatches);
        Assert.Equal(expected, renderer.LegendEntries(legendValues: false).Select(e => e.Label));
    }

    // ── The swatch ─────────────────────────────────────────────────────────

    /// <summary>Everything the legend draws for <paramref name="entry"/> alone, with no frame: the calls before its text.</summary>
    private static List<CallRecord> SwatchOf(LegendEntry entry, out LegendLayout layout)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(new Legend { Position = LegendPosition.UpperRight, FrameOn = false }, Theme.Default, ctx);
        layout = renderer.Place([entry], new Rect(80, 60, 400, 300), () => MarkObstacles.None)!;
        renderer.Draw([entry], layout, interactive: true);
        return [.. ctx.Calls.Skip(1).TakeWhile(c => c.Kind is not ("DrawText" or "DrawRichText"))];
    }

    /// <summary>matplotlib's <c>HandlerPatch</c>: the handle is a rectangle the full handle long and high, in the patch's own face,
    /// edge, width, dash and hatch, the opacity already inside each colour (no group opacity).</summary>
    [Fact]
    public void APatchRow_IsARectangleSwatch_TheFullHandle_InThePatchsOwnStyle()
    {
        var patch = new Rectangle(new(0, 0), 1, 1)
        {
            Label = "styled", FaceColor = Colors.Tab10Orange, EdgeColor = Colors.Black, EdgeWidth = 2,
            EdgeLineStyle = LineStyle.Dashed, Alpha = 0.5, Hatch = HatchPattern.Cross,
        };

        var calls = SwatchOf(new PatchLegendEntry(patch, "styled"), out var layout);

        var swatch = Assert.Single(calls);
        Assert.Equal("DrawRectangle", swatch.Kind);
        Assert.Equal(new Rect(layout.ColumnX(0), layout.EntryY(0), layout.SwatchWidth, layout.SwatchHeight), (Rect)((dynamic)swatch.Args!).rect);
        var style = (ShapeStyle)((dynamic)swatch.Args!).shape;
        Assert.Equal(patch.DrawnStyle, style);
        Assert.Equal(Colors.Tab10Orange.ScaleAlpha(0.5), style.Fill);
        Assert.Equal(Colors.Black.ScaleAlpha(0.5), style.Stroke);
        Assert.Equal((2.0, LineStyle.Dashed, HatchPattern.Cross), (style.StrokeThickness, style.StrokeLineStyle, style.Hatch));
    }

    public static TheoryData<string> Kinds => ["Rectangle", "Polygon", "Arrow", "Circle", "Ellipse", "Wedge", "Arc"];

    private static Patch Of(string kind) => kind switch
    {
        "Rectangle" => new Rectangle(new(0, 0), 1, 1),
        "Polygon" => new Polygon([new(0, 0), new(1, 0), new(0, 1)]) { EdgeColor = Colors.Red },
        "Arrow" => new Arrow(new(0, 0), 1, 1),
        "Circle" => new Circle(new(0, 0), 1) { FaceColor = Colors.Green },
        "Ellipse" => new Ellipse(new(0, 0), 2, 1) { Hatch = HatchPattern.Dots },
        "Wedge" => new Wedge(new(0, 0), 1, 0, 90) { Alpha = 0.25 },
        "Arc" => new Arc(new(0, 0), 2, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Every kind of patch has the one swatch, as every patch has in matplotlib: an arc's is an outline with no face.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryKindOfPatch_HasTheOneRectangleSwatch_InItsDrawnStyle(string kind)
    {
        var patch = Of(kind);
        patch.Label = kind;

        var swatch = Assert.Single(SwatchOf(new PatchLegendEntry(patch, kind), out _));

        Assert.Equal("DrawRectangle", swatch.Kind);
        Assert.Equal(patch.DrawnStyle, (ShapeStyle)((dynamic)swatch.Args!).shape);
    }

    /// <summary>matplotlib refuses to fill an arc, and its legend handle is a rectangle with no face and the arc's black edge.</summary>
    [Fact]
    public void AnArcsSwatch_IsAnOutlineWithNoFace()
    {
        var style = (ShapeStyle)((dynamic)Assert.Single(SwatchOf(new PatchLegendEntry(new Arc(new(0, 0), 2, 1) { Label = "arc" }, "arc"), out _)).Args!).shape;

        Assert.Null(style.Fill);
        Assert.Equal(Colors.Black, style.Stroke);
    }

    [Fact]
    public void APatchRow_IsLabelledByThePatch_AndCarriesNoToggleIndex()
    {
        var entry = new PatchLegendEntry(Region("region"), "region");

        Assert.Equal("region", entry.Label);
        Assert.Null(entry.ToggleIndex);
    }

    // ── Never toggled ──────────────────────────────────────────────────────

    [Fact]
    public void APatchRow_HasNoToggleGroup_InAnInteractiveSvg()
    {
        var figure = Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();

        string svg = figure.ToSvg();
        string legend = svg[svg.IndexOf("<g class=\"legend\"", StringComparison.Ordinal)..];

        Assert.Equal([(0, "line")], ToggledEntries(svg));
        Assert.Contains(">region<", legend);
    }

    /// <summary>The index a series' row and its drawn group carry is its own identity slot, with or without patches beside it.</summary>
    [Fact]
    public void TheSeriesIndices_AreTheSame_WithAndWithoutPatches()
    {
        FigureBuilder Chart(bool patches) => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax =>
        {
            if (patches) ax.AddPatch(Region("before"));
            ax.Plot(X, Y).Plot(X, Y, s => s.Label = "B").Scatter(X, Y, s => s.Label = "C");
            if (patches) ax.AddPatch(Region("after"));
        });

        string with = Chart(true).Build().ToSvg();
        string without = Chart(false).Build().ToSvg();

        Assert.Equal([(1, "B"), (2, "C")], ToggledEntries(with));
        Assert.Equal(ToggledEntries(without), ToggledEntries(with));
        Assert.Equal(Regex.Matches(without, "data-series-index=\"\\d+\"").Select(m => m.Value),
            Regex.Matches(with, "data-series-index=\"\\d+\"").Select(m => m.Value));
    }

    /// <summary>The hit-test has a box only for the rows a click toggles: a click on a patch's row finds no series and raises no
    /// toggle, a click on the series' row above it does.</summary>
    [Fact]
    public void AClickOnAPatchRow_TogglesNothing()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .WithLegend(LegendPosition.UpperRight)
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();
        var result = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        var layout = ChartLayout.Create(figure, result);
        var events = new List<FigureInteractionEvent>();
        var toggle = new LegendToggleModifier("chart", layout, events.Add);

        var line = Assert.Single(result.LegendItems[0]);
        double x = line.Bounds.X + 2;
        var onPatchRow = new PointerInputArgs(x, line.Bounds.Y + 1.5 * line.Bounds.Height, PointerButton.Left, ModifierKeys.None);
        var onLineRow = new PointerInputArgs(x, line.Bounds.Y + 0.5 * line.Bounds.Height, PointerButton.Left, ModifierKeys.None);

        Assert.Equal(0, layout.HitTestAxes(onPatchRow.X, onPatchRow.Y));
        Assert.Null(layout.HitTestLegendItem(onPatchRow.X, onPatchRow.Y, 0));
        Assert.False(toggle.HandlesPointerPressed(onPatchRow));
        toggle.OnPointerPressed(onPatchRow);
        Assert.Empty(events);

        Assert.True(toggle.HandlesPointerPressed(onLineRow));
        toggle.OnPointerPressed(onLineRow);
        Assert.Equal(0, Assert.IsType<LegendToggleEvent>(Assert.Single(events)).SeriesIndex);
    }

    // ── The legend of the figure ───────────────────────────────────────────

    [Fact]
    public void TheFigureLegend_ListsEachSubplotsPatchRows_AfterThatSubplotsSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("p1")).Plot(X, Y, s => s.Label = "a1"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b1"))
            .FigLegend()
            .Build();

        Assert.Equal(["a1", "p1", "b1"], FigureLegendTexts(figure));
    }

    [Fact]
    public void TheFigureLegend_ListsAPatchDrawnTheSameInTwoSubplots_Once()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .FigLegend()
            .Build();

        Assert.Equal(["zone"], FigureLegendTexts(figure));
    }

    [Fact]
    public void TheFigureLegend_ListsTheSamePatchLabelInAnotherStyle_Again()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(new Rectangle(new(0, 0), 1, 1) { Label = "zone", Hatch = HatchPattern.Cross }))
            .FigLegend()
            .Build();

        Assert.Equal(["zone", "zone"], FigureLegendTexts(figure));
    }

    /// <summary>A series and a patch are drawn differently in a legend, whatever their colour, so one label on both is two rows.</summary>
    [Fact]
    public void TheFigureLegend_ListsASeriesAndAPatchOfOneLabel_Both()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Bar(["a", "b"], [1.0, 2.0], s => { s.Label = "x"; s.Color = Colors.Tab10Blue; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(Region("x")))
            .FigLegend()
            .Build();

        Assert.Equal(["x", "x"], FigureLegendTexts(figure));
    }
}
