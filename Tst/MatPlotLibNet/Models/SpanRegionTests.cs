// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SpanRegion"/> behavior.</summary>
public class SpanRegionTests
{
    /// <summary>Verifies that AxHSpan adds a horizontal span region and AxVSpan a vertical one.</summary>
    public static TheoryData<Func<Axes, SpanRegion>, Orientation> TheSpanVerbs => new()
    {
        { axes => axes.AxHSpan(1.0, 3.0), Orientation.Horizontal },
        { axes => axes.AxVSpan(2.0, 4.0), Orientation.Vertical },
    };

    [Theory]
    [MemberData(nameof(TheSpanVerbs))]
    public void Axes_AnAxSpanVerb_AddsSpan(Func<Axes, SpanRegion> verb, Orientation orientation)
    {
        var axes = new Axes();
        var span = verb(axes);
        Assert.Single(axes.Spans);
        Assert.Equal(orientation, span.Orientation);
    }

}
