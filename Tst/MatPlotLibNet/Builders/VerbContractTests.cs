// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What every builder verb that takes a callback owes its caller, held once over <see cref="VerbCatalogue"/>: the callback
/// runs on what the verb added, after the verb, so what it sets wins; no callback draws what an empty one draws (or is refused by
/// name where the callback is the verb's point); the verb returns its builder for the chain; a figure verb draws what the axes verb
/// of the same name draws on the figure's one subplot; an extension verb refuses a missing builder by name. Every overload that
/// takes a callback is a row: one the catalogue misses turns the guard red.</summary>
public class VerbContractTests
{
    private static readonly Color ProbeColour = Color.FromHex("#123456");

    /// <summary>A setting the callback gave one object, read back after the figure is built.</summary>
    private readonly record struct Probe(object Target, PropertyInfo Property, object Value)
    {
        public bool Holds => Equals(Property.GetValue(Target), Value);
    }

    /// <summary>What one draw of a row left: the figure, what the verb returned, and the builder it was called on.</summary>
    private readonly record struct Drawn(Figure Figure, object? Returned, object? Builder);

    /// <summary>The axes a verb is drawing on, while it draws: the callback reads it to see whether its series is there yet.</summary>
    private sealed class Onto
    {
        public Axes? Axes { get; set; }
    }

