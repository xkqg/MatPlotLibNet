// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Something a figure draws that starts from values of its own when it is told nothing: a series type, a part of an axes or
/// of the figure, or a settings record a part is made of.</summary>
/// <param name="Name">The subject's name in a test row: the series type, the part or the record type.</param>
/// <param name="Type">The type whose settings the subject holds.</param>
/// <param name="AtRest">Makes the subject as it starts: a fresh instance each call.</param>
/// <param name="Only">The settings of <paramref name="Type"/> that belong to the subject, or <see langword="null"/> for all.</param>
public readonly record struct RestingSubject(string Name, Type Type, Func<object> AtRest, IReadOnlyList<string>? Only = null);

/// <summary>Every subject that starts from resting values, and the values it starts from: the one roster the resting-value corpus
/// (<see cref="RestingValueCorpusTests"/>) pins and the resting-constant contract (<see cref="RestingValueContractTests"/>) reads.
/// A series type is the roster's instance at rest (<see cref="DrawnSeriesFixtures.Instance"/>); a part is what the figure at rest holds
/// there (<see cref="DrawnAxesFixtures.Build"/>), and where the figure holds nothing yet (a colour bar never asked for), the record the
/// part is made from when it is first set; a settings record is its parameterless constructor's. The lists are REFLECTED: a series
/// type, a part or a record added tomorrow joins on its own.</summary>
public static class RestingSubjects
{
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>The parts whose values another corpus pins, each with that corpus: they are no subject here.</summary>
    public static IReadOnlyDictionary<string, string> PinnedElsewhere { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Theme"] = "the theme a figure wears is pinned value by value by ThemeCorpusTests",
    };

    /// <summary>Every subject: the series types, then the parts, then the settings records.</summary>
    public static IReadOnlyList<RestingSubject> All { get; } =
    [
        .. DrawnSeriesFixtures.DrawnTypes.Select(type => new RestingSubject(type.Name, type, () => DrawnSeriesFixtures.Instance(type.Name))),
        .. DrawnAxesFixtures.Parts.Where(part => !PinnedElsewhere.ContainsKey(part.Name) && Settings(part.Type, part.Only).Count > 0)
            .Select(part => new RestingSubject(part.Name, part.Type, () => PartAtRest(part), part.Only)),
        .. RecordsOfTheParts().Concat(RecordsACallTakes).Select(type => new RestingSubject(type.Name, type, () => Activator.CreateInstance(type)!)),
    ];

    /// <summary>The settings records a call of the library takes besides the ones the parts are made of: the options a figure is
    /// saved with.</summary>
    private static IReadOnlyList<Type> RecordsACallTakes => [typeof(SaveOptions)];

    /// <summary>One row per subject: its name.</summary>
    public static TheoryData<string> Names { get; } = new(All.Select(subject => subject.Name));

    /// <summary>The subject called <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">No subject has that name.</exception>
    public static RestingSubject Subject(string name) =>
        All.FirstOrDefault(subject => subject.Name == name) is { Name: not null } found
            ? found
            : throw new ArgumentException($"No resting subject is called '{name}'.", nameof(name));

