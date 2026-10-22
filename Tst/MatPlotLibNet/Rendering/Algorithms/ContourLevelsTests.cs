// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Algorithms;

namespace MatPlotLibNet.Tests.Rendering.Algorithms;

/// <summary><see cref="ContourLevels"/> is the one place that says which levels a contour draws: the levels a user names
/// (sorted, each once, finite) and the two even spacings a count of levels stands for. The even spacings are held to the
/// arithmetic the renderers used when each carried its own copy, bit for bit, so a figure that names no level draws the
/// bytes it always drew.</summary>
public class ContourLevelsTests
{
    private const double NaN = double.NaN;

    // ---- the levels a user names ------------------------------------------------------------------------------------

    [Fact]
    public void NoLevelsNamed_IsUnset_ForNullAndForAnEmptyList()
    {
        Assert.Null(((double[]?)null).ExplicitLevels());
        Assert.Null(Array.Empty<double>().ExplicitLevels());
    }

    [Theory]
    [InlineData(new[] { 1.0, 5.0, 9.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 9.0, 1.0, 5.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 5.0, 1.0, 5.0, 9.0, 1.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 9.0, NaN, 1.0, double.PositiveInfinity, 5.0, double.NegativeInfinity }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { -2.5, 0.0, 2.5 }, new[] { -2.5, 0.0, 2.5 })]
    [InlineData(new[] { 7.0 }, new[] { 7.0 })]
    public void NamedLevels_AreAscending_EachOnce_AndFinite(double[] named, double[] expected) =>
        Assert.Equal(expected, named.ExplicitLevels());

    /// <summary>Levels were named, and none can be drawn: that is a list with nothing in it, not an unset one, so a
    /// renderer draws nothing instead of falling back to a count the user did not ask for.</summary>
    [Theory]
    [InlineData(new[] { NaN })]
    [InlineData(new[] { double.PositiveInfinity, double.NegativeInfinity, NaN })]
    public void NamedLevels_ThatCannotBeDrawn_AreAnEmptyList_NotUnset(double[] named)
    {
        var levels = named.ExplicitLevels();

        Assert.NotNull(levels);
        Assert.Empty(levels);
    }

    [Fact]
    public void NamedLevels_AreCopied_TheCallersArrayIsNotReordered()
    {
        double[] named = [9, 1, 5];

        _ = named.ExplicitLevels();

        Assert.Equal([9.0, 1.0, 5.0], named);
    }

    // ---- the even spacings ----------------------------------------------------------------------------------------

    [Fact]
    public void InteriorLevels_AreCountLevelsStrictlyBetweenTheEnds()
    {
        Assert.Equal([2.0, 4.0, 6.0, 8.0], ContourLevels.Interior(0, 10, 4));
        Assert.Equal([-5.0], ContourLevels.Interior(-10, 0, 1));
    }

    [Fact]
    public void SpanningLevels_RunFromTheLowEndToTheHighEnd_CountOfThem()
    {
        Assert.Equal([0.0, 2.5, 5.0, 7.5, 10.0], ContourLevels.Spanning(0, 10, 5));
        Assert.Equal([-1.0, 0.0, 1.0], ContourLevels.Spanning(-1, 1, 3));
        Assert.Equal(-3.25, ContourLevels.Spanning(-3.25, 4.1, 7)[0]);
    }

    /// <summary>The arithmetic each renderer carried before the owner existed, written out here: the owner must give the
    /// very same doubles, or the contour of a figure that names no level would move by a rounding step.</summary>
    [Fact]
    public void TheEvenSpacings_AreBitForBitTheArithmeticTheRenderersHad()
    {
        var random = new Random(20260701);
        for (int trial = 0; trial < 500; trial++)
        {
            double lo = (random.NextDouble() - 0.5) * 200;
            double hi = lo + random.NextDouble() * 100 + 1e-3;
            int count = random.Next(2, 40);

            var interior = ContourLevels.Interior(lo, hi, count);
            var spanning = ContourLevels.Spanning(lo, hi, count);

            Assert.Equal(count, interior.Length);
            Assert.Equal(count, spanning.Length);
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(lo + (hi - lo) * (i + 1) / (count + 1), interior[i]);   // ContourSeriesRenderer
                Assert.Equal(lo + i * (hi - lo) / (count - 1), spanning[i]);         // MarchingSquares.ExtractBands and the iso-lines over a filled contour
                Assert.Equal(lo + (hi - lo) * i / (count - 1), spanning[i]);         // TricontourSeriesRenderer
            }
        }
    }
}