    private static Drawn Draw(VerbRow row, Delegate? configure, Onto? onto = null)
    {
        if (row.Receiver == typeof(FigureBuilder))
        {
            var builder = Plt.Create();
            var returned = row.Invoke(builder, configure);
            return new Drawn(builder.Build(), returned, builder);
        }

        if (row.Receiver == typeof(Axes))
        {
            var model = new Figure();
            var subplot = model.AddSubPlot();
            onto?.Axes = subplot;
            return new Drawn(model, row.Invoke(subplot, configure), subplot);
        }

        object? result = null, axes = null;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            axes = ax;
            onto?.Axes = ax.Axes;
            result = row.Invoke(ax, configure);
        }).Build();
        return new Drawn(figure, result, axes);
    }

    /// <summary>A callback of the row's type that records what it is handed, notes each series it is handed before the series is on
    /// the axes the verb draws on, and gives each object (each series of a parts record) a probe label, colour, line width, line
    /// style and alpha wherever it can take them.</summary>
    private static Delegate Recording(VerbRow row, List<object> seen, List<Probe> probes, Onto? onto = null, List<object>? early = null) =>
        (Delegate)typeof(VerbContractTests).GetMethod(nameof(RecordingOf), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(row.Configured).Invoke(null, [seen, probes, onto, early])!;

    private static Action<T> RecordingOf<T>(List<object> seen, List<Probe> probes, Onto? onto, List<object>? early) => item =>
    {
        seen.Add(item!);
        foreach (var part in PartsOf(item!))
        {
            if (part is ISeries series && onto?.Axes is { } axes && !axes.AllSeries.Contains(series)) early?.Add(part);
            probes.AddRange(Probed(part, seen.Count));
        }
    };

    private static Delegate Empty(VerbRow row) =>
        (Delegate)typeof(VerbContractTests).GetMethod(nameof(EmptyOf), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(row.Configured).Invoke(null, null)!;

    private static Action<T> EmptyOf<T>() => _ => { };

    /// <summary>The object itself, or the series a parts record holds.</summary>
    private static IEnumerable<object> PartsOf(object item) => item.GetType().IsValueType
        ? item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.GetValue(item)).OfType<ChartSeries>()
        : [item];

    private static IEnumerable<Probe> Probed(object target, int run)
    {
        foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.SetMethod is { IsPublic: true }))
        {
            object? value = property.Name switch
            {
                "Label" when property.PropertyType == typeof(string) => $"probe {run}",
                "Color" when property.PropertyType == typeof(Color) || property.PropertyType == typeof(Color?) => ProbeColour,
                "LineWidth" when property.PropertyType == typeof(double) || property.PropertyType == typeof(double?) => 7.25,
                "LineStyle" when property.PropertyType == typeof(LineStyle) || property.PropertyType == typeof(LineStyle?) => LineStyle.DashDot,
                "Alpha" when property.PropertyType == typeof(double) || property.PropertyType == typeof(double?) => 0.375,
                _ => null,
            };
            if (value is null) continue;
            property.SetValue(target, value);
            yield return new Probe(target, property, value);
        }
    }

    private static IEnumerable<object> HeldBy(VerbRow row, Figure figure) =>
        row.Holder?.Invoke(figure) ?? figure.SubPlots.SelectMany(axes => axes.AllSeries);

    /// <summary>Every verb of the catalogue is one item of the case; a verb that fails is named with what it broke.</summary>
    [Fact]
    public void TheCallback_RunsOnWhatTheVerbAdded_AfterTheVerb_SoWhatItSetsWins() =>
        Assert.All(VerbCatalogue.Rows.Select(row => row.Name), TheCallbackRunsOnWhatTheVerbAdded);

    [Fact]
    public void ACallWithoutACallback_DrawsWhatAnEmptyCallbackDraws_OrIsRefusedByName() =>
        Assert.All(VerbCatalogue.Rows.Select(row => row.Name), ACallWithoutACallbackDrawsWhatAnEmptyOneDraws);

    private static void TheCallbackRunsOnWhatTheVerbAdded(string verb)
    {
        var row = VerbCatalogue.Named(verb);
        var seen = new List<object>();
        var probes = new List<Probe>();
        var onto = new Onto();
        var early = new List<object>();

        var drawn = Draw(row, Recording(row, seen, probes, onto, early), onto);

        Assert.True(seen.Count == row.Runs, $"{verb}: the callback ran {seen.Count} times, not {row.Runs}.");
        if (drawn.Returned is ISeries series) Assert.Same(Assert.Single(seen), series);
        else Assert.Same(drawn.Builder, drawn.Returned);
        if (row.Series is int count) Assert.Equal(count, drawn.Figure.SubPlots.Sum(axes => axes.AllSeries.Count()));
        if (row.Holder is not null || typeof(ISeries).IsAssignableFrom(row.Configured) || row.Configured.IsValueType)
        {
            var held = HeldBy(row, drawn.Figure).ToList();
            var places = seen.SelectMany(PartsOf).Select(part => held.FindIndex(item => ReferenceEquals(item, part))).ToList();
            Assert.True(!places.Contains(-1), $"{verb}: the callback was handed a part the figure does not hold.");
            Assert.True(row.Configured.IsValueType || places.SequenceEqual(places.Order()), $"{verb}: the callback ran on its units out of the order the figure holds them.");
        }

        // The axes model's own callback verbs run the callback before the series is on the axes (register R73), the builders' after.
        Assert.True(row.Receiver == typeof(Axes) || early.Count == 0, $"{verb}: the callback was handed a series before the series was on the axes.");
        var lost = probes.Where(probe => !probe.Holds).Select(probe => $"{probe.Target.GetType().Name}.{probe.Property.Name}").ToList();
        Assert.True(lost.Count == 0, $"{verb}: the verb overwrote what the callback set: {string.Join(", ", lost)}.");
    }

    private static void ACallWithoutACallbackDrawsWhatAnEmptyOneDraws(string verb)
    {
        var row = VerbCatalogue.Named(verb);

        if (row.NoCallback == NoCallback.Refused)
        {
            ArgumentNullException? refused = null;
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => refused = Assert.Throws<ArgumentNullException>(() => row.Invoke(ax, null))).Build();

            Assert.Equal(row.Method.GetParameters().Single(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Action<>)).Name, refused!.ParamName);
            Assert.Equal(Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build().ToJson(), figure.ToJson());
            return;
        }

        if (row.NoCallback == NoCallback.Unguarded)
        {
            Assert.Throws<NullReferenceException>(() => Draw(row, null));
            return;
        }

        Assert.Equal(Draw(row, Empty(row)).Figure.ToJson(), Draw(row, null).Figure.ToJson());
    }

    [Theory]
    [MemberData(nameof(VerbCatalogue.Forwards), MemberType = typeof(VerbCatalogue))]
    public void AFigureVerb_DrawsWhatTheAxesVerbOfTheSameNameDrawsOnTheOneSubplot(string verb)
    {
        var onTheFigure = VerbCatalogue.Named(verb);
        var onTheAxes = VerbCatalogue.Rows.Single(row => row.Receiver == typeof(AxesBuilder) && row.OnTheFigure()?.Name == verb);

        string expected = Draw(onTheAxes, Recording(onTheAxes, [], [])).Figure.ToJson();
        string actual = Draw(onTheFigure, Recording(onTheFigure, [], [])).Figure.ToJson();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(VerbCatalogue.Extensions), MemberType = typeof(VerbCatalogue))]
    public void AnExtensionVerbOfTheAxesBuilder_RefusesAMissingBuilder_ByItsName(string verb)
    {
        var row = VerbCatalogue.Named(verb);

        var refused = Assert.Throws<ArgumentNullException>(() => row.Invoke(null!, null));

        Assert.Equal(row.Method.GetParameters()[0].Name, refused.ParamName);
    }

    /// <summary>On an axes that slots bars (<c>UseBarSlotX</c>, a panel under a candlestick chart) an indicator verb places its
    /// indicator on the centres of the slots before the callback sees it, so an offset the callback sets wins.</summary>
    [Theory]
    [MemberData(nameof(VerbCatalogue.Indicators), MemberType = typeof(VerbCatalogue))]
    public void AnIndicatorVerb_OnAnAxesThatSlotsBars_HandsItsCallbackTheIndicatorOnTheSlotCentres(string verb)
    {
        var row = VerbCatalogue.Named(verb);
        var seen = new List<object>();

        Plt.Create().AddSubPlot(1, 1, 1, ax => row.Invoke(ax.UseBarSlotX(), Recording(row, seen, []))).Build();

        Assert.Equal(CategorySlot.CentreOffset, Assert.IsAssignableFrom<global::MatPlotLibNet.Indicators.Indicator>(Assert.Single(seen)).Offset);
    }

    /// <summary>What a composed chart hands its callback, when it is several series at once, is a parts record: a read-only record
    /// struct the library builds and the caller only reads.</summary>
    [Fact]
    public void ThePartsAVerbHandsItsCallback_AreReadOnlyRecordStructs_TheCallerCannotBuild()
    {
        var parts = VerbCatalogue.Rows.Select(row => row.Configured).Where(type => type.IsValueType).Distinct().ToList();

        Assert.NotEmpty(parts);
        Assert.All(parts, type =>
        {
            Assert.True(type.IsPublic && type.IsDefined(typeof(IsReadOnlyAttribute)), $"{type.Name} is not a public read-only struct.");
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            Assert.NotEmpty(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
        });
    }

    /// <summary>Every public verb that takes a callback, at every layer that carries verbs (the axes model, the builders of the
    /// fluent chain: the figure, the axes, the slot of an added axis), whether the class declares it or an extension block adds
    /// it: the calls a user writes. The builders that compose whole figures from panels (a dashboard, small multiples, a mosaic,
    /// a template) hand their callbacks a panel's builder, which is a row here through its own verbs.</summary>
    private static IEnumerable<MethodInfo> VerbsWithACallback()
    {
        static bool TakesACallback(MethodInfo method) => method.GetParameters()
            .Any(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Action<>));

        Type[] layers = [typeof(Axes), typeof(AxesBuilder), typeof(FigureBuilder), typeof(YAxisSlotBuilder), typeof(SecondaryXAxisBuilder), typeof(XAxisSlotBuilder)];
        var declared = layers.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        var extensions = typeof(AxesBuilder).Assembly.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false) && layers.Contains(method.GetParameters()[0].ParameterType));
        return declared.Concat(extensions).Where(TakesACallback);
    }

    private static string NameOf(MethodInfo method) =>
        VerbCatalogue.NameOf(method.IsStatic ? method.GetParameters()[0].ParameterType : method.DeclaringType!, method);

    [Fact]
    public void EveryVerbThatTakesACallback_IsOneRowOfTheCatalogue()
    {
        var verbs = VerbsWithACallback().Select(NameOf).ToHashSet(StringComparer.Ordinal);
        var rows = VerbCatalogue.Rows.Select(row => row.Name).ToList();

        var missing = verbs.Except(rows, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var stale = rows.Except(verbs, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var twice = rows.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        Assert.True(missing.Count == 0 && stale.Count == 0 && twice.Count == 0,
            $"Every verb that takes a callback is one row of VerbCatalogue.\nNo row:\n{string.Join('\n', missing)}\nA row of no such verb:\n{string.Join('\n', stale)}\nTwo rows:\n{string.Join('\n', twice)}");
    }
}