    /// <summary>The public settable properties of <paramref name="type"/> a subject holds (those <paramref name="only"/> names, when it
    /// names any), in ordinal order: code (a delegate) is no setting a value can show.</summary>
    public static IReadOnlyList<PropertyInfo> Settings(Type type, IReadOnlyList<string>? only = null) =>
        [.. type.GetProperties(Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Where(p => !typeof(Delegate).IsAssignableFrom(p.PropertyType))
            .Where(p => only is null || only.Contains(p.Name))
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>Every setting of <paramref name="subject"/> as it starts, shown as text (<see cref="Shown"/>), by the setting's name.</summary>
    public static IReadOnlyDictionary<string, string> ValuesOf(RestingSubject subject)
    {
        object atRest = subject.AtRest();
        return Settings(subject.Type, subject.Only).ToDictionary(p => p.Name, p => Shown(p.GetValue(atRest)), StringComparer.Ordinal);
    }

    /// <summary>What the figure at rest holds at <paramref name="part"/>: a record on the way that is not there yet is made by its
    /// parameterless constructor, as the record a part is made from when it is first set; and a setting the call that made the part
    /// was given as an optional argument (the style the figure's break was drawn in) is put back at that argument's default, which is
    /// the start the call takes when it is told nothing beyond its data.</summary>
    private static object PartAtRest(DrawnPart part)
    {
        object value = part.Holder(DrawnAxesFixtures.Build(everyPropertySet: false));
        foreach (string name in part.Path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var property = value.GetType().GetProperty(name)!;
            value = property.GetValue(value) ?? Activator.CreateInstance(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType)!;
        }

        var optional = value.GetType().GetConstructors().SelectMany(constructor => constructor.GetParameters())
            .Where(parameter => parameter.HasDefaultValue)
            .Select(parameter => (parameter, property: Settings(value.GetType()).FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))))
            .Where(pair => pair.property is not null)
            .ToList();
        if (optional.Count > 0 && value.GetType().GetMethod("<Clone>$") is { } clone)
        {
            value = clone.Invoke(value, null)!;
            foreach (var (parameter, property) in optional)
            {
                property!.SetValue(value, parameter.DefaultValue);
            }
        }

        return value;
    }

    /// <summary>The settings records the parts are made of: every record of the library with a parameterless constructor that a setting
    /// of a part's type holds, a setting of such a record included, that is no part's type itself.</summary>
    private static IReadOnlyList<Type> RecordsOfTheParts()
    {
        var partTypes = DrawnAxesFixtures.Parts.Select(part => part.Type).ToHashSet();
        var found = new SortedDictionary<string, Type>(StringComparer.Ordinal);
        var pending = new Queue<Type>(partTypes);
        var seen = new HashSet<Type>();
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            if (!seen.Add(type))
            {
                continue;
            }

            foreach (var property in Settings(type))
            {
                var held = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (IsRecord(held) && held.Assembly == typeof(Figure).Assembly && held.GetConstructor(Type.EmptyTypes) is not null)
                {
                    if (!partTypes.Contains(held))
                    {
                        found[held.Name] = held;
                    }

                    pending.Enqueue(held);
                }
            }
        }

        return [.. found.Values];
    }

    /// <summary>Whether <paramref name="type"/> is a record: a class or struct the compiler gave its members' printout.</summary>
    private static bool IsRecord(Type type) =>
        type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(System.Text.StringBuilder)]) is not null;

    /// <summary>A value as the corpus writes it, the same on every machine and in every culture: a text between single quotes (so
    /// <c>null</c> and the text "null" stay apart), a number in the invariant culture, a
    /// colour as its hex and its alpha, a record as its settings, a colour map by its name, a tick locator, a tick formatter, a
    /// normaliser or a transform by its kind.</summary>
    /// <exception cref="InvalidOperationException">The value is of a kind the corpus does not know how to write.</exception>
    public static string Shown(object? value) => value switch
    {
        null => "null",
        string text => $"'{text}'",
        bool flag => flag ? "true" : "false",
        Enum member => member.ToString(),
        Color colour => colour.ToHex() + colour.A.ToString("X2", CultureInfo.InvariantCulture),
        IColorMap map => $"colour map '{map.Name}'",
        ITickLocator or ITickFormatter or INormalizer or AxisTransform => value.GetType().Name,
        TreeNode node => $"tree '{node.Label}' [{string.Join(", ", node.Children.Select(Shown))}]",
        PropCycler cycler => "cycle [" + string.Join(", ", Enumerable.Range(0, cycler.Length).Select(i => Shown(cycler[i]))) + "]",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(Shown)) + "]",
        _ when IsRecord(value.GetType()) =>
            "{" + string.Join(", ", Settings(value.GetType()).Select(p => $"{p.Name}: {Shown(p.GetValue(value))}")) + "}",
        _ => throw new InvalidOperationException($"The resting corpus cannot write a {value.GetType().Name}: teach {nameof(Shown)} its kind."),
    };
}
