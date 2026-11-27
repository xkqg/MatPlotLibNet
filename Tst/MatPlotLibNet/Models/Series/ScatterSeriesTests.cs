// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ScatterSeries"/> behaviour; what it starts from and the settings it stores are held by
/// <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class ScatterSeriesTests
{
    /// <summary>The marker style a caller reads or writes through <see cref="IHasMarkerStyle"/> is the scatter's own
    /// <see cref="ScatterSeries.Marker"/>: one setting, reached two ways.</summary>
    [Fact]
    public void TheMarkerStyleOfTheInterface_IsTheScattersMarker()
    {
        var scatter = new ScatterSeries([1.0], [2.0]);
        IHasMarkerStyle styled = scatter;

        styled.MarkerStyle = MarkerStyle.Square;

        Assert.Equal(MarkerStyle.Square, scatter.Marker);
        scatter.Marker = MarkerStyle.Diamond;
        Assert.Equal(MarkerStyle.Diamond, styled.MarkerStyle);
    }
}
