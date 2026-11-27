// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The size of the text a series writes where its author names none. matplotlib 3.11.2 sets every such text from
/// <c>rcParams['font.size']</c>, in points: <c>clabel(fontsize=None)</c> ("default: font.size"), a table cell (<c>Cell</c> builds
/// its <c>Text</c> with no size, read back as 10.0 under the default style and 12.0 under the classic one), <c>Axes3D.text</c> and
/// <c>QuiverKey</c>'s label (<c>Text(fontproperties={})</c>), 10 pt and 12 pt, and the text a series writes beside its data. The
/// library states a text's size in pixels, so the matplotlib themes draw 10 pt = 13.89 px and 12 pt = 16.67 px; the library's own
/// themes keep the size each series has always drawn, which the series names beside its <c>FontSize</c>. A size the author names is
/// what it says (pixels, a quiver key's in points) and is never taken for the one a new series holds, so a given 10 under v2 is 10 px.
/// No renderer keeps a size of its own.</summary>
public class SeriesTextSizeTests
{
    private static readonly double V2Size = 10.0.PointsInPixels;
    private static readonly double ClassicSize = 12.0.PointsInPixels;

    private static readonly double[] Xs = [0.0, 1, 2, 3, 4, 5];
    private static readonly double[] Ys = [0.0, 1, 2, 3];
    private static readonly double[] Asset = [.. Enumerable.Range(0, 60).Select(i => 100 + 10 * Math.Sin(i * 0.3) + i * 0.5)];
    private static readonly double[] Benchmark = [.. Enumerable.Range(0, 60).Select(i => 100 + i * 0.2)];
    private static readonly TreeNode Tree = new() { Label = "root", Children = [new TreeNode { Label = "leaf", Value = 3 }, new TreeNode { Label = "other", Value = 2 }] };

