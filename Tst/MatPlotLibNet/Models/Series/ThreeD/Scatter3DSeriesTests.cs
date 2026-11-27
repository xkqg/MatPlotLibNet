// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Scatter3DSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class Scatter3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];

    /// <summary>Verifies that ToSeriesDto includes ColorMapName when set.</summary>
    [Fact]
    public void ANamedColourMap_IsWrittenByItsName()
    {
        var series = new Scatter3DSeries(X, Y, Z) { ColorMap = ColorMaps.Viridis };
        var dto = series.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }
}
