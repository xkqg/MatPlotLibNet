// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one rule for stacking bars (<see cref="StackableExtensions"/>), which the axes renderer stands each layer
/// on and the range of stacked bars takes in.</summary>
public class StackableExtensionsTests
{
    private static IStackable Layer(params double[] values) =>
        new BarSeries([.. values.Select((_, i) => i.ToString(System.Globalization.CultureInfo.InvariantCulture))], values);

    [Fact]
    public void EachLayer_StandsOnTheSumOfTheLayersBelowItThatPointTheSameWay()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(4, 5, 6), Layer(-2, 0, 1)];

        var feet = layers.StackedFeet();

        Assert.Equal([0.0, 0, 0], feet[0]);
        Assert.Equal([1.0, 2, 3], feet[1]);
        // The falling value -2 stands on the layers that fall (none), not on the 5 the two above zero add up to; the 0 and
        // the 1 stand on the 7 and the 9.
        Assert.Equal([0.0, 7, 9], feet[2]);
    }

    /// <summary>The stack is worked out once: its feet are where the layers stand, and its ends are each foot plus its value.</summary>
    [Fact]
    public void TheStack_HoldsTheFeet_AndTheEndsOnThem()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(4, 5, 6), Layer(-2, 0, 1)];

        var stack = layers.Stack();

        Assert.Equal(layers.StackedFeet(), stack.Feet);
        Assert.Equal([[1.0, 2, 3], [5.0, 7, 9], [-2.0, 7, 10]], stack.Ends);
    }

    /// <summary>The axes a draw asks for its range keeps the stack it worked out, so every bar that asks for the range reads the
    /// one stack: the layers' values are read once.</summary>
    [Fact]
    public void TheAxesOfADraw_WorkTheStackOutOnce()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var counting = new TestStackable([1.0, 2.0]);
        axes.AddSeries(counting);
        axes.Bar(["A", "B"], [3.0, 4.0]);
        IAxesContext context = new AxesContextAdapter(axes);

        var first = context.BarStack;
        var second = context.BarStack;

        Assert.Same(first.Ends, second.Ends);
        Assert.Equal(1, counting.Reads);
        Assert.Equal([[1.0, 2], [4.0, 6]], first.Ends);
    }

    /// <summary>A context that keeps no stack (one an application wrote) gets the stack worked out on the spot, by the same rule.</summary>
    [Fact]
    public void AContextThatKeepsNoStack_GetsItWorkedOutOnTheSpot()
    {
        IAxesContext context = new TestAxesContext(BarMode: BarMode.Stacked, Series: [(ISeries)Layer(1, 2), (ISeries)Layer(3, 4)]);

        var stack = context.BarStack;

        Assert.Equal([[0.0, 0], [1.0, 2]], stack.Feet);
        Assert.Equal([[1.0, 2], [4.0, 6]], stack.Ends);
    }

    [Fact]
    public void EachLayer_EndsAtItsFootPlusItsValue_AFallingLayerBelowIt()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(4, 5, 6), Layer(-2, 0, 1)];

        var ends = layers.Stack().Ends;

        Assert.Equal([1.0, 2, 3], ends[0]);
        Assert.Equal([5.0, 7, 9], ends[1]);
        Assert.Equal([-2.0, 7, 10], ends[2]);
    }

    /// <summary>A diverging stack (a Likert chart): the layers above zero grow up from zero and the layers below zero grow
    /// down from zero, each on its own running sum. The columns hold [2, 3], [-1, -2] and [4, 1]: the layer of -1 and -2
    /// stands on zero, and the layer of 4 and 1 on the 2 and the 3.</summary>
    [Fact]
    public void ALikertStack_GrowsUpFromZeroAndDownFromZero()
    {
        IStackable[] layers = [Layer(2, 3), Layer(-1, -2), Layer(4, 1)];

        Assert.Equal([[0.0, 0], [0.0, 0], [2.0, 3]], layers.StackedFeet());
        Assert.Equal([[2.0, 3], [-1.0, -2], [6.0, 4]], layers.Stack().Ends);
    }

    [Fact]
    public void TwoFallingLayers_StackDownwardsOnOneAnother()
    {
        IStackable[] layers = [Layer(-1, -1), Layer(-2, -3)];

        Assert.Equal([[0.0, 0], [-1.0, -1]], layers.StackedFeet());
        Assert.Equal([[-1.0, -1], [-3.0, -4]], layers.Stack().Ends);
    }

    [Fact]
    public void AValueOfZero_StandsWithTheLayersAboveZero()
    {
        IStackable[] layers = [Layer(3), Layer(0), Layer(-2), Layer(5)];

        Assert.Equal([[0.0], [3.0], [0.0], [3.0]], layers.StackedFeet());
    }

    /// <summary>A value that is not a number adds to neither sum, so the layers after it still stand on finite feet; its
    /// own bar has no end and is not drawn.</summary>
    [Fact]
    public void AValueThatIsNotANumber_AddsToNeitherSum()
    {
        IStackable[] layers = [Layer(2, 1), Layer(double.NaN, 2), Layer(3, 3)];

        var feet = layers.StackedFeet();
        var ends = layers.Stack().Ends;

        Assert.Equal([2.0, 3], feet[2]);
        Assert.True(double.IsNaN(ends[1][0]));
        Assert.Equal([5.0, 6], ends[2]);
    }

    [Fact]
    public void AnInfinity_AddsToNeitherSumEither()
    {
        IStackable[] layers = [Layer(2), Layer(double.PositiveInfinity), Layer(3)];

        Assert.Equal([2.0], layers.StackedFeet()[2]);
    }

    /// <summary>The columns are the first layer's: a shorter layer adds nothing past its end and ends only where it has a
    /// value; a longer layer's extra values have no column.</summary>
    [Fact]
    public void TheColumns_AreTheFirstLayers()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(10), Layer(100, 100, 100, 100)];

        var feet = layers.StackedFeet();
        var ends = layers.Stack().Ends;

        Assert.Equal([11.0, 2, 3], feet[2]);
        Assert.Equal([11.0], ends[1]);
        Assert.Equal([111.0, 102, 103], ends[2]);
    }

    [Fact]
    public void NoLayers_StandAndEndNowhere()
    {
        IStackable[] layers = [];

        Assert.Empty(layers.StackedFeet());
        Assert.Empty(layers.Stack().Ends);
    }

    /// <summary>Plain columns stack by the same rule as series do: the layers of a polar axes' bars are not series that the
    /// model stacks, and take the stacking owner's rule through this one.</summary>
    [Fact]
    public void PlainColumns_StackByTheSameRule()
    {
        double[][] columns = [[2, 3], [-1, -2], [4, 1]];

        Assert.Equal([[0.0, 0], [0.0, 0], [2.0, 3]], columns.StackedFeet());
        Assert.Equal([[2.0, 3], [-1.0, -2], [6.0, 4]], columns.Stack().Ends);
    }

    [Fact]
    public void PlainColumns_NoneAtAll_StandNowhere()
    {
        double[][] none = [];

        Assert.Empty(none.StackedFeet());
        Assert.Empty(none.Stack().Ends);
    }
}
