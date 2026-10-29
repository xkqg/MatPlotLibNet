// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>
/// <see cref="PanEvent.Shifted"/> returns a named <see cref="DataSpan"/>, not a tuple, and takes its range as one
/// value, so the method keeps within the four-parameter limit. The arithmetic is the one a pan applies to each axis.
/// </summary>
public class PanEventShiftedTests
{
    [Fact]
    public void Shifted_OnALinearAxis_MovesBothEndsByTheDelta()
    {
        var moved = PanEvent.Shifted(new DataSpan(1.0, 3.0), 2.0, AxisScale.Linear, 0.0);

        Assert.Equal(new DataSpan(3.0, 5.0), moved);
    }

    [Fact]
    public void Shifted_OnALogAxis_MovesByDecades()
    {
        // One decade up: 1..100 becomes 10..1000.
        var moved = PanEvent.Shifted(new DataSpan(1.0, 100.0), 1.0, AxisScale.Log, 0.0);

        Assert.Equal(10.0, moved.Min, precision: 9);
        Assert.Equal(1000.0, moved.Max, precision: 9);
    }

    [Fact]
    public void Shifted_NamesItsEnds_MinAndMax()
    {
        var moved = PanEvent.Shifted(new DataSpan(-4.0, -2.0), -1.0, AxisScale.Linear, 0.0);

        Assert.Equal(-5.0, moved.Min);
        Assert.Equal(-3.0, moved.Max);
    }
}
