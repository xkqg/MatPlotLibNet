// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

/// <summary>Every series that says how often its data changed (a public <c>Version</c>) is counted by the streaming figure's clock
/// through the one internal question, <see cref="IVersionedSeries"/>, and keeps its samples and its count in one
/// <see cref="StreamRing{TSample}"/>. A streaming kind added without them would be drawn once and never again
/// (<see cref="StreamingFigure.DataVersion"/> could not see it), which is how the sampled signal was missed.</summary>
public sealed class StreamingVersionContractTests
{
    /// <summary>Every series type of the library's assemblies with a public <c>long Version</c>.</summary>
    public static TheoryData<Type> VersionedSeriesTypes { get; } = new(SeriesTypesWithAVersion());

    [Fact]
    public void TheScan_FindsEveryStreamingKind()
    {
        Type[] found = [.. SeriesTypesWithAVersion()];

        Assert.Contains(typeof(StreamingLineSeries), found);
        Assert.Contains(typeof(StreamingScatterSeries), found);
        Assert.Contains(typeof(StreamingCandlestickSeries), found);
        Assert.Contains(typeof(StreamingSignalSeries), found);
    }

    [Theory]
    [MemberData(nameof(VersionedSeriesTypes))]
    public void ASeriesWithAVersion_IsCountedByTheClock(Type type) =>
        Assert.True(typeof(IVersionedSeries).IsAssignableFrom(type),
            $"{type.Name} has a public Version but is not an IVersionedSeries: a streaming figure never asks it whether a frame is due");

    [Theory]
    [MemberData(nameof(VersionedSeriesTypes))]
    public void ASeriesWithAVersion_KeepsItsSamplesInAStreamRing(Type type)
    {
        var fields = HierarchyOf(type).SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)).ToList();

        Assert.Contains(fields, f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(StreamRing<>));
        Assert.DoesNotContain(fields, f => f.FieldType == typeof(long) && f.Name.Contains("version", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fields, f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(MatPlotLibNet.Data.RingBuffer<>));
    }

    private static IEnumerable<Type> SeriesTypesWithAVersion() =>
        LibraryAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ISeries).IsAssignableFrom(t))
            .Where(t => t.GetProperty("Version", BindingFlags.Instance | BindingFlags.Public)?.PropertyType == typeof(long))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

    private static IEnumerable<Assembly> LibraryAssemblies()
    {
        // The core and every package the test assembly references (Geo, DataFrame...), loaded by name so none is missed for not
        // having been touched yet.
        var core = typeof(ISeries).Assembly;
        var referenced = typeof(StreamingVersionContractTests).Assembly.GetReferencedAssemblies()
            .Where(n => n.Name is { } name && name.StartsWith("MatPlotLibNet", StringComparison.Ordinal))
            .Select(Assembly.Load);
        return referenced.Append(core).Distinct();
    }

    private static IEnumerable<Type> HierarchyOf(Type type)
    {
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType) yield return t;
    }
}
