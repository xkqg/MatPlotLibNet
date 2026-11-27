// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="VoxelSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class VoxelSeriesTests
{
    /// <summary>A grid with no cell along one of its three sides fills nothing and adds no range.</summary>
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void AGridWithNoCellAlongASide_AddsNoRange(int x, int y, int z)
    {
        Assert.Equal(default, new VoxelSeries(new bool[x, y, z]).ComputeDataRange(new TestAxesContext()));
    }
}
