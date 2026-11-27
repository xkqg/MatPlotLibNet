// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

public class DatasetSeriesTests
{
    private static readonly double[][] Data = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];

    [Fact]
    public void StripplotSeries_ComputeDataRange_InheritedFromBase()
    {
        var series = new StripplotSeries(Data);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(-0.5, range.XMin);
        Assert.Equal(2.5, range.XMax);  // 3 datasets → Length - 0.5
        Assert.Equal(1, range.YMin);
        Assert.Equal(9, range.YMax);
    }

    /// <summary>A kind of series of datasets as a row: how it is made over datasets at positions, the values of a dataset it draws, and half
    /// of the room one of its datasets takes along X.</summary>
    /// <param name="Name">What the series is called in a failure.</param>
    /// <param name="Make">Makes the series over the datasets, at the positions.</param>
    /// <param name="Body">The values of a dataset the series draws.</param>
    /// <param name="Half">Half of the room a dataset takes along X, at its position.</param>
    public readonly record struct DatasetKind(string Name, Func<double[][], double[], ChartSeries> Make, double[] Body, double Half)
    {
        /// <inheritdoc />
        public override string ToString() => Name;
    }

    /// <summary>The two series that draw datasets: a box and a violin (its body only, the extrema ticks off).</summary>
    public static TheoryData<DatasetKind> Kinds { get; } = new()
    {
        new DatasetKind("box", (datasets, positions) => new BoxSeries(datasets) { Positions = positions }, [1.0, 2, 3], 0.25),
        new DatasetKind("violin", (datasets, positions) => new ViolinSeries(datasets) { Positions = positions, ShowExtrema = false }, [5.0, 5, 5, 5], 0.5),
    };

    private static IAxesContext NoLimits => new TestAxesContext();

    /// <summary>A dataset with nothing to draw is not drawn, so it makes no room, wherever it is placed.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ComputeDataRange_WithPositions_MakesNoRoomForADatasetThatDrawsNothing(DatasetKind kind)
    {
        var range = kind.Make([kind.Body, [double.NaN]], [3, 90]).ComputeDataRange(NoLimits);

        Assert.Equal(3 - kind.Half, range.XMin!.Value, 12);
        Assert.Equal(3 + kind.Half, range.XMax!.Value, 12);
    }

    /// <summary>A series that places datasets and has none to draw contributes nothing along X.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ComputeDataRange_WithPositionsAndNothingToDraw_ContributesNoX(DatasetKind kind)
    {
        var range = kind.Make([[double.NaN]], [3]).ComputeDataRange(NoLimits);

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>A dataset the position array does not reach stands at its index, as it is drawn.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ComputeDataRange_WithAShortPositionsArray_PlacesTheRestAtTheirIndex(DatasetKind kind)
    {
        var range = kind.Make([kind.Body, kind.Body], [10]).ComputeDataRange(NoLimits);

        Assert.Equal(1 - kind.Half, range.XMin!.Value, 12);
        Assert.Equal(10 + kind.Half, range.XMax!.Value, 12);
    }
}
