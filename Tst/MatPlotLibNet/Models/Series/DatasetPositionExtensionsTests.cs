// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Where the datasets of a box or violin series stand, and the range that follows: the renderer and the series
/// both ask here, so the picture and the axes cannot disagree.</summary>
public class DatasetPositionExtensionsTests
{
    // ── where a dataset stands ─────────────────────────────────────────────────

    [Fact]
    public void WithoutPositions_ADatasetStandsAtItsIndex()
    {
        double[]? none = null;

        Assert.Equal(0, none.PositionOf(0));
        Assert.Equal(3, none.PositionOf(3));
    }

    [Fact]
    public void ADatasetThePositionsReach_StandsWhereTheyPutIt() =>
        Assert.Equal(20, new[] { 10.0, 20, 30 }.PositionOf(1));

    [Fact]
    public void ADatasetThePositionsDoNotReach_StandsAtItsIndex()
    {
        double[] positions = [10, 20];

        Assert.Equal(20, positions.PositionOf(1));
        Assert.Equal(2, positions.PositionOf(2));
        Assert.Equal(5, positions.PositionOf(5));
    }

    // ── the range that follows ─────────────────────────────────────────────────

    private static readonly Range1D Frame = new(-1, 3);

    private static Range1D? Cover(double[]? positions, params Range1D[] drawn) =>
        drawn.CoverAlongPositions(positions, Frame);

    [Fact]
    public void PlacedByPositions_OnlyThePlacesDrawnAtCount() =>
        Assert.Equal(new Range1D(9.5, 20.5), Cover([10, 20], new Range1D(9.5, 10.5), new Range1D(19.5, 20.5)));

    [Fact]
    public void PlacedByPositions_NothingDrawn_CoversNothing() =>
        Assert.Null(Cover([10]));

    [Fact]
    public void PlacedByPositions_AnUndrawablePlaceIsLeftOut() =>
        Assert.Equal(new Range1D(9.5, 10.5),
            Cover([10, double.NaN], new Range1D(9.5, 10.5), new Range1D(double.NaN, double.NaN)));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NotPlaced_TheFrameIsKept_WhenWhatIsDrawnLiesInsideIt(bool emptyArray) =>
        Assert.Equal(Frame, Cover(emptyArray ? [] : null, new Range1D(-0.25, 0.25), new Range1D(1.75, 2.25)));

    [Fact]
    public void NotPlaced_TheFrameIsKept_WhenNothingIsDrawn() =>
        Assert.Equal(Frame, Cover(null));

    [Fact]
    public void NotPlaced_TheFrameWidensOnEitherSide_ForWhatIsDrawnWiderThanIt() =>
        Assert.Equal(new Range1D(-4, 5), Cover(null, new Range1D(-4, 0), new Range1D(2, 5)));
}