    private static double[,] Ramp()
    {
        var z = new double[4, 6];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 6; c++)
                z[r, c] = 2.0 * c;
        return z;
    }

    /// <summary>One chart, the text it writes, the size its series has always drawn that text in pixels, and the same chart with a size named.</summary>
    private readonly record struct Case(Func<double?, Action<AxesBuilder>> Draw, string Text, double RestingPixels);

    private static readonly Dictionary<string, Case> Cases = new()
    {
        ["ContourLabel"] = new(size => ax => ax.Contour(Xs, Ys, Ramp(), s => { s.LevelValues = [0.74]; s.ShowLabels = true; if (size is { } v) s.LabelFontSize = v; }), "0.74", 10),
        ["TableCell"] = new(size => ax => ax.Table([["cellular"]], s => { if (size is { } v) s.FontSize = v; }), "cellular", 11),
        ["Text3D"] = new(size => ax => ax.Text3D(1, 2, 3, "hello3d", s => { if (size is { } v) s.FontSize = v; }), "hello3d", 10),
        ["TreeGridRow"] = new(size => ax => ax.TreeGrid([new("Svc.Billing", ["10 %"]) { Depth = 0 }], s => { s.ColumnHeaders = ["CPU"]; if (size is { } v) s.FontSize = v; }), "Svc.Billing", 12),
        ["ProgressBar"] = new(size => ax => ax.ProgressBar(0.62, s => { if (size is { } v) s.FontSize = v; }), "62%", 11),
        ["StateTimeline"] = new(size => ax => ax.StateTimeline([new StateSegment(0, 10, "busy", Colors.Tab10Blue)], s => { if (size is { } v) s.FontSize = v; }), "busy", 11),
        ["Donut"] = new(size => ax => ax.Donut([1, 2], null, s => { s.CenterText = "Total"; if (size is { } v) s.FontSize = v; }), "Total", 14),
        ["Gauge"] = new(size => ax => ax.Gauge(42, s => { if (size is { } v) s.FontSize = v; }), "42", 12),
        ["NetworkWeight"] = new(size => ax => ax.NetworkGraph([new GraphNode("a", 0, 0), new GraphNode("b", 1, 1)], [new GraphEdge("a", "b", 2.5)],
            s => { s.ShowEdgeWeights = true; if (size is { } v) s.WeightFontSize = v; }), "2.5", 9),
        ["Icicle"] = new(size => ax => ax.Icicle(Tree, s => { if (size is { } v) s.FontSize = v; }), "leaf", 12),
        ["Treemap"] = new(size => ax => ax.Treemap(Tree, s => { if (size is { } v) s.FontSize = v; }), "leaf", 12),
        ["StatTile"] = new(size => ax => ax.StatTile(99.9, s => { s.Label = "Uptime"; if (size is { } v) s.FontSize = v; }), "Uptime", 14),
        ["RelativeRotation"] = new(size => ax => ax.RelativeRotation([Asset], Benchmark, ["asset"], s => { if (size is { } v) s.FontSize = v; }), "asset", 9),
    };

    public static TheoryData<string> Names => [.. Cases.Keys];

    private static Figure Of(Theme theme, Action<AxesBuilder> chart) =>
        Plt.Create().WithSize(800, 500).WithTheme(theme).AddSubPlot(1, 1, 1, chart).Build();

    private static double SizeOf(Figure figure, string text) => figure.Drawn().Of<DrawTextCall>().First(c => c.Text == text).Font.Size;

    [Theory]
    [MemberData(nameof(Names))]
    public void ATextThatNamesNoSize_IsTheFontSizeOfAMatplotlibTheme(string name)
    {
        var chart = Cases[name];

        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Size, SizeOf(Of(Theme.MatplotlibV2, chart.Draw(null)), chart.Text), 9);
        Assert.Equal(Theme.MatplotlibClassic.DefaultFont.Size, SizeOf(Of(Theme.MatplotlibClassic, chart.Draw(null)), chart.Text), 9);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ATextThatNamesNoSize_KeepsItsOwnPixelsUnderTheLibrarysThemes(string name)
    {
        var chart = Cases[name];

        Assert.Equal(chart.RestingPixels, SizeOf(Of(Theme.Default, chart.Draw(null)), chart.Text), 9);
        Assert.Equal(chart.RestingPixels, SizeOf(Of(Theme.Dark, chart.Draw(null)), chart.Text), 9);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ASizeThatIsNamed_IsThatManyPixels_EvenWhenItEqualsTheOneANewSeriesHolds(string name)
    {
        var chart = Cases[name];

        Assert.Equal(chart.RestingPixels, SizeOf(Of(Theme.MatplotlibV2, chart.Draw(chart.RestingPixels)), chart.Text), 9);
        Assert.Equal(17.0, SizeOf(Of(Theme.MatplotlibClassic, chart.Draw(17.0)), chart.Text), 9);
        Assert.Equal(17.0, SizeOf(Of(Theme.Default, chart.Draw(17.0)), chart.Text), 9);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ASizeThatIsNamed_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed(string name)
    {
        var chart = Cases[name];

        Assert.Equal(chart.RestingPixels, SizeOf(Of(Theme.MatplotlibV2, chart.Draw(chart.RestingPixels)).RoundTripped(), chart.Text), 9);
        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Size, SizeOf(Of(Theme.MatplotlibV2, chart.Draw(null)).RoundTripped(), chart.Text), 9);
    }

    /// <summary>A Sankey node's name keeps its own rule, the theme's text capped at 10 px under every theme (the cap keeps a long name
    /// inside its band; matplotlib's Sankey is a flow diagram of another shape): measured, the theme's size takes the classic Fidelity row
    /// below its limit. A size its author gives wins.</summary>
    [Fact]
    public void ASankeyName_IsTheThemesTextCappedAtTen_UnlessGiven()
    {
        static Action<AxesBuilder> Sankey(double? size) => ax =>
            ax.Sankey([new SankeyNode("From"), new SankeyNode("To")], [new SankeyLink(0, 1, 5)], s => { if (size is { } v) s.FontSize = v; });

        Assert.Equal(10, SizeOf(Of(Theme.MatplotlibV2, Sankey(null)), "From"), 9);
        Assert.Equal(10, SizeOf(Of(Theme.Default, Sankey(null)), "From"), 9);
        Assert.Equal(17, SizeOf(Of(Theme.MatplotlibV2, Sankey(17)), "From"), 9);
        Assert.Equal(8, SizeOf(Of(new Theme("small", Theme.Default) { DefaultFont = Theme.Default.DefaultFont with { Size = 8 } }, Sankey(null)), "From"), 9);
    }

    // ── a quiver key's label: points, the theme's text when none is named ─────────────────────────────────────────

    private static Figure Key(Theme theme, double? points) =>
        Of(theme, ax => ax.QuiverKey(0.5, 0.5, 1.0, "key", s => { if (points is { } p) s.FontSize = p; }));

    [Fact]
    public void AKeyThatNamesNoSize_IsTheThemesText_UnderEveryTheme()
    {
        Assert.Equal(V2Size, SizeOf(Key(Theme.MatplotlibV2, null), "key"), 9);
        Assert.Equal(ClassicSize, SizeOf(Key(Theme.MatplotlibClassic, null), "key"), 9);
        Assert.Equal(Theme.Default.DefaultFont.Size, SizeOf(Key(Theme.Default, null), "key"), 9);
    }

    [Fact]
    public void AKeyThatNamesTwelvePoints_IsTwelvePoints_NotTheThemesText()
    {
        Assert.Equal(ClassicSize, SizeOf(Key(Theme.MatplotlibV2, 12), "key"), 9);
        Assert.Equal(ClassicSize, SizeOf(Key(Theme.Default, 12), "key"), 9);
    }

    [Fact]
    public void AKeyThatNamesNoSize_WritesNoSizeToTheJson_AndOneThatDoesIsRead_AsNamed()
    {
        string unnamed = Key(Theme.MatplotlibV2, null).ToJson();
        string named = Key(Theme.MatplotlibV2, 12).ToJson();

        Assert.DoesNotContain("markerSize", unnamed);
        Assert.Contains("markerSize", named);
        Assert.Equal(V2Size, SizeOf(ChartSerializerDefault.Current.FromJson(unnamed), "key"), 9);
        Assert.Equal(ClassicSize, SizeOf(ChartSerializerDefault.Current.FromJson(named), "key"), 9);
    }

    // ── the theme's setting travels with it ───────────────────────────────────────────────────────────────────────

}
