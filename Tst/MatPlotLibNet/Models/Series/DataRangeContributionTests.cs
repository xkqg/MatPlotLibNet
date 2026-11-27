// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A range whose data fill it to every edge (a grid, a mesh, a field, the cells of a matrix) is pinned on all four sides by the
/// one factory: the range and its sticky edges are the same four numbers, so no margin reaches past the data.</summary>
public class DataRangeContributionTests
{
    [Fact]
    public void APinnedRange_IsStickyAtEachOfItsFourEdges()
    {
        var pinned = DataRangeContribution.Pinned(new Range1D(-2, 3.5), new Range1D(0.25, 9));

        Assert.Equal(new DataRangeContribution(-2, 3.5, 0.25, 9, StickyXMin: -2, StickyXMax: 3.5, StickyYMin: 0.25, StickyYMax: 9), pinned);
    }

    /// <summary>An axis with nothing drawable on it adds nothing to that axis and pins nothing there.</summary>
    [Fact]
    public void AnAxisWithNothingDrawable_AddsAndPinsNothingThere()
    {
        var pinned = DataRangeContribution.Pinned(null, new Range1D(1, 2));

        Assert.Equal(new DataRangeContribution(null, null, 1, 2, StickyYMin: 1, StickyYMax: 2), pinned);
    }

    /// <summary>The cells of a matrix span 0 to the number of columns and 0 to the number of rows, pinned; a matrix with no row or no
    /// column adds nothing.</summary>
    [Theory]
    [InlineData(3, 4)]
    [InlineData(1, 1)]
    public void TheCellsOfAMatrix_ArePinnedFromZeroToTheirCount(int rows, int cols)
    {
        Assert.Equal(DataRangeContribution.Pinned(new Range1D(0, cols), new Range1D(0, rows)), DataRangeContribution.OfCells(new double[rows, cols]));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(3, 0)]
    public void AMatrixWithNoCell_AddsNothing(int rows, int cols)
    {
        Assert.Equal(new DataRangeContribution(null, null, null, null), DataRangeContribution.OfCells(new double[rows, cols]));
    }

    /// <summary>A range that pins no edge opens each flat axis by half a unit, as the axis would, and leaves a spread one as it is.</summary>
    [Fact]
    public void AnOpenedRange_OpensAFlatAxisByHalfAUnit_AndPinsNothing()
    {
        Assert.Equal(new DataRangeContribution(1.5, 2.5, -1, 4), DataRangeContribution.Opened(new Range1D(2, 2), new Range1D(-1, 4)));
    }

    /// <summary>The points of a surface (a mesh, a triangulation, a field's grid) pin the axes to their drawable extent: a missing value
    /// and an infinity are left out, and an axis with no drawable point adds and pins nothing.</summary>
    [Fact]
    public void ThePointsOfASurface_PinTheAxesToTheirDrawableExtent()
    {
        Assert.Equal(DataRangeContribution.Pinned(new Range1D(-1, 3), new Range1D(2, 5)),
            DataRangeContribution.PinnedOver([3, double.NaN, -1, double.PositiveInfinity], [2, 5]));
        Assert.Equal(DataRangeContribution.Pinned(null, new Range1D(1, 1)), DataRangeContribution.PinnedOver([], [1]));
    }

    /// <summary>A range spanned along X and Y pins no edge, and an axis with nothing drawable adds nothing there; over points, a missing
    /// value and an infinity are left out.</summary>
    [Fact]
    public void ASpannedRange_PinsNoEdge_AndOverPointsLeavesOutWhatCannotBeDrawn()
    {
        Assert.Equal(new DataRangeContribution(null, null, 1, 2), DataRangeContribution.Spanning(null, new Range1D(1, 2)));
        Assert.Equal(new DataRangeContribution(0, 1, null, null, ZMin: -3, ZMax: 4),
            DataRangeContribution.Spanning(new Range1D(0, 1), null, new Range1D(-3, 4)));
        Assert.Equal(new DataRangeContribution(-1, 3, 2, 5),
            DataRangeContribution.Over([3, double.NaN, -1], [2, double.PositiveInfinity, 5]));
        Assert.Equal(new DataRangeContribution(null, null, null, null), DataRangeContribution.Over([], []));
    }

    /// <summary>A mesh and a triangulation with no point range their axes from 0 to 1 on both, the library's default for a series with no
    /// data; with points they pin the axes to them.</summary>
    [Fact]
    public void AMeshAndATriangulation_RangeTheLibrarysDefaultWithNoPoint_ElsePinToTheirPoints()
    {
        var noData = new DataRangeContribution(0, 1, 0, 1);
        var pinned = DataRangeContribution.Pinned(new Range1D(1, 3), new Range1D(-2, 4));

        Assert.Equal(noData, new PcolormeshSeries(new Vec([]), new Vec([1.0, 2.0]), new double[0, 0]).ComputeDataRange(null!));
        Assert.Equal(noData, new TricontourSeries(new Vec([]), new Vec([]), new Vec([])).ComputeDataRange(null!));
        Assert.Equal(pinned, new PcolormeshSeries(new Vec([1.0, 3.0]), new Vec([-2.0, 4.0]), new double[1, 1]).ComputeDataRange(null!));
        Assert.Equal(pinned, new TricontourSeries(new Vec([1.0, 3.0, 2.0]), new Vec([-2.0, 4.0, 0.0]), new Vec([0.0, 1.0, 2.0])).ComputeDataRange(null!));
    }

    /// <summary>A triangulation coloured by its faces and a field of barbs with no point range their axes from 0 to 1 on both, the
    /// library's default for a series with no data; with points they span them, pinning no edge.</summary>
    [Fact]
    public void ATripcolorAndBarbs_RangeTheLibrarysDefaultWithNoPoint_ElseSpanTheirPoints()
    {
        var noData = new DataRangeContribution(0, 1, 0, 1);
        var spanned = new DataRangeContribution(1, 3, -2, 4);
        double[] none = [];

        Assert.Equal(noData, new TripcolorSeries(new Vec(none), new Vec(none), new Vec(none)).ComputeDataRange(null!));
        Assert.Equal(noData, new BarbsSeries(none, none, none, none).ComputeDataRange(null!));
        Assert.Equal(spanned, new TripcolorSeries(new Vec([1.0, 3.0, 2.0]), new Vec([-2.0, 4.0, 0.0]), new Vec([0.0, 1.0, 2.0])).ComputeDataRange(null!));
        Assert.Equal(spanned, new BarbsSeries(new Vec([1.0, 3.0]), new Vec([-2.0, 4.0]), new Vec([1.0, 1.0]), new Vec([0.0, 0.0])).ComputeDataRange(null!));
    }
}
