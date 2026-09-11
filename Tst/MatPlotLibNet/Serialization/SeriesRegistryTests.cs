// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies test-isolation hooks on the static <see cref="SeriesRegistry"/>.
/// The registry is process-global by design (all <see cref="ChartSerializer"/> instances
/// share one factory dispatch table), so test code that registers a custom factory must
/// be able to roll back its mutation. <c>ResetForTests</c> rebuilds the default registration
/// set without leaving stray test factories visible to sibling test runs.</summary>
/// <remarks>Tagged into the <c>ChartSerializerGlobalState</c> collection (see
/// <see cref="ChartSerializerGlobalStateCollection"/>): this class mutates the process-global
/// registry, so it must not run concurrently with any test that reads through it via
/// <c>ChartSerializer.FromJson</c> / <c>SeriesRegistry.Create</c>.</remarks>
[Collection("ChartSerializerGlobalState")]
public class SeriesRegistryTests
{
    private static readonly object _lock = new();

    [Fact]
    public void ResetForTests_DropsCustomRegistration()
    {
        lock (_lock)
        {
            try
            {
                SeriesRegistry.Register("__test_custom__", (axes, _) => axes.Plot(new double[] { 0.0 }, new double[] { 0.0 }));

                var probeBefore = SeriesRegistry.Create("__test_custom__", FreshAxes(), new SeriesDto());
                Assert.NotNull(probeBefore);

                SeriesRegistry.ResetForTestsInternal();

                var probeAfter = SeriesRegistry.Create("__test_custom__", FreshAxes(), new SeriesDto());
                Assert.Null(probeAfter);
            }
            finally
            {
                // Defensive: even if assertions throw, leave the registry in its default state.
                SeriesRegistry.ResetForTestsInternal();
            }
        }
    }

    [Fact]
    public void ResetForTests_RestoresBuiltInFactories()
    {
        lock (_lock)
        {
            SeriesRegistry.Register("line", (axes, _) => null);   // Stomp the default.
            SeriesRegistry.ResetForTestsInternal();

            // After reset, the built-in "line" factory must produce a real LineSeries again.
            var s = SeriesRegistry.Create("line", FreshAxes(),
                new SeriesDto { XData = [0.0, 1.0], YData = [0.0, 1.0] });
            Assert.IsType<LineSeries>(s);
        }
    }

    /// <summary>The registry exposes the key set it dispatches on, sorted, so a consumer that has to SHOW
    /// the chart types (the MCP server's <c>list_chart_types</c>) reads the one list the serializer uses
    /// instead of keeping a second one that drifts. Hash order is not an order, so the list is sorted.</summary>
    [Fact]
    public void Discriminators_ListEveryRegisteredType_Sorted_WithoutDuplicates()
    {
        lock (_lock)
        {
            SeriesRegistry.ResetForTestsInternal();

            var listed = SeriesRegistry.Discriminators;

            Assert.Contains("line", listed);
            Assert.Contains("candlestick", listed);
            Assert.Contains("surface", listed);
            Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(listed.OrderBy(d => d, StringComparer.Ordinal), listed);
            Assert.NotEmpty(listed);
        }
    }

    [Fact]
    public void Discriminators_FollowARegistration_AndAReset()
    {
        lock (_lock)
        {
            try
            {
                SeriesRegistry.ResetForTestsInternal();
                int before = SeriesRegistry.Discriminators.Count;

                SeriesRegistry.Register("__test_custom__", (axes, _) => null);

                Assert.Contains("__test_custom__", SeriesRegistry.Discriminators);
                Assert.Equal(before + 1, SeriesRegistry.Discriminators.Count);
            }
            finally
            {
                SeriesRegistry.ResetForTestsInternal();
            }

            Assert.DoesNotContain("__test_custom__", SeriesRegistry.Discriminators);
        }
    }

    private static Axes FreshAxes() => new Axes();

    /// <summary>Council fix F3: <c>ResetForTests</c> is a test-infrastructure escape hatch that
    /// should never have been on the production public API. It becomes an <c>[Obsolete]</c> public
    /// shim (kept for one release so external consumers' test suites keep compiling) delegating to
    /// the real, <c>internal</c> reset entry point that in-repo tests reach via
    /// <c>InternalsVisibleTo</c>.</summary>
    [Fact]
    public void SeriesRegistry_ResetForTests_PublicShim_IsObsolete()
    {
        var method = typeof(SeriesRegistry).GetMethod(nameof(SeriesRegistry.ResetForTests), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        var obsolete = method!.GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(obsolete);
    }
}
