// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>How a modifier reads a pointer against the layout, once (<see cref="ChartLayoutViewExtensions"/>): whether it falls on an
/// axes, the axes it fell in (the first when it fell in none), a press of the left button on an axes, the press read as its axes and
/// the data under it, and the range every secondary Y axis shows between two pixels.</summary>
public class ChartLayoutPressTests
{
    private static IChartLayout Layout()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 5;
        return ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
    }

    private static PointerInputArgs Press(double x, double y, PointerButton button = PointerButton.Left) =>
        new(x, y, button, ModifierKeys.None);

    [Fact]
    public void APointerOverAnAxes_IsOnIt_AndOffEveryAxes_IsNot()
    {
        var layout = Layout();

        Assert.True(layout.IsOnAxes(60, 35));
        Assert.False(layout.IsOnAxes(500, 500));
    }

    [Fact]
    public void TheAxesAPointerFellIn_IsItsIndex_AndTheFirstWhenItFellInNone()
    {
        var layout = Layout();

        Assert.Equal(0, layout.AxesOrFirstAt(60, 35));
        Assert.Equal(0, layout.AxesOrFirstAt(500, 500));
    }

    [Fact]
    public void ALeftPressOnAnAxes_NamesItsAxes_AnotherButtonOrAPressOffTheAxesNamesNone()
    {
        var layout = Layout();

        Assert.Equal(0, layout.LeftPressAxes(Press(60, 35)));
        Assert.True(layout.IsLeftPressOnAxes(Press(60, 35)));
        Assert.Null(layout.LeftPressAxes(Press(60, 35, PointerButton.Right)));
        Assert.False(layout.IsLeftPressOnAxes(Press(60, 35, PointerButton.Right)));
        Assert.Null(layout.LeftPressAxes(Press(500, 500)));
        Assert.False(layout.IsLeftPressOnAxes(Press(500, 500)));
    }

    [Fact]
    public void APress_IsReadAsTheAxesItFellInAndTheDataUnderIt()
    {
        var layout = Layout();

        var press = layout.PressAt(60, 35);

        Assert.NotNull(press);
        Assert.Equal(0, press.Value.AxesIndex);
        var expected = layout.PixelToData(60, 35, 0)!.Value;
        Assert.Equal(expected.DataX, press.Value.Data.DataX);
        Assert.Equal(expected.DataY, press.Value.Data.DataY);
    }

    [Fact]
    public void APressNoDataLiesUnder_IsNone()
    {
        Assert.Null(new FakeLayout().PressAt(1, 1));
    }

    [Fact]
    public void TheSecondaryRangesBetweenTwoPixels_AreEachAxisOwnReading_InTheShapeAsked()
    {
        var layout = new FakeLayout();
        var top = new Point(100, 0);
        var bottom = new Point(0, 100);

        var corners = layout.SecondaryRangesBetween(0, top, bottom, DataRange.FromCorners);
        var box = layout.SecondaryRangesBetween(0, top, bottom, DataRange.Between);

        var corner = Assert.Single(corners!);
        Assert.Equal(new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(10, 0, 200, 100)), corner);
        Assert.Equal(new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 10, 100, 200)), Assert.Single(box!));
    }

    [Fact]
    public void AnAxesWithNoSecondaryAxis_HasNoSecondaryRanges()
    {
        Assert.Null(Layout().SecondaryRangesBetween(0, new Point(10, 10), new Point(20, 20), DataRange.Between));
    }

    [Fact]
    public void TheBoxOfTwoCorners_PutsEachCoordinatesSmallerValueLow_TheCornersKeepTheirOrder()
    {
        var high = new Point(4, 1);
        var low = new Point(2, 3);

        Assert.Equal(new DataRange(2, 4, 1, 3), DataRange.Between(high, low));
        Assert.Equal(new DataRange(4, 2, 1, 3), DataRange.FromCorners(high, low));
    }

    /// <summary>A host's own layout: a plot of 100 by 100 pixels showing 0..10 on both axes, one secondary axis on the right showing
    /// 100..200, and no data under any pixel.</summary>
    private sealed class FakeLayout : TestChartLayout, IChartLayout
    {
        public FakeLayout() : base(new Rect(0, 0, 100, 100), new DataRange(0, 10, 0, 10)) => AxesHit = null;

        public DataPoint? PixelToData(double pixelX, double pixelY, int axesIndex) => null;

        public IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) =>
            [new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 10, 100, 200))];
    }
}
