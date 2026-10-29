// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A series from outside the library's hierarchy says nothing about its own legend parts, so the legend lists it by its
/// label as a filled rectangle, and not at all when it has none. The entry list is the one place that decides it.</summary>
public class LegendOfAForeignSeriesTests
{
    /// <summary>A series that is not a <see cref="ChartSeries"/>.</summary>
    private sealed class ForeignSeries : ISeries
    {
        public string? Label { get; set; }

        public bool Visible { get; set; } = true;

        public int ZOrder { get; set; }

        public void Accept(ISeriesVisitor visitor, RenderArea area)
        {
        }

        public DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

        public SeriesDto ToSeriesDto() => new() { Type = "foreign" };
    }

    [Fact]
    public void ANamedForeignSeries_HasOneEntry_UnderItsLabel_AsAFilledRectangle()
    {
        var axes = new Axes();
        axes.AddSeries(new ForeignSeries { Label = "outside" });

        var entry = Assert.Single(axes.LegendEntries(directLabelsDrawn: false));

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

        Assert.Empty(axes.LegendEntries(directLabelsDrawn: false));
    }
}
