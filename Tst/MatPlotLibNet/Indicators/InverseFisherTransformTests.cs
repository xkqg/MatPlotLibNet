// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="InverseFisherTransform"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class InverseFisherTransformTests
{
    // ── Branch 4 — Single value → length-1 output ──
    [Fact]
    public void Compute_SingleValue_ReturnsSingle()
    {
        var ift = new InverseFisherTransform([0.5]).Compute().Values;
        Assert.Single(ift);
        Assert.Equal(Math.Tanh(0.5), ift[0], precision: 9);
    }

    [Theory]
    [InlineData(100.0, 1.0, 6)]     // a very large positive input approaches +1
    [InlineData(-100.0, -1.0, 6)]   // a very large negative input approaches -1
    [InlineData(0.0, 0.0, 10)]      // zero stays zero
    public void Compute_AnInputOfOneValue_IsItsTanh(double input, double expected, int precision)
    {
        var ift = new InverseFisherTransform([input]).Compute().Values;
        Assert.Equal(expected, ift[0], precision);
    }

    // ── Branch 8 — Normal mixed input + scale > 1 ──
    [Fact]
    public void Compute_KnownTanhValues_Match()
    {
        var ift = new InverseFisherTransform([0.0, 0.5, 1.0, -0.5, -1.0]).Compute().Values;
        Assert.Equal(5, ift.Length);
        Assert.Equal(0.0, ift[0], precision: 9);
        Assert.Equal(Math.Tanh(0.5), ift[1], precision: 9);
        Assert.Equal(Math.Tanh(1.0), ift[2], precision: 9);
        Assert.Equal(Math.Tanh(-0.5), ift[3], precision: 9);
        Assert.Equal(Math.Tanh(-1.0), ift[4], precision: 9);
    }

    [Fact]
    public void Compute_ScaleSteepensTransition()
    {
        var ift = new InverseFisherTransform([0.5], scale: 2.0).Compute().Values;
        Assert.Equal(Math.Tanh(2.0 * 0.5), ift[0], precision: 9);
    }
}
