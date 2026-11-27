// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A series from outside the library's hierarchy says nothing about its own legend parts, so the legend lists it by its
/// label as a filled rectangle, and not at all when it has none. The entry list is the one place that decides it.</summary>
public class LegendOfAForeignSeriesTests
{
    [Fact]
    public void ANamedForeignSeries_HasOneEntry_UnderItsLabel_AsAFilledRectangle()
    {
        var axes = new Axes();
        axes.AddSeries(new ForeignSeries { Label = "outside" });

        var entry = Assert.Single(axes.LegendEntries(Theme.Default, directLabelsDrawn: false));

        Assert.Equal("outside", entry.Label);
        Assert.Equal(LegendSwatchKind.Patch, entry.Swatch.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AForeignSeriesWithoutAName_HasNoEntry(string? label)
    {
        var axes = new Axes();
        axes.AddSeries(new ForeignSeries { Label = label });

        Assert.Empty(axes.LegendEntries(Theme.Default, directLabelsDrawn: false));
    }
}
