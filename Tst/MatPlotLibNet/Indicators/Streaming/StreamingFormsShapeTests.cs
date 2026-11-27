// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>What every streaming form shares, shown on all of them at once: it can be made with its defaults, it has a warm-up of at
/// least one input, it names its lines, it hands the capacity it is given to every line, and it has not seen an input yet. The
/// values are the business of <see cref="StreamingEqualsBatchTests"/>; this holds the shape.</summary>
public sealed class StreamingFormsShapeTests
{
    private static IEnumerable<Type> Forms() => typeof(StreamingSma).Assembly.ConcretePublicSubtypesOf(typeof(StreamingIndicator<>));

    private static bool IsWindowed(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(WindowedStreamingIndicator<>)) return true;
        }

        return false;
    }

    /// <summary>The form made with the defaults it declares and a capacity of 7; a required number (<c>Roc.lookback</c>, the period of
    /// the older forms) is 5. A form that has a second constructor naming one more setting (the stochastic's smoothing) is made with the
    /// one that asks for the fewest numbers.</summary>
    private static IStreamingIndicator Make(Type form)
    {
        var ctor = form.GetConstructors().OrderBy(c => c.GetParameters().Count(p => !p.HasDefaultValue && p.Name != "capacity")).First();
        var args = ctor.GetParameters()
            .Select(p => p.Name == "capacity" ? 7
                : p.ParameterType == typeof(TransferEntropyOptions) ? new TransferEntropyOptions(Window: 30)
                : p.HasDefaultValue ? p.DefaultValue : (object)5)
            .ToArray();
        return (IStreamingIndicator)ctor.Invoke(args);
    }

    /// <summary>Every indicator the map streams, as a theory takes it.</summary>
    public static TheoryData<string> Names() => new(StreamingMap.All.Where(c => c.IsStreamed).Select(c => c.Name));

    /// <summary>The form of an indicator: the one that carries its name, or the start of it (the form of <c>BollingerBands</c> is
    /// <c>StreamingBollinger</c>).</summary>
    private static Type? FormOf(string indicator) =>
        Forms().SingleOrDefault(t => t.Name == "Streaming" + indicator)
        ?? Forms().SingleOrDefault(t => indicator.StartsWith(t.Name["Streaming".Length..], StringComparison.Ordinal));

    /// <summary>The other way: a form is an indicator the map streams, so a form cannot be added without a line saying how it streams.</summary>
    [Fact]
    public void EveryForm_IsOfAnIndicatorTheMapStreams()
    {
        var streamed = StreamingMap.All.Where(c => c.IsStreamed).Select(c => c.Name).ToList();

        foreach (var form in Forms())
        {
            string bare = form.Name["Streaming".Length..];
            Assert.Contains(streamed, name => name == bare || name.StartsWith(bare, StringComparison.Ordinal));
        }
    }

    /// <summary>Every indicator the map streams has a form, made with the defaults it declares: it warms up over one input or more,
    /// hands the capacity it is given to every line, has seen no input yet, is called what its batch indicator made with its defaults
    /// is called when that indicator draws a line under its name, and names among its lines every line that indicator draws.</summary>
    [Theory]
    [MemberData(nameof(Names))]
    public void AStreamingForm_CanBeMadeWithItsDefaults_AndHasAShape(string name)
    {
        var type = FormOf(name);
        Assert.True(type is not null, $"{name} is streamed and has no form");
        var form = Make(type);

        Assert.True(form.WarmupPeriod >= 1, $"{name} warms up in {form.WarmupPeriod} inputs");
        Assert.NotEmpty(form.OutputSeries);
        Assert.All(form.OutputSeries, line => Assert.False(string.IsNullOrWhiteSpace(line.Label), $"{name} has an unnamed line"));
        Assert.All(form.OutputSeries, line => Assert.Equal(7, line.Capacity));
        Assert.Equal(0, form.ProcessedCount);
        Assert.False(form.IsWarmedUp);
        Assert.True(double.IsNaN(form.GetLatest()));
        if (IsWindowed(type)) Assert.True((int)type.GetProperty(nameof(WindowedStreamingIndicator<double>.Width))!.GetValue(form)! >= 1);

        if (IndicatorCases.MadeByDefault(name) is not { } batch || batch is MultivariateIndicator<SignalResult>) return;
        var axes = new Axes();
        batch.Apply(axes);
        var drawn = axes.Series.OfType<LineSeries>().Select(line => line.Label).ToList();
        if (drawn.Contains(batch.Label)) Assert.Equal(batch.Label, type.GetProperty(nameof(Indicator.Label))!.GetValue(form));
        Assert.Empty(drawn.Except(form.OutputSeries.Select(line => line.Label)));
    }
}
