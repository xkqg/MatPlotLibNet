// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>One part of an axes the figure JSON has to carry: the name a test row carries, the object in a figure that
/// holds the part, the property path from that object to the part (empty when the holder is the part), the part's type,
/// and, for a part that is a few properties of a bigger type, which properties those are.</summary>
/// <param name="Name">The part's name in a test row.</param>
/// <param name="Holder">Finds the object that holds the part in a figure built by <see cref="DrawnAxesFixtures.Build"/>.</param>
/// <param name="Path">The property path from the holder to the part, dot separated; empty when the holder is the part.</param>
/// <param name="Type">The part's type, whose public settable properties are the contract.</param>
/// <param name="Only">The properties of <paramref name="Type"/> that belong to the part, or <see langword="null"/> for all.</param>
public readonly record struct DrawnPart(string Name, Func<Figure, object> Holder, string Path, Type Type, IReadOnlyList<string>? Only = null);

/// <summary>The parts a figure draws besides its series (each axis, the title, the legend, the colour bar, the grid, the
/// spines and the overlays of an axes, and the figure's own settings and theme), and every property of theirs that
/// changes the picture. As for the series (<see cref="DrawnSeriesFixtures"/>) the properties are REFLECTED off the types,
/// never written by hand: a property added tomorrow joins the contract on its own and the JSON has to carry it or a test
/// names it. A record a part is built from (a tick configuration, a text style, a font, a spacing, a palette) contributes
/// its own properties one by one, so a field lost inside it is named too.
/// <para>Public API only, so the Mcp test project links this very file and runs the same contract through its spec
/// reader.</para></summary>
public static class DrawnAxesFixtures
{
    /// <summary>The records a part is built from whose properties join the contract one by one.</summary>
    private static readonly HashSet<Type> NestedRecords =
    [
        typeof(TickConfig), typeof(TextStyle), typeof(Font), typeof(GridStyle), typeof(SubPlotSpacing),
        typeof(InteractionTheme), typeof(GridSpec), typeof(AlarmPalette),
    ];

    /// <summary>The settable properties of <see cref="Figure"/> that the "Figure" part does not hold, each with the
    /// reason. Every other settable property of the figure joins the contract on its own.</summary>
    public static IReadOnlyDictionary<string, string> FigureExemptions { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [nameof(Figure.Theme)] = "a part of its own (Theme), held property by property",
        [nameof(Figure.FigureColorBar)] = "a part of its own (FigureColorBar), held property by property",
        [nameof(Figure.Legend)] = "a part of its own (FigureLegend), held property by property",
    };

    /// <summary>The get-only list properties of <see cref="Figure"/> other than its subplots (the texts and the images placed on
    /// the figure itself). A list has no setter, so <see cref="FigureSettings"/> never sees it: each list is held by the parts
    /// of its items ("FigureText", "FigureImage"), by the values of its items that the parts cannot set
    /// (<see cref="FigureListValues"/>) and by its length. Reflected, so a list added tomorrow is walked too, and the contract
    /// goes red until the fixture holds an item of it and the JSON carries the list.</summary>
    public static IReadOnlyList<PropertyInfo> FigureLists { get; } =
        [.. typeof(Figure).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is null && p.GetIndexParameters().Length == 0)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            .Where(p => p.PropertyType.GetGenericArguments()[0] != typeof(Axes))
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>The length of each list of <see cref="FigureLists"/> and every readable value of every item of it, the ones
    /// with a setter and the ones given to the constructor alike, keyed by where it stands (<c>Texts[0].X</c>). A grid of
    /// numbers is held as its size and its cells in row order, since a flat comparison of the cells alone would take a
    /// 2 by 3 image for a 3 by 2 one.</summary>
    /// <param name="figure">The figure, built by <see cref="Build"/>.</param>
    public static IReadOnlyDictionary<string, object?> FigureListValues(Figure figure) => ListValues(figure, FigureLists, item => true);

