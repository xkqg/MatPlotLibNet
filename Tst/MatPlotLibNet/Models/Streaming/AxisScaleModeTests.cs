// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Streaming;

public sealed class AxisScaleModeTests
{
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

    // ── What each mode does to an axis: the mode owns its rule, for X and for Y ──

    private static Axis Axis(double? min = null, double? max = null)
    {
        var axis = new Axes().XAxis;
        axis.Min = min;
        axis.Max = max;
        return axis;
    }

    [Fact]
    public void Fixed_LeavesBothAxesAsTheyAre()
    {
        var x = Axis(1, 2);
        var y = Axis(3, 4);

        new AxisScaleMode.Fixed().ScaleX(x, 10, 20);
        new AxisScaleMode.Fixed().ScaleY(y, 10, 20);

        Assert.Equal((1.0, 2.0), (x.Min, x.Max));
        Assert.Equal((3.0, 4.0), (y.Min, y.Max));
    }

    public static TheoryData<Action<Axis, double, double>, double, double, double, double> TheAutoScale => new()
    {
        { (axis, low, high) => new AxisScaleMode.AutoScale().ScaleX(axis, low, high), 10, 20, 10, 20 },
        { (axis, low, high) => new AxisScaleMode.AutoScale().ScaleY(axis, low, high), 7, 7, 6, 8 }, // flat data gets a margin of one
    };

    [Theory]
    [MemberData(nameof(TheAutoScale))]
    public void AutoScale_FitsTheAxisToTheData(Action<Axis, double, double> scale, double low, double high, double min, double max)
    {
        var axis = Axis();
        scale(axis, low, high);
        Assert.Equal((min, max), (axis.Min, axis.Max));
    }

    [Fact]
    public void AutoScale_FitsYToTheData_WithAMarginOfATwentiethOfTheSpan()
    {
        var y = Axis();
        new AxisScaleMode.AutoScale().ScaleY(y, 10, 20);
        Assert.Equal(9.5, y.Min!.Value, 12);
        Assert.Equal(20.5, y.Max!.Value, 12);
    }

    [Fact]
    public void SlidingWindow_ShowsTheLastWindowOfX()
    {
        var x = Axis();
        new AxisScaleMode.SlidingWindow(5).ScaleX(x, 10, 20);
        Assert.Equal((15.0, 20.0), (x.Min, x.Max));
    }

    /// <summary>A sticky right edge scrolls while the data reaches it, and the first time when the axis has no right edge yet; it stays
    /// where the reader panned to when the data does not reach it.</summary>
    [Theory]
    [InlineData(10.0, 20.0, 21.0, 16.0, 21.0)]
    [InlineData(null, null, 21.0, 16.0, 21.0)]
    [InlineData(0.0, 10.0, 9.0, 0.0, 10.0)]
    public void StickyRight_ScrollsOnlyWhileTheDataReachesTheRightEdge(double? min, double? max, double dataMax, double expectedMin, double expectedMax)
    {
        var x = Axis(min, max);
        new AxisScaleMode.StickyRight(5).ScaleX(x, 0, dataMax);
        Assert.Equal((expectedMin, expectedMax), (x.Min, x.Max));
    }

    /// <summary>A window is a span of X: on the Y axis the two window modes leave the axis as it is, the rule the switch
    /// that chose the Y behaviour followed by having no arm for them.</summary>
    [Fact]
    public void TheWindowModes_LeaveTheYAxisAsItIs()
    {
        var y = Axis(3, 4);

        new AxisScaleMode.SlidingWindow(5).ScaleY(y, 10, 20);
        new AxisScaleMode.StickyRight(5).ScaleY(y, 10, 20);

        Assert.Equal((3.0, 4.0), (y.Min, y.Max));
    }
}
