// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Data;

/// <summary>Observations split by a hue are drawn by one rule: one series with no configuration when there are no labels, otherwise
/// one series per group in the order the labels are first seen, wearing the palette's colour of the group and its label; a
/// histogram of a group is drawn at the overlap opacity.</summary>
public sealed class HueSplitTests
{
    private sealed record Drawn(double[] X, double[] Y, LineSeries? Dressed);

    private static List<Drawn> Draw(HueSplit split)
    {
        var drawn = new List<Drawn>();
        split.Draw<LineSeries>((x, y, dress) =>
        {
            LineSeries? series = null;
            if (dress is not null)
            {
                series = new LineSeries(x, y);
                dress(series);
            }

            drawn.Add(new Drawn(x, y, series));
        });
        return drawn;
    }

    [Fact]
    public void WithoutLabels_TheObservationsAreOneSeries_LeftAsTheyAre()
    {
        var drawn = Draw(new HueSplit([1, 2], [3, 4], null, null));

        var only = Assert.Single(drawn);
        Assert.Equal([1.0, 2.0], only.X);
        Assert.Equal([3.0, 4.0], only.Y);
        Assert.Null(only.Dressed);
    }

    [Fact]
    public void WithLabels_EachGroupIsASeries_InTheOrderFirstSeen_InThePalettesColourAndWithItsLabel()
    {
        var drawn = Draw(new HueSplit([1, 2, 3], [4, 5, 6], ["b", "a", "b"], [Colors.Red, Colors.Blue]));

        Assert.Equal(2, drawn.Count);
        Assert.Equal([1.0, 3.0], drawn[0].X);
        Assert.Equal([4.0, 6.0], drawn[0].Y);
        Assert.Equal("b", drawn[0].Dressed!.Label);
        Assert.Equal(Colors.Red, drawn[0].Dressed!.Color);
        Assert.Equal("a", drawn[1].Dressed!.Label);
        Assert.Equal(Colors.Blue, drawn[1].Dressed!.Color);
    }

    [Fact]
    public void AHistogramOfAGroup_IsDrawnAtTheOverlapOpacity_AndOneWithoutLabelsIsLeftAsItIs()
    {
        var dressed = new List<HistogramSeries?>();
        void Add(double[] values, Action<HistogramSeries>? dress)
        {
            HistogramSeries? series = null;
            if (dress is not null)
            {
                series = new HistogramSeries(values);
                dress(series);
            }

            dressed.Add(series);
        }

        new HueSplit([1, 2], [1, 2], ["x", "y"], null).DrawOverlapping(Add);
        new HueSplit([1, 2], [1, 2], null, null).DrawOverlapping(Add);

        Assert.All(dressed.Take(2), s => Assert.Equal(HueSplit.OverlapAlpha, s!.Alpha));
        Assert.Null(dressed[2]);
    }
}