    /// <summary>The overlay lists of an axes: every public get-only list of <see cref="Axes"/> whose items are neither series,
    /// axes nor Y axis slots (the series have their own contract, an inset is an axes read back by the same path, and the slots
    /// are walked through the container). An item's settable properties are held by a part of its type (<see cref="Parts"/>);
    /// the values it was made with, which no setter reaches (a span's edges, a signal's place), by <see cref="AxesListValues"/>.
    /// Reflected, so an overlay kind added tomorrow is walked too.</summary>
    public static IReadOnlyList<PropertyInfo> AxesLists { get; } =
        [.. typeof(Axes).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is null && p.GetIndexParameters().Length == 0)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            .Where(p => p.PropertyType.GetGenericArguments()[0] is var item
                && !typeof(ISeries).IsAssignableFrom(item) && item != typeof(Axes) && !IsYAxisSlot(item))
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>Whether <paramref name="item"/> is a Y axis slot: the container's <see cref="YAxisSlot"/>, or any list item
    /// that carries a Y axis' <see cref="YAxisSlot.Axis"/> the way a slot does.</summary>
    private static bool IsYAxisSlot(Type item) =>
        item == typeof(YAxisSlot) || item.GetProperty(nameof(YAxisSlot.Axis))?.PropertyType == typeof(Axis);

