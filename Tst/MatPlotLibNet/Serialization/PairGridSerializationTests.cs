// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A pair grid read from a record that names no variables is refused, as its constructor refuses one. Its settings and
/// its data travel under the drawn-property contract and the GivenSeries pins.</summary>
public class PairGridSerializationTests
{
    // ── Empty-Variables DTO must throw (matches model invariant) ──────────────

    [Fact]
    public void Deserialize_EmptyVariablesDto_Throws()
    {
        // Hand-crafted JSON with a "pairgrid" type but null/missing variables must
        // be rejected — the constructor invariant requires ≥1 variable.
        var axes = new MatPlotLibNet.Models.Axes();
        var dto = new SeriesDto { Type = "pairgrid", Variables = null };
        var ex = Assert.Throws<InvalidOperationException>(
            () => MatPlotLibNet.Serialization.SeriesRegistry.Create("pairgrid", axes, dto));
        Assert.Contains("Variables", ex.Message);
    }
}
