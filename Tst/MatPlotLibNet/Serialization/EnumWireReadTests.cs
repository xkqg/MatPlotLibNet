// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Every text field a document names an enum member in, read back. A member is named by its name, in any case, and by nothing else:
/// a name in another case reads as the member, and a text that names no member (an unknown word, a number, two names joined by a
/// comma, a name with a space before it) is announced on <see cref="ChartDiagnostics"/> and leaves the setting where a document without
/// it leaves it. Each row is one setting of one record, written by the library itself and then altered as a hand-made or newer
/// document would alter it.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class EnumWireReadTests
{
    private static readonly double[] X = [1.0, 2, 3, 4];
    private static readonly double[] Y = [2.0, 1, 4, 3];

    /// <summary>One setting a document names an enum member for.</summary>
    /// <param name="Name">What the row is.</param>
    /// <param name="Build">Sets the setting to <paramref name="Member"/> on an empty axes.</param>
    /// <param name="Key">The key the setting is written under.</param>
    /// <param name="Written">The name the library writes for <paramref name="Member"/>.</param>
    /// <param name="Read">The setting, read off the axes a document was read into.</param>
    /// <param name="Member">The member the setting is set to.</param>
    /// <param name="Rest">What the setting is when the document does not name it.</param>
    /// <param name="Other">A second member's name, to join to the first by a comma.</param>
    public sealed record Site(string Name, Action<Axes> Build, string Key, string Written, Func<Axes, object?> Read, object Member, object? Rest, string Other);

    private static readonly Site[] Sites =
    [
        new("bar orientation", a => a.Bar(["a", "b"], [1.0, 2]).Orientation = BarOrientation.Horizontal,
            "orientation", "horizontal", a => a.Series.OfType<BarSeries>().Single().Orientation, BarOrientation.Horizontal, BarOrientation.Vertical, "vertical"),
        new("count orientation", a => a.Countplot(["a", "b", "a"]).Orientation = BarOrientation.Horizontal,
            "orientation", "horizontal", a => a.Series.OfType<CountSeries>().Single().Orientation, BarOrientation.Horizontal, BarOrientation.Vertical, "vertical"),
        new("area orientation", a => a.FillBetweenX(Y, X),
            "orientation", "horizontal", a => a.Series.OfType<AreaSeries>().Single().Orientation, BarOrientation.Horizontal, BarOrientation.Vertical, "vertical"),
        new("bullet graph orientation", a => a.Bullet(0.5).Orientation = Orientation.Vertical,
            "orientation", "vertical", a => a.Series.OfType<BulletGraphSeries>().Single().Orientation, Orientation.Vertical, Orientation.Horizontal, "horizontal"),
        new("cycle call", a => a.Plot(X, Y).AsksAs = CycleCall.Scatter,
            "asksAs", "scatter", a => a.Series.OfType<LineSeries>().Single().AsksAs, CycleCall.Scatter, null, "line"),
        new("step position", a => a.Step(X, Y).StepPosition = StepPosition.Mid,
            "stepPosition", "mid", a => a.Series.OfType<StepSeries>().Single().StepPosition, StepPosition.Mid, StepPosition.Post, "pre"),
        new("series line style", a => a.Plot(X, Y).LineStyle = LineStyle.Dashed,
            "lineStyle", "dashed", a => a.Series.OfType<LineSeries>().Single().LineStyle, LineStyle.Dashed, LineStyle.Solid, "dotted"),
        new("label placement", a => a.Plot(X, Y).LabelPlacement = SeriesLabelPlacement.LineEnd,
            "labelPlacement", "lineend", a => a.Series.OfType<LineSeries>().Single().LabelPlacement, SeriesLabelPlacement.LineEnd, SeriesLabelPlacement.Legend, "legend"),
        new("network graph layout", a => a.NetworkGraph([new("a"), new("b")], [new("a", "b")]).Layout = GraphLayout.ForceDirected,
            "networkGraphLayout", "ForceDirected", a => a.Series.OfType<NetworkGraphSeries>().Single().Layout, GraphLayout.ForceDirected, GraphLayout.Circular, "Manual"),
        new("heatmap mask mode", a => a.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).MaskMode = HeatmapMaskMode.UpperTriangle,
            "maskMode", "UpperTriangle", a => a.Series.OfType<HeatmapSeries>().Single().MaskMode, HeatmapMaskMode.UpperTriangle, HeatmapMaskMode.None, "LowerTriangle"),
        new("pair grid diagonal", a => a.PairGrid([X, Y], p => p.DiagonalKind = PairGridDiagonalKind.Kde),
            "pairGridDiagonal", "Kde", a => a.Series.OfType<PairGridSeries>().Single().DiagonalKind, PairGridDiagonalKind.Kde, PairGridDiagonalKind.Histogram, "None"),
        new("pair grid off diagonal", a => a.PairGrid([X, Y], p => p.OffDiagonalKind = PairGridOffDiagonalKind.None),
            "pairGridOffDiagonal", "None", a => a.Series.OfType<PairGridSeries>().Single().OffDiagonalKind, PairGridOffDiagonalKind.None, PairGridOffDiagonalKind.Scatter, "Scatter"),
        new("pair grid triangle", a => a.PairGrid([X, Y], p => p.Triangular = PairGridTriangle.LowerOnly),
            "pairGridTriangular", "LowerOnly", a => a.Series.OfType<PairGridSeries>().Single().Triangular, PairGridTriangle.LowerOnly, PairGridTriangle.Both, "UpperOnly"),
        new("relative rotation formula", a => a.RelativeRotation([[1.0, 2, 3, 4, 5, 6]], [1.0, 2, 3, 4, 5, 6], ["a"]).Formula = RrgFormula.ZScore,
            "rrgFormula", "ZScore", a => a.Series.OfType<RelativeRotationSeries>().Single().Formula, RrgFormula.ZScore, RrgFormula.DualEma, "DualEma"),
        new("coordinate system", a => a.CoordinateSystem = CoordinateSystem.Polar,
            "coordinateSystem", "polar", a => a.CoordinateSystem, CoordinateSystem.Polar, CoordinateSystem.Cartesian, "ternary"),
        new("bar mode", a => a.BarMode = BarMode.Stacked,
            "barMode", "stacked", a => a.BarMode, BarMode.Stacked, BarMode.Grouped, "grouped"),
        new("axis scale", a => a.XAxis.Scale = AxisScale.Log,
            "scale", "log", a => a.XAxis.Scale, AxisScale.Log, AxisScale.Linear, "symlog"),
        new("axis break style", a => a.AddXBreak(1, 2, BreakStyle.Straight),
            "style", "straight", a => a.XBreaks.Single().Style, BreakStyle.Straight, BreakStyle.Zigzag, "none"),
        new("spine position", a => a.Spines = a.Spines with { Left = a.Spines.Left with { Position = SpinePosition.Data } },
            "position", "data", a => a.Spines.Left.Position, SpinePosition.Data, SpinePosition.Edge, "axes"),
        new("annotation connection style", a => a.Annotate("note", 1, 1).ConnectionStyle = ConnectionStyle.Arc3,
            "connectionStyle", "arc3", a => a.Annotations.Single().ConnectionStyle, ConnectionStyle.Arc3, ConnectionStyle.Straight, "angle"),
        new("annotation box style", a => a.Annotate("note", 1, 1).BoxStyle = BoxStyle.Round,
            "boxStyle", "round", a => a.Annotations.Single().BoxStyle, BoxStyle.Round, BoxStyle.None, "square"),
        new("reference line orientation", a => a.AxVLine(3),
            "orientation", "vertical", a => a.ReferenceLines.Single().Orientation, Orientation.Vertical, Orientation.Horizontal, "horizontal"),
        new("reference line line style", a => a.AxHLine(3).LineStyle = LineStyle.Dotted,
            "lineStyle", "dotted", a => a.ReferenceLines.Single().LineStyle, LineStyle.Dotted, LineStyle.Dashed, "solid"),
        new("span orientation", a => a.AxVSpan(1, 2),
            "orientation", "vertical", a => a.Spans.Single().Orientation, Orientation.Vertical, Orientation.Horizontal, "horizontal"),
        new("span line style", a => a.AxHSpan(1, 2).LineStyle = LineStyle.Dashed,
            "lineStyle", "dashed", a => a.Spans.Single().LineStyle, LineStyle.Dashed, LineStyle.None, "dotted"),
        new("trendline line style", a => a.AddTrendline(0, 0, 1, 1).LineStyle = LineStyle.Dotted,
            "lineStyle", "dotted", a => a.Trendlines.Single().LineStyle, LineStyle.Dotted, LineStyle.Solid, "dashed"),
        new("horizontal level line style", a => a.AddLevel(5).LineStyle = LineStyle.Dotted,
            "lineStyle", "dotted", a => a.HorizontalLevels.Single().LineStyle, LineStyle.Dotted, LineStyle.Dashed, "solid"),
    ];

    public static TheoryData<string> SiteNames { get; } = new(Sites.Select(site => site.Name));

    /// <summary>Texts that name no member, each spelled from the site's own written name where it needs one.</summary>
    public static TheoryData<string, string> SitesAndTextsThatNameNoMember { get; } = TextsThatNameNoMember();

    private static TheoryData<string, string> TextsThatNameNoMember()
    {
        var data = new TheoryData<string, string>();
        foreach (var site in Sites)
        {
            data.Add(site.Name, "zigzagzag");
            data.Add(site.Name, "1");
            data.Add(site.Name, "99");
            data.Add(site.Name, $"{site.Written}, {site.Other}");
            data.Add(site.Name, $" {site.Written}");
        }

        return data;
    }

    private static Site SiteNamed(string name) => Sites.Single(site => site.Name == name);

    private static string Written(Site site)
    {
        var figure = new Figure { Width = 400, Height = 300 };
        site.Build(figure.AddSubPlot(1, 1, 1));
        return figure.ToJson();
    }

    private static string Pair(string key, string value) => $"\"{key}\":\"{value}\"";

    private static Captured<Axes> ReadWith(Site site, string value)
    {
        string json = Written(site).Replace(Pair(site.Key, site.Written), Pair(site.Key, value), StringComparison.Ordinal);
        return DiagnosticsCapture.Of(() => new ChartSerializer().FromJson(json).SubPlots[0]);
    }

    [Theory]
    [MemberData(nameof(SiteNames))]
    public void TheMember_IsWrittenOnceUnderItsKey_AsItsName(string name)
    {
        var site = SiteNamed(name);

        string json = Written(site);

        int at = json.IndexOf(Pair(site.Key, site.Written), StringComparison.Ordinal);
        Assert.True(at >= 0, $"{name}: {Pair(site.Key, site.Written)} not in {json}");
        Assert.Equal(-1, json.IndexOf(Pair(site.Key, site.Written), at + 1, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(SiteNames))]
    public void TheNameInAnotherCase_ReadsAsTheMember_AndNothingIsAnnounced(string name)
    {
        var site = SiteNamed(name);

        var (axes, announced) = ReadWith(site, site.Written.ToUpperInvariant());

        Assert.Equal(site.Member, site.Read(axes));
        Assert.Empty(announced);
    }

    [Theory]
    [MemberData(nameof(SitesAndTextsThatNameNoMember))]
    public void ATextThatNamesNoMember_IsAnnounced_AndLeavesTheSettingWhereAnAbsentOneLeavesIt(string name, string text)
    {
        var site = SiteNamed(name);

        var (axes, announced) = ReadWith(site, text);

        Assert.Equal(site.Rest, site.Read(axes));
        Assert.Contains(announced, d => d.Message.Contains($"'{text}'", StringComparison.Ordinal)
                                        && d.Message.Contains(site.Member.GetType().Name, StringComparison.Ordinal));
    }
}