    /// <summary>The item types of an overlay list (<see cref="AxesLists"/>) the JSON does not carry, as <c>List: Type</c>, each
    /// with the reason. Every other concrete item type of every overlay list has a part (<see cref="Parts"/>) that holds an item of
    /// that list, and its values are held by <see cref="AxesListValues"/>.</summary>
    public static IReadOnlyDictionary<string, string> OverlaysNotCarried { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The length of each overlay list of the subplot (<see cref="AxesLists"/>) and every readable value of every item of
    /// it, keyed by where it stands (<c>Spans[0].Min</c>), as <see cref="FigureListValues"/> holds the figure's. An item of a type
    /// named in <see cref="OverlaysNotCarried"/> is left out, and so is its share of the length.</summary>
    /// <param name="figure">The figure, built by <see cref="Build"/>.</param>
    public static IReadOnlyDictionary<string, object?> AxesListValues(Figure figure) =>
        ListValues(TheAxes(figure), AxesLists, item => !OverlaysNotCarried.ContainsKey(OverlayKey(item.List, item.Type)));

    /// <summary>The key of an item type of an overlay list in <see cref="OverlaysNotCarried"/>: <c>List: Type</c>.</summary>
    /// <param name="list">The overlay list's name.</param>
    /// <param name="type">The item type.</param>
    public static string OverlayKey(string list, Type type) => $"{list}: {type.Name}";

    /// <summary>An item of a list, by the list's name and the item's type.</summary>
    private readonly record struct ListItem(string List, Type Type);

    /// <summary>The length of each of <paramref name="lists"/> of <paramref name="owner"/> and every readable value of every item
    /// of it that <paramref name="held"/> takes: the one walk <see cref="FigureListValues"/> and <see cref="AxesListValues"/>
    /// share.</summary>
    private static IReadOnlyDictionary<string, object?> ListValues(object owner, IEnumerable<PropertyInfo> lists, Func<ListItem, bool> held)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var list in lists)
        {
            var items = ((System.Collections.IEnumerable)list.GetValue(owner)!).Cast<object>()
                .Where(item => held(new ListItem(list.Name, item.GetType()))).ToList();
            values[$"{list.Name}.Count"] = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                foreach (var property in items[i].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.GetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0))
                {
                    string at = $"{list.Name}[{i}].{property.Name}";
                    if (property.GetValue(items[i]) is double[,] grid)
                    {
                        values[$"{at}.Rows"] = grid.GetLength(0);
                        values[$"{at}.Columns"] = grid.GetLength(1);
                        values[at] = grid.Cast<double>().ToArray();
                    }
                    else
                    {
                        values[at] = property.GetValue(items[i]);
                    }
                }
            }
        }

        return values;
    }

    // ---- what an axes holds that no public setter reaches ---------------------------------------------------------

    /// <summary>The properties of <see cref="Axes"/> whose setter no caller outside the library can use: set by the call that
    /// adds the subplot (its cell, its grid position, the axes it shares a scale with), by the call that adds an inset or the top
    /// axis or changes the grid, or by the renderer. The "AxesSettings" part reflects public setters only, so it never sees them:
    /// each is set by a public call of <see cref="InternallySetRoutes"/> and comes back from JSON, or is named in
    /// <see cref="InternallySetNotCarried"/> with the reason. Reflected, internal properties included, so one added tomorrow is
    /// caught.</summary>
    public static IReadOnlyList<PropertyInfo> InternallySet { get; } =
        [.. typeof(Axes).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: false } && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>The internally set properties of <see cref="Axes"/> (<see cref="InternallySet"/>) no JSON carries, each with the
    /// reason. Named by string: some are internal, and the Mcp test project compiles this file against the public API.</summary>
    public static IReadOnlyDictionary<string, string> InternallySetNotCarried { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["EmitDataCursorMapping"] = "the SVG writer sets it on every axes for each render, from the figure's ServerInteraction and "
            + "EnableDataCursor, which the JSON carries: the state of one render, not something an author sets",
        ["ServerViewIndex"] = "the SVG writer sets it on every axes for each render, from the figure's ServerInteraction and the axes' "
            + "place among the subplots, both of which the JSON keeps: the state of one render, not something an author sets",
    };

    /// <summary>A public call that sets an internally set property of an axes: the property, the call, and a figure built
    /// through the call.</summary>
    /// <param name="Property">The property's name.</param>
    /// <param name="Call">The public call that sets it, as a test row names it.</param>
    /// <param name="Build">Builds a figure through that call.</param>
    public readonly record struct InternalRoute(string Property, string Call, Func<Figure> Build);

    /// <summary>Every public call that sets an internally set property of an axes (<see cref="InternallySet"/>), each with a figure
    /// built through it, whose value of that property on every axes the JSON must give back. A share of a scale is carried by the
    /// key of the axes it is shared with (<c>ShareX(key)</c>, the call the builder offers); an axes that shares through
    /// <c>Figure.AddSubPlot</c> with a partner that has no key has no name the JSON can write the share by, and reads back
    /// unshared.</summary>
    public static IReadOnlyList<InternalRoute> InternallySetRoutes { get; } =
    [
        new("GridRows", "AddSubPlot(rows, cols, index)", CellsOfAGrid),
        new("GridCols", "AddSubPlot(rows, cols, index)", CellsOfAGrid),
        new("GridIndex", "AddSubPlot(rows, cols, index)", CellsOfAGrid),
        new("GridPosition", "AddSubPlot(GridPosition) in a grid specification", CellsOfAGridSpecification),
        new("GridPosition", "AddSubPlot(GridPosition) without a grid specification", CellWithoutAGridSpecification),
        new("ShareXWith", "ShareX(key)", SharedScales),
        new("ShareYWith", "ShareY(key)", SharedScales),
        new("InsetBounds", "AddInset(x, y, width, height)", AnInset),
        new("SecondaryXAxis", "WithSecondaryXAxis", ATopAxis),
        new("GridChange", "WithGrid", AGridChange),
    ];

    private static readonly double[] RouteX = [1, 2, 3], RouteY = [2, 4, 3];

    private static Figure CellsOfAGrid() => Plt.Create()
        .AddSubPlot(2, 3, 5, ax => ax.Plot(RouteX, RouteY))
        .AddSubPlot(2, 3, 1, ax => ax.Plot(RouteY, RouteX))
        .Build();

    private static Figure CellsOfAGridSpecification() => Plt.Create().WithGridSpec(2, 2)
        .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot(RouteX, RouteY))
        .AddSubPlot(GridPosition.Single(1, 1), ax => ax.Plot(RouteY, RouteX))
        .Build();

    private static Figure CellWithoutAGridSpecification() => Plt.Create()
        .AddSubPlot(GridPosition.Single(1, 1), ax => ax.Plot(RouteX, RouteY))
        .Build();

    private static Figure SharedScales() => Plt.Create()
        .AddSubPlot(1, 2, 1, ax => ax.Plot(RouteX, RouteY), key: "left")
        .AddSubPlot(1, 2, 2, ax => ax.Plot(RouteY, RouteX).ShareX("left").ShareY("left"))
        .Build();

    private static Figure AnInset() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot(RouteX, RouteY).AddInset(0.55, 0.1, 0.4, 0.3, inset => inset.Plot(RouteY, RouteX)))
        .Build();

    private static Figure ATopAxis() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot(RouteX, RouteY).WithSecondaryXAxis(top => top.PlotXSecondary(RouteY, RouteX)))
        .Build();

    private static Figure AGridChange() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot(RouteX, RouteY).WithGrid(grid => grid with { Visible = true, Alpha = 0.4 }))
        .Build();

    /// <summary>One row per route: the property and the call.</summary>
    public static TheoryData<string, string> InternallySetRouteRows { get; } =
        new(InternallySetRoutes.Select(route => (route.Property, route.Call)));

    /// <summary>The route that sets <paramref name="property"/> through <paramref name="call"/>.</summary>
    /// <exception cref="ArgumentException">No route has that property and call.</exception>
    public static InternalRoute Route(string property, string call) =>
        InternallySetRoutes.FirstOrDefault(route => route.Property == property && route.Call == call) is { Property: not null } found
            ? found
            : throw new ArgumentException($"No route sets '{property}' through '{call}'.", nameof(call));

    /// <summary>The value of the internally set <paramref name="property"/> on every axes of <paramref name="figure"/>, each
    /// subplot and then its insets, as a reader of the picture can compare it: an axes by its place among the subplots, an axis by
    /// being there (what it draws is the "SecondaryXAxis" part's), anything else as it is.</summary>
    /// <param name="figure">The figure.</param>
    /// <param name="property">The property's name, one of <see cref="InternallySet"/>.</param>
    public static IReadOnlyList<object?> InternallySetValues(Figure figure, string property)
    {
        var info = InternallySet.Single(p => p.Name == property);
        return [.. AllAxesOf(figure).Select(axes => Comparable(figure, info.GetValue(axes)))];
    }

    /// <summary>The value <paramref name="property"/> has on an axes nothing has placed or changed.</summary>
    /// <param name="property">The property's name, one of <see cref="InternallySet"/>.</param>
    public static object? InternallySetAtRest(string property) =>
        Comparable(new Figure(), InternallySet.Single(p => p.Name == property).GetValue(new Axes()));

    private static object? Comparable(Figure figure, object? value) => value switch
    {
        Axes other => $"subplot {IndexOf(figure.SubPlots, other)}",
        Axis => "an axis",
        _ => value,
    };

    private static int IndexOf(IReadOnlyList<Axes> subPlots, Axes axes)
    {
        for (int i = 0; i < subPlots.Count; i++)
        {
            if (ReferenceEquals(subPlots[i], axes))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Every axes of <paramref name="figure"/>: each subplot, then its insets, depth first.</summary>
    private static IEnumerable<Axes> AllAxesOf(Figure figure)
    {
        static IEnumerable<Axes> WithInsets(Axes axes) => axes.Insets.SelectMany(WithInsets).Prepend(axes);
        return figure.SubPlots.SelectMany(WithInsets);
    }

    /// <summary>The figure's settable properties the "Figure" part holds: all of them, less <see cref="FigureExemptions"/>.</summary>
    private static IReadOnlyList<string> FigureSettings { get; } =
        [.. typeof(Figure).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .Where(name => !FigureExemptions.ContainsKey(name))];

    /// <summary>The settable properties of <see cref="Axes"/> that the "AxesSettings" part does not hold, each with the
    /// reason. Every other settable property of an axes joins the contract on its own — the same mechanism that holds
    /// <see cref="FigureExemptions"/>/<see cref="FigureSettings"/>, so a scalar axes property added tomorrow is caught
    /// without a new line here.</summary>
    public static IReadOnlyDictionary<string, string> AxesExemptions { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [nameof(Axes.TitleStyle)] = "held by the \"Title\" part",
        [nameof(Axes.TitleLoc)] = "held by the \"Title\" part",
        [nameof(Axes.Legend)] = "a part of its own (Legend), held property by property",
        [nameof(Axes.Grid)] = "a part of its own (Grid), held property by property",
        [nameof(Axes.Spines)] = "a part of its own (Spine), held property by property",
        [nameof(Axes.Pane3D)] = "a part of its own (Pane3D), held property by property",
        [nameof(Axes.ColorBar)] = "a part of its own (ColorBar), held property by property",
        [nameof(Axes.ZoomIndicator)] = "a part of its own (ZoomIndicator, on the inset: only an inset draws it), held property by property",
        [nameof(Axes.LightSource)] = "only DirectionalLight is written today (roadmap: a light that is not directional); ILightSource has one implementation, so there is nothing else to reflect yet",
        [nameof(Axes.Projection)] = "a render-time cache rebuilt from Elevation/Azimuth/CameraDistance (Rotate3DEvent.cs nulls it to force a rebuild), not authored state",
    };

    /// <summary>The axes' settable properties the "AxesSettings" part holds: all of them, less <see cref="AxesExemptions"/>.</summary>
    private static IReadOnlyList<string> AxesSettings { get; } =
        [.. typeof(Axes).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .Where(name => !AxesExemptions.ContainsKey(name))];

    /// <summary>The settable properties of <see cref="Axis"/> the JSON does not carry yet, each with the reason: none today (the link
    /// was the last, and now travels as a record per kind of transform). No axis seat (a part of type <see cref="Axis"/>) holds a
    /// name listed here; every seat holds every other settable property of an axis (<see cref="AxisSettings"/>), so a property added
    /// tomorrow is caught without a new line here. A name stays only while it really is not carried:
    /// <c>AxesDrawnPropertiesRoundTripTests</c> goes red on one that comes back from JSON.</summary>
    public static IReadOnlyDictionary<string, string> AxisNotCarried { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The settable properties of an axis every axis seat holds: all of them, less <see cref="AxisNotCarried"/>.</summary>
    private static IReadOnlyList<string> AxisSettings { get; } =
        [.. typeof(Axis).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .Where(name => !AxisNotCarried.ContainsKey(name))];

    private static Axes TheAxes(Figure figure) => figure.SubPlots[0];

    private static Axes TheInset(Figure figure) => figure.SubPlots[0].Insets[0];

    /// <summary>Every part, on every seat the serializer writes and reads it through: each axis of the subplot, an inset's
    /// axis and legend (an inset is read back by the same path as a subplot), the figure's own colour bar, settings and
    /// theme.</summary>
    public static IReadOnlyList<DrawnPart> Parts { get; } =
    [
        new("Figure", figure => figure, "", typeof(Figure), FigureSettings),
        new("Theme", figure => figure, nameof(Figure.Theme), typeof(Theme)),
        new("XAxis", TheAxes, nameof(Axes.XAxis), typeof(Axis), AxisSettings),
        new("YAxis", TheAxes, nameof(Axes.YAxis), typeof(Axis), AxisSettings),
        new("ZAxis", TheAxes, nameof(Axes.ZAxis), typeof(Axis), AxisSettings),
        new("Pane3D", TheAxes, nameof(Axes.Pane3D), typeof(Pane3DConfig)),
        new("AxesSettings", TheAxes, "", typeof(Axes), AxesSettings),
        // Every seat a Y axis has on the wire, each through the container: the primary as left slot 0 ("yAxis"), right
        // slot 0 ("secondaryYAxis"), the one slot each side's frozen list holds (right slot 1 in "extraRightYAxes", left
        // slot 1 in "leftYAxes") and a slot past it (slot 2 of each side, in "rightYAxesOverflow" / "leftYAxesOverflow").
        new("LeftYAxisSlot0", figure => TheAxes(figure).YAxes.Left[0], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("LeftYAxisSlot1", figure => TheAxes(figure).YAxes.Left[1], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("LeftYAxisSlot2", figure => TheAxes(figure).YAxes.Left[2], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("RightYAxisSlot0", figure => TheAxes(figure).YAxes.Right[0], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("RightYAxisSlot1", figure => TheAxes(figure).YAxes.Right[1], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("RightYAxisSlot2", figure => TheAxes(figure).YAxes.Right[2], nameof(YAxisSlot.Axis), typeof(Axis), AxisSettings),
        new("YAxisContainer", TheAxes, nameof(Axes.YAxes), typeof(YAxisContainer)),
        new("SecondaryXAxis", TheAxes, nameof(Axes.SecondaryXAxis), typeof(Axis), AxisSettings),
        // A YAxisSlot has no settable property of its own (Axis and Side are construction-only) — this row contributes
        // zero DrawnPaths and exists only so ComposedChartRoundTripTests recognises the type every slot of the container
        // has. The data a slot wraps round-trips through the Y axis slot parts above (the Axis) and the series contract
        // (the slot's own series).
        new("YAxisSlot", figure => TheAxes(figure).YAxes.Right[0], "", typeof(YAxisSlot)),
        new("InsetXAxis", TheInset, nameof(Axes.XAxis), typeof(Axis), AxisSettings),
        new("Title", TheAxes, "", typeof(Axes), [nameof(Axes.TitleStyle), nameof(Axes.TitleLoc)]),
        new("Legend", TheAxes, nameof(Axes.Legend), typeof(Legend)),
        new("InsetLegend", TheInset, nameof(Axes.Legend), typeof(Legend)),
        // The zoom an inset indicates on its parent: a setting of the inset, created once a property of it is set.
        new("ZoomIndicator", TheInset, nameof(Axes.ZoomIndicator), typeof(ZoomIndicator)),
        new("ColorBar", TheAxes, nameof(Axes.ColorBar), typeof(ColorBar)),
        new("FigureColorBar", figure => figure, nameof(Figure.FigureColorBar), typeof(ColorBar)),
        // What the figure itself draws besides its subplots: its legend (settable, held property by property as the axes' is),
        // its texts and its images (get-only lists, held through their first item: the constructor's own values are held by
        // FigureListValues).
        new("FigureLegend", figure => figure, nameof(Figure.Legend), typeof(Legend)),
        new("FigureText", figure => figure.Texts[0], "", typeof(FigureText)),
        new("FigureImage", figure => figure.Images[0], "", typeof(FigureImage)),
        new("Grid", TheAxes, nameof(Axes.Grid), typeof(GridStyle)),
        new("Spine", TheAxes, $"{nameof(Axes.Spines)}.{nameof(SpinesConfig.Left)}", typeof(SpineConfig)),
        new("ReferenceLine", figure => TheAxes(figure).ReferenceLines[0], "", typeof(ReferenceLine)),
        new("Annotation", figure => TheAxes(figure).Annotations[0], "", typeof(Annotation)),
        new("Span", figure => TheAxes(figure).Spans[0], "", typeof(SpanRegion)),
        new("Trendline", figure => TheAxes(figure).Trendlines[0], "", typeof(Trendline)),
        new("HorizontalLevel", figure => TheAxes(figure).HorizontalLevels[0], "", typeof(HorizontalLevel)),
        new("Fibonacci", figure => TheAxes(figure).FibonacciRetracements[0], "", typeof(FibonacciRetracement)),
        new("Signal", figure => TheAxes(figure).Signals[0], "", typeof(SignalMarker)),
        // A break's ends and style are init properties of a record: set in place on the break the list holds.
        new("XBreak", figure => TheAxes(figure).XBreaks[0], "", typeof(AxisBreak)),
        new("YBreak", figure => TheAxes(figure).YBreaks[0], "", typeof(AxisBreak)),
        // One part per kind of patch, each holding the item of that kind in the axes' list (Build adds them in this order).
        new("Rectangle", figure => TheAxes(figure).Patches[0], "", typeof(Rectangle)),
        new("Polygon", figure => TheAxes(figure).Patches[1], "", typeof(Polygon)),
        new("Arrow", figure => TheAxes(figure).Patches[2], "", typeof(Arrow)),
        new("Circle", figure => TheAxes(figure).Patches[3], "", typeof(Circle)),
        new("Ellipse", figure => TheAxes(figure).Patches[4], "", typeof(Ellipse)),
        new("Wedge", figure => TheAxes(figure).Patches[5], "", typeof(Wedge)),
        new("Arc", figure => TheAxes(figure).Patches[6], "", typeof(Arc)),
    ];

    /// <summary>The part called <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">No part has that name.</exception>
    public static DrawnPart Part(string name) =>
        Parts.FirstOrDefault(p => p.Name == name) is { Name: not null } part
            ? part
            : throw new ArgumentException($"No drawn part is called '{name}'.", nameof(name));

    /// <summary>The property paths of <paramref name="part"/> the JSON must carry, relative to the part.</summary>
    public static IReadOnlyList<string> DrawnPaths(DrawnPart part) => [.. LeafPaths(part.Type, part.Only)];

    private static IEnumerable<string> LeafPaths(Type type, IReadOnlyList<string>? only)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Where(p => only is null || only.Contains(p.Name))
            .OrderBy(p => p.Name, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (NestedRecords.Contains(valueType))
            {
                foreach (var inner in LeafPaths(valueType, null))
                {
                    yield return $"{property.Name}.{inner}";
                }
            }
            else
            {
                yield return property.Name;
            }
        }
    }

    /// <summary>One row per part and drawn property path.</summary>
    public static TheoryData<string, string> Rows { get; } = RowsOf(Parts);

    /// <summary>One row per key of <see cref="FigureListValues"/> for a figure built by <see cref="Build"/>. Declared after
    /// <see cref="Parts"/>, which <see cref="Build"/> reads.</summary>
    public static TheoryData<string> FigureListValueKeys { get; } = new(FigureListValues(Build(everyPropertySet: true)).Keys);

    /// <summary>One row per key of <see cref="AxesListValues"/> for a figure built by <see cref="Build"/>. Declared after
    /// <see cref="Parts"/>, which <see cref="Build"/> reads.</summary>
    public static TheoryData<string> AxesListValueKeys { get; } = new(AxesListValues(Build(everyPropertySet: true)).Keys);

    /// <summary>The rows of <paramref name="parts"/>: one per part and drawn property path.</summary>
    public static TheoryData<string, string> RowsOf(IEnumerable<DrawnPart> parts)
    {
        var rows = new TheoryData<string, string>();
        foreach (var part in parts)
        {
            foreach (var path in DrawnPaths(part))
            {
                rows.Add(part.Name, path);
            }
        }

        return rows;
    }

    /// <summary>A figure with one subplot that holds every part: a labelled line, three right Y axes and two left Y axes
    /// past the primary (so every wire seat of a Y axis is taken: slot 0, the frozen list's one slot and a slot past it, on
    /// both sides) and a secondary X axis, with a line each, a reference line, an annotation, a span, a trendline, a level,
    /// a Fibonacci retracement, a signal, a break of each axis, a patch of each kind and an inset with a line, and on the figure itself one text and one image (the figure's legend
    /// and colour bar are parts that exist only once set). At rest, or with every drawn property of every part set to a
    /// value that is not its default.</summary>
    public static Figure Build(bool everyPropertySet)
    {
        double[] x = [1, 2, 3], y = [2, 4, 3];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(x, y, s => s.Label = "values")
            .WithAddRightYAxis(r0 => r0.Plot(x, x))
            .WithAddRightYAxis(r1 => r1.Plot(x, y))
            .WithAddRightYAxis(r2 => r2.Plot(y, x))
            .WithAddLeftYAxis(l1 => l1.Plot(x, x))
            .WithAddLeftYAxis(l2 => l2.Plot(x, y))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary(x, x))).Build();
        var axes = figure.SubPlots[0];
        axes.AxHLine(2.5);
        axes.Annotate("note", 2, 3);
        axes.AxHSpan(1, 2);
        axes.AddTrendline(1, 2, 3, 4);
        axes.AddLevel(3);
        axes.AddFibonacci(4, 2);
        // A sell signal and a break of each axis whose style is not the default, so a direction or a style the JSON drops reads
        // back as another.
        axes.AddSignal(2, 3.5, SignalDirection.Sell);
        axes.AddXBreak(1.25, 1.5, BreakStyle.Straight);
        axes.AddYBreak(2.25, 2.5, BreakStyle.None);

        // A patch of each kind, in the order of the parts above, inside the range the line frames.
        axes.AddPatch(new Rectangle(new(1.1, 2.1), 0.4, 0.3));
        axes.AddPatch(new Polygon([new(1.5, 2.2), new(2, 2.2), new(1.8, 2.6)]));
        axes.AddPatch(new Arrow(new(1.2, 3.5), 0.5, 0.2));
        axes.AddPatch(new Circle(new(2.5, 3.5), 0.2));
        axes.AddPatch(new Ellipse(new(2.6, 2.4), 0.5, 0.25));
        axes.AddPatch(new Wedge(new(1.5, 3.2), 0.3, 20, 100));
        axes.AddPatch(new Arc(new(2.2, 3), 0.6, 0.3));
        axes.AddInset(0.6, 0.6, 0.3, 0.3).Plot(x, y);

        // What the figure carries itself: a text and an image (with a missing cell, which the JSON writes as "NaN").
        figure.AddText("note", 0.25, 0.75);
        figure.AddImage(new double[,] { { 1, double.NaN, 3 }, { 4, 5, 6 } }, 12, 34);

        if (everyPropertySet)
        {
            foreach (var part in Parts)
            {
                int k = 0;
                foreach (var path in DrawnPaths(part))
                {
                    Set(figure, part.Name, path, k++);
                }
            }
        }

        return figure;
    }

    /// <summary>Sets the property at <paramref name="path"/> of the part called <paramref name="partName"/> to a value
    /// that is not its default (the <paramref name="k"/>-th such value), leaving every other property as it is.</summary>
    public static void Set(Figure figure, string partName, string path, int k)
    {
        var part = Part(partName);
        SetPath(part.Holder(figure), Join(part.Path, path).Split('.'), 0,
            (property, current) => DrawnSeriesFixtures.NotTheDefault(property, current, k));
    }

    /// <summary>The value of the property at <paramref name="path"/> of the part called <paramref name="partName"/>;
    /// <see langword="null"/> when a record on the way is not there (a colour bar never asked for).</summary>
    public static object? ValueOf(Figure figure, string partName, string path)
    {
        var part = Part(partName);
        object? value = part.Holder(figure);
        foreach (var name in Join(part.Path, path).Split('.'))
        {
            if (value is null)
            {
                return null;
            }

            value = (value.GetType().GetProperty(name) ?? throw new ArgumentException($"{value.GetType().Name} has no property '{name}'.", nameof(path)))
                .GetValue(value);
        }

        return value;
    }

    private static string Join(string prefix, string path) => prefix.Length == 0 ? path : $"{prefix}.{path}";

    /// <summary>Sets the property at the end of <paramref name="names"/>. A record on the way is copied, changed and put
    /// back (it has value semantics, and an init-only property is set on the copy); a struct on the way is changed in its
    /// box and put back; a theme on the way is replaced by a copy of its own first (the built-in themes are shared: every
    /// new figure holds the same instance, so changing it in place would change every other figure); any other class on
    /// the way is changed in place; a record that is not there yet is created.</summary>
    private static void SetPath(object owner, string[] names, int at, Func<PropertyInfo, object?, object> valueFor)
    {
        var property = owner.GetType().GetProperty(names[at])
            ?? throw new ArgumentException($"{owner.GetType().Name} has no property '{names[at]}'.", nameof(names));
        object? current = property.GetValue(owner);
        if (at == names.Length - 1)
        {
            property.SetValue(owner, valueFor(property, current));
            return;
        }

        var childType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        object child = current ?? Activator.CreateInstance(childType)!;
        var clone = child.GetType().GetMethod("<Clone>$");
        if (clone is not null)
        {
            child = clone.Invoke(child, null)!;
        }

        bool copied = false;
        if (child is Theme shared)
        {
            child = Theme.CreateFrom(shared).WithName(shared.Name).Build();
            copied = true;
        }

        SetPath(child, names, at + 1, valueFor);
        if (clone is not null || current is null || copied || childType.IsValueType)
        {
            property.SetValue(owner, child);
        }
    }
}
