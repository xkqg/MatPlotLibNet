// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one rule for stacking bars (<see cref="StackableExtensions"/>), which the axes renderer stands each layer
/// on and the range of stacked bars takes in.</summary>
public class StackableExtensionsTests
{
    private static IStackable Layer(params double[] values) =>
        new BarSeries([.. values.Select((_, i) => i.ToString(System.Globalization.CultureInfo.InvariantCulture))], values);

    [Fact]
    public void EachLayer_StandsOnTheSumOfTheLayersBelowIt()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(4, 5, 6), Layer(-2, 0, 1)];

        var feet = layers.StackedFeet();

        Assert.Equal([0.0, 0, 0], feet[0]);
        Assert.Equal([1.0, 2, 3], feet[1]);
        Assert.Equal([5.0, 7, 9], feet[2]);
    }

    [Fact]
    public void EachLayer_EndsAtItsFootPlusItsValue_AFallingLayerBelowIt()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(4, 5, 6), Layer(-2, 0, 1)];

        var ends = layers.StackedEnds();

        Assert.Equal([1.0, 2, 3], ends[0]);
        Assert.Equal([5.0, 7, 9], ends[1]);
        Assert.Equal([3.0, 7, 10], ends[2]);
    }

    /// <summary>The columns are the first layer's: a shorter layer adds nothing past its end and ends only where it has a
    /// value; a longer layer's extra values have no column.</summary>
    [Fact]
    public void TheColumns_AreTheFirstLayers()
    {
        IStackable[] layers = [Layer(1, 2, 3), Layer(10), Layer(100, 100, 100, 100)];

        var feet = layers.StackedFeet();
        var ends = layers.StackedEnds();

        Assert.Equal([11.0, 2, 3], feet[2]);
        Assert.Equal([11.0], ends[1]);
        Assert.Equal([111.0, 102, 103], ends[2]);
    }

    [Fact]
    public void NoLayers_StandAndEndNowhere()
    {
        IStackable[] layers = [];

        Assert.Empty(layers.StackedFeet());
        Assert.Empty(layers.StackedEnds());
    }
}
