// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The edge each kind of patch series draws its shapes with, under the library's own theme, a library theme that names a
/// patch edge colour, matplotlib's default style and its classic style, pinned for every kind and every way its author can name an
/// edge. Written down at the tree where each kind kept its own rule after the theme's (a <c>??</c> in its renderer), and held
/// unchanged by the one owner (<c>Theme.PatchEdgeOf</c>) that replaced them: no picture moves. Each row is the colour (RGBA)
/// and the width in pixels of the stroke of every shape the series drew, <c>none</c> for no stroke; a width is the one the shape was
/// handed, so a shape with no stroke shows 0 where the theme decided none and the width where the series' rule did.</summary>
public class PatchEdgePinTests
{
    private static readonly Color Ink = Color.FromHex("#B0A001");
    private static readonly Color Green = Color.FromHex("#00AA00");

    /// <summary>The one theme row that is no built-in theme: the library's own with a patch edge colour of its own (the rows that name
    /// a built-in one are read by <c>AsTheme</c>).</summary>
    private static readonly Theme LibraryWithEdge = new("library with an edge colour", Theme.Default) { PatchEdgeColor = Green };

    private static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}");

    private static string Stroke(Color? color, double width) =>
        color is { } c ? string.Create(CultureInfo.InvariantCulture, $"{Hex(c)}/{width:0.###}") : string.Create(CultureInfo.InvariantCulture, $"none/{width:0.###}");

    private static RecordingRenderContext Draw(Theme theme, Func<AxesBuilder, AxesBuilder> build)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => build(ax)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

    /// <summary>The distinct strokes of every shape of the given kinds whose fill is the colour <paramref name="fill"/> (alpha aside), or
    /// every one of them when <paramref name="fill"/> is <see langword="null"/>.</summary>
    private static string Strokes(RecordingRenderContext ctx, Type[] kinds, Color? fill)
    {
        var rows = ctx.Calls.OfType<ShapeCall>().Where(c => kinds.Contains(c.GetType()))
            .Where(c => fill is null || (c.Fill is Color f && f.R == fill.Value.R && f.G == fill.Value.G && f.B == fill.Value.B))
            .Select(c => Stroke(c.Stroke, c.StrokeThickness))
            .Distinct().ToList();
        return rows.Count == 0 ? "no shape" : string.Join(" ", rows);
    }

    private static readonly Type[] Rects = [typeof(DrawRectangleCall)];
    private static readonly Type[] Polys = [typeof(DrawPolygonCall)];
    private static readonly Type[] Paths = [typeof(DrawPathCall)];

    private static string Of(string kind, string scenario, Theme theme)
    {
        var tree = new TreeNode { Label = "root", Color = Ink, Children = [new TreeNode { Label = "a", Value = 2, Color = Ink }, new TreeNode { Label = "b", Value = 1, Color = Ink }] };
        string[][] cells = [["1", "2"], ["3", "4"]];
        return (kind, scenario) switch
        {
            ("bar", "plain") => Strokes(Draw(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => b.Color = Ink)), Rects, Ink),
            ("bar", "red") => Strokes(Draw(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; })), Rects, Ink),
            ("bar", "wide") => Strokes(Draw(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.LineWidth = 2; })), Rects, Ink),
            ("bar", "red+wide") => Strokes(Draw(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.LineWidth = 2; })), Rects, Ink),
            ("bar", "red+zero") => Strokes(Draw(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.LineWidth = 0; })), Rects, Ink),
            ("hist", "plain") => Strokes(Draw(theme, ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, h => h.Color = Ink)), Rects, Ink),
            ("hist", "red") => Strokes(Draw(theme, ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, h => { h.Color = Ink; h.EdgeColor = Colors.Red; })), Rects, Ink),
            ("count", "plain") => Strokes(Draw(theme, ax => ax.Countplot(["a", "b", "a"], c => c.Color = Ink)), Rects, Ink),
            ("brokenbar", "plain") => Strokes(Draw(theme, ax => ax.BrokenBarH([[new BarRange(1, 2), new BarRange(4, 1)]], b => b.Color = Ink)), Rects, Ink),
            ("gantt", "plain") => Strokes(Draw(theme, ax => ax.Gantt(["a", "b"], [0.0, 1.0], [2.0, 3.0], g => g.Color = Ink)), Rects, Ink),
            ("waterfall", "plain") => Strokes(Draw(theme, ax => ax.Waterfall(["a", "b"], [3.0, -1.0], w => { w.IncreaseColor = Ink; w.DecreaseColor = Ink; })), Rects, Ink),
            ("donut", "plain") => Strokes(Draw(theme, ax => ax.Donut([1.0, 2.0], configure: d => d.Colors = [Ink, Ink])), Paths, Ink),
            ("pie", "plain") => Strokes(Draw(theme, ax => ax.Pie([1.0, 2.0], configure: p => p.Colors = [Ink, Ink])), Paths, Ink),
            ("polarbar", "plain") => Strokes(Draw(theme, ax => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => { b.Color = Ink; b.Alpha = 1; })), Polys, Ink),
            ("stackedarea", "plain") => Strokes(Draw(theme, ax => ax.StackPlot([1.0, 2, 3], [[1.0, 2, 3], [2.0, 1, 2]])), Polys, null),
            ("area", "plain") => Strokes(Draw(theme, ax => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => a.Color = Ink)), Polys, Ink),
            ("area", "red") => Strokes(Draw(theme, ax => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; a.EdgeColor = Colors.Red; })), Polys, Ink),
            ("area", "wide") => Strokes(Draw(theme, ax => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; a.LineWidth = 2; })), Polys, Ink),
            ("treemap", "plain") => Strokes(Draw(theme, ax => ax.Treemap(tree)), Rects, Ink),
            ("violin", "plain") => Strokes(Draw(theme, ax => ax.Violin([[1.0, 2, 3, 4], [2.0, 3, 4, 5]], v => v.Color = Ink)), Polys, Ink),
            ("table", "red") => Strokes(Draw(theme, ax => ax.Table(cells, t => { t.CellColor = Ink; t.BorderColor = Colors.Red; })), Rects, Ink),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), $"{kind} {scenario}"),
        };
    }

    // The columns: library, library+edge, v2, classic. P = 1 point in pixels = 1.389; a violin's classic edge is black at the body's
    // alpha (0.3 -> 76); a treemap tile under v2 is handed no stroke and width 0.
    [Theory]
    [InlineData("bar", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("bar", "red", "FF0000FF/1", "FF0000FF/1", "FF0000FF/1.389", "FF0000FF/1.389")]
    [InlineData("bar", "wide", "B0A001FF/2", "B0A001FF/2", "none/0", "000000FF/2")]
    [InlineData("bar", "red+wide", "FF0000FF/2", "FF0000FF/2", "FF0000FF/2", "FF0000FF/2")]
    [InlineData("bar", "red+zero", "FF0000FF/1", "FF0000FF/1", "none/0", "none/0")]
    [InlineData("hist", "plain", "none/0", "00AA00FF/0.5", "none/0", "000000FF/1.389")]
    [InlineData("hist", "red", "FF0000FF/0.5", "FF0000FF/0.5", "FF0000FF/1.389", "FF0000FF/1.389")]
    [InlineData("count", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("brokenbar", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("gantt", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("waterfall", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("donut", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("pie", "plain", "FFFFFFFF/1", "FFFFFFFF/1", "none/0", "000000FF/1.389")]
    [InlineData("polarbar", "plain", "B0A001FF/0.5", "B0A001FF/0.5", "none/0", "000000FF/1.389")]
    [InlineData("stackedarea", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("area", "plain", "none/0", "none/0", "none/0", "000000FF/1.389")]
    [InlineData("area", "red", "none/0", "none/0", "FF0000FF/1.389", "FF0000FF/1.389")]
    [InlineData("area", "wide", "none/0", "none/0", "none/0", "none/0")]
    [InlineData("treemap", "plain", "FFFFFFFF/1", "FFFFFFFF/1", "none/0", "000000FF/1.389")]
    [InlineData("violin", "plain", "B0A001FF/1", "B0A001FF/1", "none/0", "0000004C/1.389")]
    [InlineData("table", "red", "FF0000FF/1", "FF0000FF/1", "FF0000FF/1.389", "FF0000FF/1.389")]
    public void EveryKindOfPatchSeries_KeepsItsEdge_UnderEveryThemeFamily(string kind, string scenario, string library, string libraryEdge, string v2, string classic)
    {
        Assert.Equal(library, Of(kind, scenario, "library".AsTheme()));
        Assert.Equal(libraryEdge, Of(kind, scenario, LibraryWithEdge));
        Assert.Equal(v2, Of(kind, scenario, "v2".AsTheme()));
        Assert.Equal(classic, Of(kind, scenario, "classic".AsTheme()));
    }

    /// <summary>The opacity of the fill of the three kinds that name none themselves: the matplotlib themes draw it opaque (matplotlib's
    /// <c>fill_between</c>, <c>stackplot</c> and polar <c>bar</c> name no alpha), the library's own at the kind's resting one
    /// (<see cref="MatPlotLibNet.Models.Series.AreaSeries"/> 0.3, the stacked area's 0.7, the polar bar's 0.8); an alpha given is
    /// drawn under every theme.</summary>
    [Theory]
    [InlineData("area", null)]
    [InlineData("stackedarea", null)]
    [InlineData("polarbar", null)]
    [InlineData("area", 0.5)]
    [InlineData("stackedarea", 0.5)]
    [InlineData("polarbar", 0.5)]
    public void TheFillOfAnAreaAStackAndAPolarBar_IsDrawnAtTheOpacityTheThemeDecides_OrAsGiven(string kind, double? given)
    {
        double resting = kind switch { "area" => 0.3, "stackedarea" => 0.7, _ => 0.8 };
        byte Alpha(Color c) => c.A;
        byte Of(Theme theme)
        {
            var ctx = Draw(theme, ax => kind switch
            {
                "area" => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; if (given is { } g) a.Alpha = g; }),
                "stackedarea" => ax.StackPlot([1.0, 2, 3], [[1.0, 2, 3], [2.0, 1, 2]], s => { if (given is { } g) s.Alpha = g; }),
                _ => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => { b.Color = Ink; if (given is { } g) b.Alpha = g; }),
            });
            var shape = ctx.Of<DrawPolygonCall>().First();
            return Alpha(shape.Fill!.Value);
        }

        byte Byte(double a) => (byte)Math.Round(a * 255);
        Assert.Equal(Byte(given ?? resting), Of("library".AsTheme()));
        Assert.Equal(Byte(given ?? 1), Of("v2".AsTheme()));
        Assert.Equal(Byte(given ?? 1), Of("classic".AsTheme()));
    }
}
