// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StemSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class StemSeriesTests
{
    /// <summary>The stems stand on the baseline at 0, so the baseline is a sticky edge of the Y range. Along X nothing is
    /// sticky: the first and last stem are marks like any other and keep the axis margin, as matplotlib's stem does (it
    /// sets no sticky edge at all). Before, X was sticky at both ends of the data, so the first and last head stood on the
    /// plot's left and right edge, half of each outside the frame.</summary>
    [Fact]
    public void TheRange_StaysStickyOnTheBaselineOnly()
    {
        var axes = new Axes();

        var range = new StemSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]).ComputeDataRange(new AxesContextAdapter(axes));

        Assert.Equal(1.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
        Assert.Null(range.StickyXMin);
        Assert.Null(range.StickyXMax);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Null(range.StickyYMax);
    }

    // StemSeries — `if (0 > yMax) yMax = 0` — all-negative Y.
    [Fact] public void StemSeries_AllNegativeY_AdjustsYMaxToZero()
    {
        var s = new StemSeries(new double[] { 1, 2 }, new double[] { -1, -2 });
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.True(range.YMax >= 0);
    }
}
