// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="ReferenceLine"/> behavior.</summary>
public class ReferenceLineTests
{
    /// <summary>Verifies that AxHLine adds a horizontal reference line and AxVLine a vertical one.</summary>
    public static TheoryData<Func<Axes, ReferenceLine>, Orientation> TheLineVerbs => new()
    {
        { axes => axes.AxHLine(5.0), Orientation.Horizontal },
        { axes => axes.AxVLine(3.0), Orientation.Vertical },
    };

    [Theory]
    [MemberData(nameof(TheLineVerbs))]
    public void Axes_AnAxLineVerb_AddsReferenceLine(Func<Axes, ReferenceLine> verb, Orientation orientation)
    {
        var axes = new Axes();
        var line = verb(axes);
        Assert.Single(axes.ReferenceLines);
        Assert.Equal(orientation, line.Orientation);
    }

}
