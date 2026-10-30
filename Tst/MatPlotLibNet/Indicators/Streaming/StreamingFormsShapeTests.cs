// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>What every streaming form shares, shown on all of them at once: it can be made with its defaults, it has a warm-up of at
/// least one input, it names its lines, it hands the capacity it is given to every line, and it has not seen an input yet. The
/// values are the business of <see cref="StreamingEqualsBatchTests"/>; this holds the shape.</summary>
public sealed class StreamingFormsShapeTests
{
    private static IEnumerable<Type> Forms() =>
        typeof(StreamingSma).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && t.Namespace == "MatPlotLibNet.Indicators.Streaming" && t.Name.StartsWith("Streaming", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.Ordinal);

    private static bool IsWindowed(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(WindowedStreamingIndicator<>)) return true;
        }

        return false;
    }

    /// <summary>The form made with the defaults it declares and a capacity of 7; a required number (<c>Roc.lookback</c>, the period of
    /// the older forms) is 5.</summary>
    private static IStreamingIndicator Make(Type form)
    {
        var ctor = form.GetConstructors().Single();
        var args = ctor.GetParameters()
            .Select(p => p.Name == "capacity" ? 7
                : p.ParameterType == typeof(TransferEntropyOptions) ? new TransferEntropyOptions(Window: 30)
                : p.HasDefaultValue ? p.DefaultValue : (object)5)
            .ToArray();
        return (IStreamingIndicator)ctor.Invoke(args);
    }

    public static TheoryData<string> Names() => new(Forms().Select(t => t.Name));

    /// <summary>The map says which indicators stream; each of them has a form of its name (the form of <c>BollingerBands</c> is
    /// <c>StreamingBollinger</c>, so a form may carry the start of the name).</summary>
    [Fact]
    public void EveryIndicatorTheMapStreams_HasAForm()
    {
        var names = Forms().Select(t => t.Name["Streaming".Length..]).ToList();

        foreach (var line in StreamingMap.All.Where(c => c.IsStreamed))
            Assert.True(names.Any(n => n == line.Name || line.Name.StartsWith(n, StringComparison.Ordinal)), $"{line.Name} is streamed and has no form");
    }

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

    [Theory]
    [MemberData(nameof(Names))]
    public void AStreamingForm_CanBeMadeWithItsDefaults_AndHasAShape(string name)
    {
        var type = Forms().Single(t => t.Name == name);
        var form = Make(type);

        Assert.True(form.WarmupPeriod >= 1, $"{name} warms up in {form.WarmupPeriod} inputs");
        Assert.NotEmpty(form.OutputSeries);
        Assert.All(form.OutputSeries, line => Assert.False(string.IsNullOrWhiteSpace(line.Label), $"{name} has an unnamed line"));
        Assert.All(form.OutputSeries, line => Assert.Equal(7, line.Capacity));
        Assert.Equal(0, form.ProcessedCount);
        Assert.False(form.IsWarmedUp);
        Assert.True(double.IsNaN(form.GetLatest()));
        if (IsWindowed(type)) Assert.True(((dynamic)form).Width >= 1);
    }
}
