// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies the <see cref="AxisBreak"/> model and how an axis takes it in (<c>AddXBreak</c>, <c>AddYBreak</c>).</summary>
public class AxisBreakTests
{
    // ---- Axes XBreaks/YBreaks -----------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddXBreakAndAddYBreak_Add(bool x)
    {
        var axes = new Axes();
        if (x) axes.AddXBreak(50, 100);
        else axes.AddYBreak(200, 500);
        Assert.Single(x ? axes.XBreaks : axes.YBreaks);
    }

    [Fact]
    public void MultipleBreaks_AllStored()
    {
        var axes = new Axes();
        axes.AddXBreak(10, 20).AddXBreak(50, 60);
        Assert.Equal(2, axes.XBreaks.Count);
    }

    [Fact]
    public void AddXBreak_ReturnsAxes_ForChaining()
    {
        var axes = new Axes();
        var result = axes.AddXBreak(10, 20);
        Assert.Same(axes, result);
    }
}
