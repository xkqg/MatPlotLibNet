// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Text3DSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class Text3DSeriesTests
{
    private static readonly List<Text3DAnnotation> Annotations =
    [
        new(1, 2, 3, "A"),
        new(4, 5, 6, "B"),
        new(7, 8, 9, "C")
    ];

    /// <summary>Verifies that ComputeDataRange is derived from annotation positions for the text, and is 0..dim for each axis for the voxels.</summary>
    public static TheoryData<Func<ISeries>, double[]> TheDataRanges => new()
    {
        { () => new Text3DSeries(Annotations), [1, 7, 2, 8, 3, 9] },
        { () => new VoxelSeries(new bool[2, 3, 4]), [0, 2, 0, 3, 0, 4] },
    };

    [Theory]
    [MemberData(nameof(TheDataRanges))]
    public void ComputeDataRange_OfA3DSeries(Func<ISeries> make, double[] xMinXMaxYMinYMaxZMinZMax)
    {
        var range = make().ComputeDataRange(null!);
        Assert.Equal(xMinXMaxYMinYMaxZMinZMax, new double[] { Convert.ToDouble(range.XMin), Convert.ToDouble(range.XMax), Convert.ToDouble(range.YMin), Convert.ToDouble(range.YMax), Convert.ToDouble(range.ZMin), Convert.ToDouble(range.ZMax) });
    }

    /// <summary>Empty annotations must yield an all-null DataRangeContribution
    /// (the early-return arm of ComputeDataRange).</summary>
    [Fact]
    public void ComputeDataRange_EmptyAnnotations_ReturnsAllNull()
    {
        var series = new Text3DSeries([]);
        var range = series.ComputeDataRange(null!);
        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }
}
