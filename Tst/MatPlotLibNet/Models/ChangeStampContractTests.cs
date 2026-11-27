// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Every change to a figure reports itself. A figure is drawn again only where something changed, so a change that
/// does not report itself is a picture that stays as it was: every property a user can set, every item added to or taken from
/// a list, every interaction and every point a stream appends moves the change mark of the axes that draws it (or of the
/// figure, for what the figure draws itself), and drawing moves nothing. The properties are the drawn-property contract's own
/// (<see cref="DrawnSeriesFixtures"/>, <see cref="DrawnAxesFixtures"/>), so a property added tomorrow is held here too: one case
/// per series type or part, each property set on a figure at rest, every one that reports nothing named.</summary>
public sealed class ChangeStampContractTests
{
    /// <summary>The parts the figure draws itself, outside every subplot: their change is the figure's.</summary>
    private static readonly HashSet<string> FigureParts = ["Figure", "Theme", "FigureColorBar", "FigureLegend", "FigureText", "FigureImage"];

    /// <summary>The parts no user can change in place, each with the reason; the call that adds them reports the change.</summary>
    private static readonly Dictionary<string, string> NotChangedInPlace = new(StringComparer.Ordinal)
    {
        ["XBreak"] = "a break is a record with init-only ends held in a list: AddXBreak, which reports the change, is the only way in",
        ["YBreak"] = "a break is a record with init-only ends held in a list: AddYBreak, which reports the change, is the only way in",
    };

    /// <summary>The parts a user can change in place.</summary>
    public static TheoryData<string> PartsChangedInPlace { get; } =
        new(DrawnAxesFixtures.PartNames.Select(row => row.Data).Where(part => !NotChangedInPlace.ContainsKey(part)));

    /// <summary>The drawn properties of the series type <paramref name="type"/> a user can change after the series exists: an init-only
    /// property is set once, when the series is made, and the call that adds the series reports that.</summary>
    internal static IEnumerable<string> ChangedAfterMade(string type) =>
        DrawnSeriesFixtures.DrawnProperties(DrawnSeriesFixtures.DrawnTypes.Single(t => t.Name == type)).Where(IsPublicSetter).Select(p => p.Name);

    private static long MarkOf(Figure figure, string part) =>
        FigureParts.Contains(part) ? figure.LatestChange : figure.SubPlots[0].LatestChange;

