// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A logit axis ranges over its data as matplotlib's does: the data limits keep every finite value, and the scale's
/// <c>limit_range_for_scale</c> takes an end at or below 0 to the smallest positive value (<c>minpos</c>) and an end at or above 1 to
/// one less it; then the margin is laid in the logit space. A value at or above 1 therefore ends the axis one <c>minpos</c> short of
/// 1, not at the largest value below 1. The numbers are matplotlib 3.11.2's, read with python (<c>axes.ymargin</c> 0.05):
/// <code>
/// ax.plot(range(4), [0, 0.3, 0.6, 1]); ax.set_yscale('logit')  -> ylim (0.28251353013018654, 0.7174864698698133), minpos 0.3
/// ax.plot(range(3), [0.2, 0.5, 1.5]); ax.set_yscale('logit')   -> ylim (0.17873760922563603, 0.8212623907743639), minpos 0.2
/// ax.plot(range(3), [0.1, 0.4, 0.8]); ax.set_yscale('logit')   -> ylim (0.08499006745548109, 0.8271369057737346), minpos 0.1
/// </code></summary>
public class LogitDataLimitsTests
{
    [Theory]
    [InlineData(new[] { 0.0, 0.3, 0.6, 1 }, 0.28251353013018654, 0.7174864698698133)]
    [InlineData(new[] { 0.2, 0.5, 1.5 }, 0.17873760922563603, 0.8212623907743639)]
    [InlineData(new[] { 0.1, 0.4, 0.8 }, 0.08499006745548109, 0.8271369057737346)]
    public void ALogitAxis_EndsItsDataAtTheSmallestPositiveValue_AndOneLessIt(double[] y, double lo, double hi)
    {
        double[] x = [.. Enumerable.Range(0, y.Length).Select(i => (double)i)];
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, y).SetYScale(AxisScale.Logit)).Build().SubPlots[0];

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(lo, range.YMin, 1e-12);
        Assert.Equal(hi, range.YMax, 1e-12);
    }

    /// <summary>What a range is folded from on each scale: every finite value on a linear, symmetric-log and date axis; on a log and a
    /// logit axis the values above 0, a logit axis keeping those at and above 1, which its range then ends one <c>minpos</c> short
    /// of. A missing value and an infinity are in no range.</summary>
    [Theory]
    [InlineData(AxisScale.Logit, 1.0, true)]
    [InlineData(AxisScale.Logit, 1.5, true)]
    [InlineData(AxisScale.Logit, 0.0, false)]
    [InlineData(AxisScale.Log, 0.0, false)]
    [InlineData(AxisScale.Log, 3.0, true)]
    [InlineData(AxisScale.Linear, -3.0, true)]
    [InlineData(AxisScale.SymLog, -3.0, true)]
    [InlineData(AxisScale.Logit, double.PositiveInfinity, false)]
    [InlineData(AxisScale.Linear, double.NaN, false)]
    public void ARange_IsFoldedFromTheValuesAtOrAboveTheLowestTheScalePlaces(AxisScale scale, double value, bool taken) =>
        Assert.Equal(taken, value.IsPlaceable(scale.Rule));
}
