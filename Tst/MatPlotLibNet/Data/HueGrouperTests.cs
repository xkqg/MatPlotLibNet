// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Data;

/// <summary>Verifies <see cref="HueGrouper"/> grouping, ordering, and color-cycling behaviour.</summary>
public class HueGrouperTests
{
    private record Row(string Cat, double X, double Y);

    [Fact]
    public void SingleGroup_ReturnsOneGroup()
    {
        var data = new[] { new Row("A", 1, 2), new Row("A", 3, 4) };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        Assert.Single(groups);
    }

    [Fact]
    public void SingleGroup_LabelMatchesKey()
    {
        var data = new[] { new Row("Alpha", 1, 2), new Row("Alpha", 3, 4) };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        Assert.Equal("Alpha", groups[0].Label);
    }

    [Fact]
    public void SingleGroup_XYArraysContainAllPoints()
    {
        var data = new[] { new Row("A", 1.0, 10.0), new Row("A", 2.0, 20.0), new Row("A", 3.0, 30.0) };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        Assert.Equal([1.0, 2.0, 3.0], groups[0].X);
        Assert.Equal([10.0, 20.0, 30.0], groups[0].Y);
    }

    [Fact]
    public void MultiGroup_ThreeCategories_ReturnsThreeGroups()
    {
        var data = new[]
        {
            new Row("A", 1, 1), new Row("B", 2, 2),
            new Row("C", 3, 3), new Row("A", 4, 4),
        };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        Assert.Equal(3, groups.Length);
    }

    [Fact]
    public void MultiGroup_DataSplitsCorrectly()
    {
        var data = new[]
        {
            new Row("A", 1, 10), new Row("B", 2, 20),
            new Row("A", 3, 30),
        };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        var a = groups.Single(g => g.Label == "A");
        var b = groups.Single(g => g.Label == "B");
        Assert.Equal([1.0, 3.0], a.X);
        Assert.Equal([2.0], b.X);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyArray()
    {
        var groups = HueGrouper.GroupBy(Array.Empty<Row>(), r => r.Cat, r => r.X, r => r.Y);
        Assert.Empty(groups);
    }

    [Fact]
    public void PreservesFirstSeenOrder()
    {
        var data = new[]
        {
            new Row("B", 1, 1), new Row("A", 2, 2), new Row("C", 3, 3),
        };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        Assert.Equal(["B", "A", "C"], groups.Select(g => g.Label).ToArray());
    }

    [Fact]
    public void DefaultPalette_CyclesColors()
    {
        var data = Enumerable.Range(0, 12)
            .Select(i => new Row(i.ToString(), i, i))
            .ToArray();
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y);
        // Index 10 wraps around to same color as index 0
        Assert.Equal(groups[0].Color, groups[10].Color);
    }

    [Fact]
    public void CustomPalette_UsesProvidedColors()
    {
        var red  = Color.FromHex("#FF0000");
        var blue = Color.FromHex("#0000FF");
        var data = new[] { new Row("A", 1, 1), new Row("B", 2, 2) };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y, [red, blue]);
        Assert.Equal(red,  groups[0].Color);
        Assert.Equal(blue, groups[1].Color);
    }

    [Fact]
    public void CustomPalette_WrapsWhenFewerColorsThanGroups()
    {
        var red = Color.FromHex("#FF0000");
        var data = new[] { new Row("A", 1, 1), new Row("B", 2, 2), new Row("C", 3, 3) };
        var groups = HueGrouper.GroupBy(data, r => r.Cat, r => r.X, r => r.Y, [red]);
        Assert.All(groups, g => Assert.Equal(red, g.Color));
    }

    [Fact]
    public void IntegerKey_LabelIsStringRepresentation()
    {
        var data = new[] { (Key: 1, X: 1.0, Y: 2.0), (Key: 2, X: 3.0, Y: 4.0) };
        var groups = HueGrouper.GroupBy(data, t => t.Key, t => t.X, t => t.Y);
        Assert.Equal(["1", "2"], groups.Select(g => g.Label).ToArray());
    }
}
