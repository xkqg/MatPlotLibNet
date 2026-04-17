// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Streaming;

public sealed class AxisScaleModeTests
{
    [Fact]
    public void Fixed_IsDistinctType() =>
        Assert.IsType<AxisScaleMode.Fixed>(new AxisScaleMode.Fixed());

    [Fact]
    public void AutoScale_IsDistinctType() =>
        Assert.IsType<AxisScaleMode.AutoScale>(new AxisScaleMode.AutoScale());

    [Fact]
    public void SlidingWindow_StoresWindowSize()
    {
        var sw = new AxisScaleMode.SlidingWindow(100.0);
        Assert.Equal(100.0, sw.WindowSize);
    }

    [Fact]
    public void StickyRight_StoresWindowSize()
    {
        var sr = new AxisScaleMode.StickyRight(50.0);
        Assert.Equal(50.0, sr.WindowSize);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new AxisScaleMode.SlidingWindow(100.0);
        var b = new AxisScaleMode.SlidingWindow(100.0);
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new AxisScaleMode.SlidingWindow(100.0);
        var b = new AxisScaleMode.SlidingWindow(200.0);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentTypes_AreNotEqual()
    {
        AxisScaleMode a = new AxisScaleMode.Fixed();
        AxisScaleMode b = new AxisScaleMode.AutoScale();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AllModesAreAxisScaleMode()
    {
        Assert.IsAssignableFrom<AxisScaleMode>(new AxisScaleMode.Fixed());
        Assert.IsAssignableFrom<AxisScaleMode>(new AxisScaleMode.AutoScale());
        Assert.IsAssignableFrom<AxisScaleMode>(new AxisScaleMode.SlidingWindow(10));
        Assert.IsAssignableFrom<AxisScaleMode>(new AxisScaleMode.StickyRight(10));
    }

    [Fact]
    public void DefaultConfig_UsesSlidingWindowAndAutoScale()
    {
        var config = StreamingAxesConfig.Default(200.0);
        Assert.IsType<AxisScaleMode.SlidingWindow>(config.XMode);
        Assert.IsType<AxisScaleMode.AutoScale>(config.YMode);
        Assert.Equal(200.0, ((AxisScaleMode.SlidingWindow)config.XMode).WindowSize);
    }
}
