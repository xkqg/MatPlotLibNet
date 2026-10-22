// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
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
    };

    /// <summary>The figure's settable properties the "Figure" part holds: all of them, less <see cref="FigureExemptions"/>.</summary>
    private static IReadOnlyList<string> FigureSettings { get; } =
        [.. typeof(Figure).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .Where(name => !FigureExemptions.ContainsKey(name))];

    private static Axes TheAxes(Figure figure) => figure.SubPlots[0];

    private static Axes TheInset(Figure figure) => figure.SubPlots[0].Insets[0];

    /// <summary>Every part, on every seat the serializer writes and reads it through: each axis of the subplot, an inset's
    /// axis and legend (an inset is read back by the same path as a subplot), the figure's own colour bar, settings and
    /// theme.</summary>
    public static IReadOnlyList<DrawnPart> Parts { get; } =
    [
        new("Figure", figure => figure, "", typeof(Figure), FigureSettings),
        new("Theme", figure => figure, nameof(Figure.Theme), typeof(Theme)),
        new("XAxis", TheAxes, nameof(Axes.XAxis), typeof(Axis)),
        new("YAxis", TheAxes, nameof(Axes.YAxis), typeof(Axis)),
        new("SecondaryYAxis", TheAxes, nameof(Axes.SecondaryYAxis), typeof(Axis)),
        new("SecondaryXAxis", TheAxes, nameof(Axes.SecondaryXAxis), typeof(Axis)),
        new("InsetXAxis", TheInset, nameof(Axes.XAxis), typeof(Axis)),
        new("Title", TheAxes, "", typeof(Axes), [nameof(Axes.TitleStyle), nameof(Axes.TitleLoc)]),
        new("Legend", TheAxes, nameof(Axes.Legend), typeof(Legend)),
        new("InsetLegend", TheInset, nameof(Axes.Legend), typeof(Legend)),
        new("ColorBar", TheAxes, nameof(Axes.ColorBar), typeof(ColorBar)),
        new("FigureColorBar", figure => figure, nameof(Figure.FigureColorBar), typeof(ColorBar)),
        new("Grid", TheAxes, nameof(Axes.Grid), typeof(GridStyle)),
        new("Spine", TheAxes, $"{nameof(Axes.Spines)}.{nameof(SpinesConfig.Left)}", typeof(SpineConfig)),
        new("ReferenceLine", figure => TheAxes(figure).ReferenceLines[0], "", typeof(ReferenceLine)),
        new("Annotation", figure => TheAxes(figure).Annotations[0], "", typeof(Annotation)),
        new("Span", figure => TheAxes(figure).Spans[0], "", typeof(SpanRegion)),
        new("Trendline", figure => TheAxes(figure).Trendlines[0], "", typeof(Trendline)),
        new("HorizontalLevel", figure => TheAxes(figure).HorizontalLevels[0], "", typeof(HorizontalLevel)),
        new("Fibonacci", figure => TheAxes(figure).FibonacciRetracements[0], "", typeof(FibonacciRetracement)),
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

    /// <summary>A figure with one subplot that holds every part: a labelled line, a secondary Y and a secondary X axis
    /// with a line each, a reference line, an annotation, a span, a trendline, a level, a Fibonacci retracement and an
    /// inset with a line. At rest, or with every drawn property of every part set to a value that is not its default.</summary>
    public static Figure Build(bool everyPropertySet)
    {
        double[] x = [1, 2, 3], y = [2, 4, 3];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(x, y, s => s.Label = "values")
            .WithSecondaryYAxis(y2 => y2.Plot(x, x))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary(x, x))).Build();
        var axes = figure.SubPlots[0];
        axes.AxHLine(2.5);
        axes.Annotate("note", 2, 3);
        axes.AxHSpan(1, 2);
        axes.AddTrendline(1, 2, 3, 4);
        axes.AddLevel(3);
        axes.AddFibonacci(4, 2);
        axes.AddInset(0.6, 0.6, 0.3, 0.3).Plot(x, y);

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