    /// <summary>Every drawn property of a series, each set on a series at rest, moves the change mark of the axes that draws it.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.Seats), MemberType = typeof(DrawnSeriesFixtures))]
    public void SettingADrawnSeriesProperty_MarksItsAxesChanged(string type, SeriesSeat seat)
    {
        Assert.All(ChangedAfterMade(type), property =>
        {
            var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: false, seat);
            var series = DrawnSeriesFixtures.TheSeries(figure, seat);
            long before = figure.SubPlots[0].LatestChange;

            var info = series.GetType().GetProperty(property)!;
            info.SetValue(series, DrawnSeriesFixtures.NotTheDefault(info, info.GetValue(series), 0));

            Assert.True(figure.SubPlots[0].LatestChange > before, $"{type}.{property} on {seat} was set and its axes did not report a change.");
        });
    }

    /// <summary>Every drawn property of a part, each set on a figure at rest, moves the change mark of what draws the part.</summary>
    [Theory]
    [MemberData(nameof(PartsChangedInPlace))]
    public void SettingADrawnPart_MarksWhatDrawsItChanged(string part)
    {
        Assert.All(DrawnAxesFixtures.DrawnPaths(DrawnAxesFixtures.Part(part)), path =>
        {
            var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
            long before = MarkOf(figure, part);

            DrawnAxesFixtures.Set(figure, part, path, 0);

            Assert.True(MarkOf(figure, part) > before, $"{part}.{path} was set and {(FigureParts.Contains(part) ? "the figure" : "its axes")} did not report a change.");
        });
    }

    [Fact]
    public void EveryPartLeftOutOfTheInPlaceRows_IsAPartThatExists()
    {
        foreach (var name in NotChangedInPlace.Keys)
            Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == name);
    }

    /// <summary>The three settable series properties no JSON carries are drawn all the same.</summary>
    [Fact]
    public void SettingADrawnPropertyNoJsonCarries_MarksItsAxesChanged()
    {
        var area = DrawnSeriesFixtures.Build(nameof(AreaSeries), everyPropertySet: false);
        long before = area.SubPlots[0].LatestChange;
        ((AreaSeries)DrawnSeriesFixtures.TheSeries(area)).Where = (x, y) => x > 1;
        Assert.True(area.SubPlots[0].LatestChange > before);

        var map = DrawnSeriesFixtures.Build(nameof(ClustermapSeries), everyPropertySet: false);
        var clustermap = (ClustermapSeries)DrawnSeriesFixtures.TheSeries(map);
        before = map.SubPlots[0].LatestChange;
        clustermap.RowTree = new TreeNode { Label = "rows" };
        Assert.True(map.SubPlots[0].LatestChange > before);
        before = map.SubPlots[0].LatestChange;
        clustermap.ColumnTree = new TreeNode { Label = "columns" };
        Assert.True(map.SubPlots[0].LatestChange > before);
    }

    [Fact]
    public void SettingAPropertyToTheValueItHolds_ReportsNoChange()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        var axes = figure.SubPlots[0];
        var line = (LineSeries)axes.Series[0];
        long axesBefore = axes.LatestChange, figureBefore = figure.LatestChange;

        axes.Title = axes.Title;
        axes.XAxis.Min = axes.XAxis.Min;
        axes.EnableInteractiveAttributes = axes.EnableInteractiveAttributes;
        line.MarkerSize = line.MarkerSize;
        figure.Width = figure.Width;

        Assert.Equal(axesBefore, axes.LatestChange);
        Assert.Equal(figureBefore, figure.LatestChange);
    }

    [Fact]
    public void DrawingAFigure_ChangesNothingInIt()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        figure.Enable3DRotation = true;
        figure.EnableRichTooltips = true;
        _ = figure.ToSvg();
        long axesBefore = figure.SubPlots[0].LatestChange, figureBefore = figure.LatestChange;

        _ = figure.ToSvg();

        Assert.Equal(axesBefore, figure.SubPlots[0].LatestChange);
        Assert.Equal(figureBefore, figure.LatestChange);
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void DrawingASeries_ChangesNothingInIt(string type)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true);
        figure.Enable3DRotation = true;
        _ = figure.ToSvg();
        long before = figure.SubPlots[0].LatestChange;

        _ = figure.ToSvg();

        Assert.Equal(before, figure.SubPlots[0].LatestChange);
    }

    /// <summary>Each list an axes draws from, with the public call that adds to it; every overlay list the contract reflects
    /// off <see cref="Axes"/> has one, so a list added tomorrow goes red here until its call is named.</summary>
    private static readonly Dictionary<string, Action<Axes>> AddsTo = new(StringComparer.Ordinal)
    {
        [nameof(Axes.Annotations)] = axes => axes.Annotate("more", 1, 1),
        [nameof(Axes.ReferenceLines)] = axes => axes.AxVLine(1.5),
        [nameof(Axes.Trendlines)] = axes => axes.AddTrendline(1, 1, 2, 2),
        [nameof(Axes.HorizontalLevels)] = axes => axes.AddLevel(2),
        [nameof(Axes.FibonacciRetracements)] = axes => axes.AddFibonacci(3, 1),
        [nameof(Axes.Spans)] = axes => axes.AxVSpan(1, 2),
        [nameof(Axes.Signals)] = axes => axes.AddSignal(1, 1, SignalDirection.Buy),
        [nameof(Axes.Patches)] = axes => axes.AddPatch(new MatPlotLibNet.Models.Patches.Circle(new(1, 1), 0.1)),
        [nameof(Axes.XBreaks)] = axes => axes.AddXBreak(1.1, 1.2),
        [nameof(Axes.YBreaks)] = axes => axes.AddYBreak(2.1, 2.2),
    };

    public static TheoryData<string> AxesListNames { get; } = new(AddsTo.Keys);

    [Theory]
    [MemberData(nameof(AxesListNames))]
    public void AddingToAnAxesList_MarksItChanged(string list)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        long before = figure.SubPlots[0].LatestChange;

        AddsTo[list](figure.SubPlots[0]);

        Assert.True(figure.SubPlots[0].LatestChange > before, $"Adding to {list} reported no change.");
    }

    [Fact]
    public void EveryOverlayListOfAnAxes_HasACallThatAddsToIt()
    {
        var missing = DrawnAxesFixtures.AxesLists.Select(p => p.Name).Except(AddsTo.Keys).ToList();
        Assert.True(missing.Count == 0, $"No adding call named for: {string.Join(", ", missing)}.");
    }

    [Fact]
    public void AddingASeriesAnInsetASecondaryXSeriesOrAnOutwardYAxis_MarksTheAxesChanged()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        var axes = figure.SubPlots[0];
        double[] x = [1, 2, 3];

        long before = axes.LatestChange;
        axes.Plot(x, x);
        Assert.True(axes.LatestChange > before, "a series");

        before = axes.LatestChange;
        axes.AddInset(0.1, 0.1, 0.2, 0.2);
        Assert.True(axes.LatestChange > before, "an inset");

        before = axes.LatestChange;
        axes.Insets[0].Plot(x, x);
        Assert.True(axes.LatestChange > before, "a series on an inset");

        before = axes.LatestChange;
        axes.PlotXSecondary(x, x);
        Assert.True(axes.LatestChange > before, "a series on the secondary x axis");

        before = axes.LatestChange;
        var slot = axes.YAxes.Right.Add();
        Assert.True(axes.LatestChange > before, "an outward y axis");

        before = axes.LatestChange;
        slot.AddSeries(new LineSeries(x, x));
        Assert.True(axes.LatestChange > before, "a series on an outward y axis");

        before = axes.LatestChange;
        slot.Axis.Label = "rate";
        Assert.True(axes.LatestChange > before, "the label of an outward y axis");

        before = axes.LatestChange;
        axes.YAxes.TitleGap = 9;
        Assert.True(axes.LatestChange > before, "the title gap of the y axes");
    }

    [Fact]
    public void AddingASubplotATextOrAnImage_MarksTheFigureChanged()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);

        long before = figure.LatestChange;
        figure.AddSubPlot();
        Assert.True(figure.LatestChange > before, "a subplot");

        before = figure.LatestChange;
        figure.AddText("more", 0.5, 0.5);
        Assert.True(figure.LatestChange > before, "a text");

        before = figure.LatestChange;
        figure.AddImage(new double[,] { { 1 } }, 1, 1);
        Assert.True(figure.LatestChange > before, "an image");
    }

    [Fact]
    public void RemovingADrawingTool_MarksTheAxesChanged()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        var axes = figure.SubPlots[0];

        foreach (object tool in new object[] { axes.Trendlines[0], axes.HorizontalLevels[0], axes.FibonacciRetracements[0] })
        {
            long before = axes.LatestChange;
            new RemoveDrawingToolEvent("chart", 0, tool).ApplyTo(figure);
            Assert.True(axes.LatestChange > before, $"removing a {tool.GetType().Name} reported no change");
        }
    }

    public static TheoryData<string> MutationEvents { get; } = new(["zoom", "pan", "reset", "rotate", "legend"]);

    [Theory]
    [MemberData(nameof(MutationEvents))]
    public void AnInteractionThatChangesTheFigure_MarksItsAxesChanged(string kind)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [2.0, 4, 3], s => s.Label = "a")).Build();
        if (kind == "rotate") figure.SubPlots[0].CoordinateSystem = CoordinateSystem.ThreeD;
        // A pan moves limits and a reset brings back the ones before a zoom: both need a zoom first to have anything to move.
        if (kind is "reset" or "pan") new ZoomEvent("c", 0, 1.5, 2.5, 2.5, 3.5).ApplyTo(figure);
        long before = figure.SubPlots[0].LatestChange;

        FigureInteractionEvent evt = kind switch
        {
            "zoom" => new ZoomEvent("c", 0, 1.5, 2.5, 2.5, 3.5),
            "pan" => new PanEvent("c", 0, 0.5, 0.25),
            "reset" => new ResetEvent("c", 0, 1, 3, 2, 4),
            "rotate" => new Rotate3DEvent("c", 0, 10, 5),
            _ => new LegendToggleEvent("c", 0, 0),
        };
        evt.ApplyTo(figure);

        Assert.True(figure.SubPlots[0].LatestChange > before, $"{kind} reported no change.");
    }

    [Fact]
    public void AStreamAppendingAPoint_MarksItsAxesChanged()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var stream = axes.AddSeries(new StreamingLineSeries(16));
        long before = axes.LatestChange;

        stream.AppendPoint(1, 2);

        Assert.True(axes.LatestChange > before);
    }

    [Fact]
    public void AChangeLaterThanAnother_CarriesALaterMark()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        var first = figure.SubPlots[0];
        first.Title = "one";
        long earlier = first.LatestChange;

        figure.Width += 10;

        Assert.True(figure.LatestChange > earlier);
    }

    /// <summary>The public types with a public setter that no figure draws from, each with the reason. Every other such type
    /// carries a change mark, so a mutable type added tomorrow goes red here until it reports its changes or is named.</summary>
    private static readonly Dictionary<string, string> NotDrawnFrom = new(StringComparer.Ordinal)
    {
        ["SeriesDto"] = "a data transfer object of the JSON, read once into a series",
        ["InteractionHost"] = "the native controls' input host, not part of a figure",
        ["TouchGestureTranslator"] = "turns touches into pointer input, not part of a figure",
        ["RcParams"] = "process-wide defaults read when a figure is built, not held by it",
        ["AnimationBuilder"] = "builds an animation of figures, not part of one",
        ["EasedFigureAnimation"] = "an animation of figures, not part of one",
        ["AnimationTimer"] = "a timer that drives an animation, not part of a figure; every timer of the library derives from it",
        ["StreamingFigure"] = "a wrapper that renders a figure on a timer; the figure it wraps reports its own changes",
    };

    [Fact]
    public void EveryPublicTypeWithASetter_CarriesAChangeMark_OrIsNamedWithTheReasonItDoesNot()
    {
        var unmarked = typeof(Figure).Assembly.GetExportedTypes()
            .Where(t => t.IsClass && t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(IsPublicSetter))
            .Where(t => !typeof(IChangeTracked).IsAssignableFrom(t))
            .Where(t => !NotDrawnFrom.ContainsKey(t.Name) && !NotDrawnFrom.ContainsKey(t.BaseType!.Name))
            .Where(t => !t.Namespace!.StartsWith("MatPlotLibNet.Indicators", StringComparison.Ordinal))
            .Select(t => t.FullName).Order(StringComparer.Ordinal).ToList();

        Assert.True(unmarked.Count == 0, $"Mutable public types that report no change: {string.Join(", ", unmarked)}.");
    }

    [Fact]
    public void EveryTypeNamedAsNotDrawnFrom_Exists()
    {
        var names = typeof(Figure).Assembly.GetExportedTypes().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(NotDrawnFrom.Keys, name => Assert.Contains(name, names));
    }

    private static bool IsPublicSetter(PropertyInfo p) =>
        p.SetMethod is { IsPublic: true } set && !set.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit");
}
