// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The extent of bars that stand in rows: from half a bar below the first row to half a bar above the last.</summary>
public sealed class BarHeightExtensionsTests
{
    /// <summary>Bars stand half their height beyond the first and the last row (a bar as tall as a row fills the rows' slots), a negative
    /// height reaches the same way, and no rows are taken as one, so the extent never runs backwards.</summary>
    [Theory]
    [InlineData(0.6, 4, -0.3, 3.3)]
    [InlineData(1.0, 3, -0.5, 2.5)]
    [InlineData(-0.8, 2, -0.4, 1.4)]
    [InlineData(0.6, 0, -0.3, 0.3)]
    public void Bars_StandHalfTheirHeightBeyondTheFirstAndTheLastRow(double height, int rows, double lo, double hi)
    {
        var extent = height.AcrossRows(rows);

        Assert.Equal(lo, extent.Lo, 12);
        Assert.Equal(hi, extent.Hi, 12);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AHeightThatIsNotANumber_LeavesTheRowsTheirPlaces(double height)
    {
        var extent = height.AcrossRows(3);

        Assert.Equal(0, extent.Lo);
        Assert.Equal(2, extent.Hi);
    }
}
