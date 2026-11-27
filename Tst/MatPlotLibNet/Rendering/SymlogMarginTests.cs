// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A symmetric-log axis is padded in the space it is drawn in, as every scale is: matplotlib's <c>autoscale_view</c> adds the
/// margin after the axis' transform and maps the ends back (<c>x0t, x1t = transform.transform([x0, x1]); delta = (x1t - x0t) *
/// margin; x0, x1 = inverse_trans.transform([x0t - delta, x1t + delta])</c>), for a log, a logit and a symmetric-log axis alike. The
/// numbers are matplotlib 3.11.2's, read with python (<c>axes.ymargin</c> 0.05, <c>autolimit_mode</c> 'data'):
/// <code>
/// ax.plot([1, 2, 3], [0.5, 10, 500]); ax.set_yscale('symlog', linthresh=1.0)   -> ylim (0.35354634980487915, 727.2710200073208)
/// ax.plot([1, 2, 3], [0.5, 10, 500]); ax.set_yscale('symlog', linthresh=10.0)  -> ylim (-0.7395365019512083, 686.5881164454686)
/// ax.plot([1, 2, 3], [-500, -5, 300]); ax.set_yscale('symlog')  (linthresh 2)   -> ylim (-1093.4090869239733, 656.0454521543841)
/// </code>
/// A margin laid linearly on such an axis reached -24.5 under data that starts at 0.5, a decade and a half of empty axis.</summary>
public class SymlogMarginTests
{
    [Theory]
    [InlineData(new[] { 0.5, 10, 500 }, 1.0, 0.35354634980487915, 727.2710200073208)]
    [InlineData(new[] { 0.5, 10, 500 }, 10.0, -0.7395365019512083, 686.5881164454686)]
    [InlineData(new[] { -500.0, -5, 300 }, 2.0, -1093.4090869239733, 656.0454521543841)]
    public void ASymlogAxis_IsPaddedInTheSpaceItIsDrawnIn_AsMatplotlibPadsIt(double[] y, double linthresh, double lo, double hi)
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], y).WithSymlogYScale(linthresh)).Build().SubPlots[0];

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(lo, range.YMin, Math.Abs(lo) * 1e-12);
        Assert.Equal(hi, range.YMax, Math.Abs(hi) * 1e-12);
        Assert.Equal(0.9, range.XMin, 1e-12);
        Assert.Equal(3.1, range.XMax, 1e-12);
    }

    /// <summary>The padding asks the scale's own mapping, so a symmetric-log axis' threshold moves its margin.</summary>
    [Fact]
    public void TheMargin_IsLaidByTheAxisThreshold()
    {
        var range = new Range1D(0.5, 500);

        var one = new AxisAutoscale(new Axis { Scale = AxisScale.SymLog, SymLogLinThresh = 1 }, isX: false, Theme.Default).Padded(range, 0.05);
        var ten = new AxisAutoscale(new Axis { Scale = AxisScale.SymLog, SymLogLinThresh = 10 }, isX: false, Theme.Default).Padded(range, 0.05);

        Assert.Equal(0.35354634980487915, one.Lo, 1e-12);
        Assert.Equal(-0.7395365019512083, ten.Lo, 1e-12);
    }
}
