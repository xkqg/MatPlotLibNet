// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="AreaSeries"/> EdgeColor and StepMode enhancements.</summary>
public class AreaSeriesEnhancementTests
{
    [Fact]
    public void AreaSeries_EdgeColor_DefaultsToNull()
    {
        var s = new AreaSeries([1.0, 2.0], [1.0, 2.0]);
        Assert.Null(s.EdgeColor);
    }

    [Fact]
    public void AreaSeries_EdgeColor_CanBeSet()
    {
        var s = new AreaSeries([1.0], [1.0]) { EdgeColor = new Color(255, 0, 0) };
        Assert.Equal(new Color(255, 0, 0), s.EdgeColor);
    }

    [Fact]
    public void AreaSeries_StepMode_DefaultsToDefault()
    {
        var s = new AreaSeries([1.0], [1.0]);
        Assert.Equal(DrawStyle.Default, s.StepMode);
    }

    [Theory]
    [InlineData(DrawStyle.StepsPre)]
    [InlineData(DrawStyle.StepsMid)]
    [InlineData(DrawStyle.StepsPost)]
    public void AreaSeries_StepMode_CanBeSetToEachStepVariant(DrawStyle mode)
    {
        var s = new AreaSeries([1.0], [1.0]) { StepMode = mode };
        Assert.Equal(mode, s.StepMode);
    }
}
