// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies test-isolation hooks on the static <see cref="SeriesRegistry"/>.
/// The registry is process-global by design (all <see cref="ChartSerializer"/> instances
/// share one factory dispatch table), so test code that registers a custom factory must
/// be able to roll back its mutation. <c>ResetForTests</c> rebuilds the default registration
/// set without leaving stray test factories visible to sibling test runs.</summary>
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

                SeriesRegistry.ResetForTests();

                var probeAfter = SeriesRegistry.Create("__test_custom__", FreshAxes(), new SeriesDto());
                Assert.Null(probeAfter);
            }
            finally
            {
                // Defensive: even if assertions throw, leave the registry in its default state.
                SeriesRegistry.ResetForTests();
            }
        }
    }

    [Fact]
    public void ResetForTests_RestoresBuiltInFactories()
    {
        lock (_lock)
        {
            SeriesRegistry.Register("line", (axes, _) => null);   // Stomp the default.
            SeriesRegistry.ResetForTests();

            // After reset, the built-in "line" factory must produce a real LineSeries again.
            var s = SeriesRegistry.Create("line", FreshAxes(),
                new SeriesDto { XData = [0.0, 1.0], YData = [0.0, 1.0] });
            Assert.IsType<LineSeries>(s);
        }
    }

    private static Axes FreshAxes() => new Axes();
}
