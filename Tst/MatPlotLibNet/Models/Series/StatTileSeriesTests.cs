// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StatTileSeries"/> — a single-value headline "stat tile" (big number + label),
/// built for compact dashboard KPIs. No axes/data; the value and label render as centred text.</summary>
public class StatTileSeriesTests
{
    [Fact]
    public void Ctor_StoresValue() => Assert.Equal(12, new StatTileSeries(12).Value);

    [Fact]
    public void AccentColor_IsSettable()
    {
        var s = new StatTileSeries(1) { AccentColor = Colors.Tab10Blue };
        Assert.Equal(Colors.Tab10Blue, s.AccentColor);
    }

    [Fact]
    public void Format_DefaultsToTwoDecimals_AndIsHonoured()
    {
        Assert.Equal("0.##", new StatTileSeries(1).Format);
        Assert.Equal("42", new StatTileSeries(42) { Format = "0" }.FormattedValue);
    }

    [Fact]
    public void ComputeDataRange_ContributesNothing()
    {
        var r = new StatTileSeries(1).ComputeDataRange(null!);
        Assert.False(r.XMin.HasValue);
        Assert.False(r.YMax.HasValue);
    }

    [Fact]
    public void ToSeriesDto_CarriesTypeValueAndColor()
    {
        var dto = new StatTileSeries(7) { AccentColor = Colors.Tab10Blue }.ToSeriesDto();
        Assert.Equal("stattile", dto.Type);
        Assert.Equal(7, dto.GaugeValue);
        Assert.Equal(Colors.Tab10Blue, dto.Color);
    }

    [Fact]
    public void Render_DrawsTheValueAndLabel_AsText()
    {
        var svg = Plt.Create().StatTile(42, t => t.Label = "Participants").ToSvg();
        Assert.Contains("42", svg);
        Assert.Contains("Participants", svg);
    }

    [Fact]
    public void Render_WithoutALabel_DrawsOnlyTheValue()
    {
        var svg = Plt.Create().StatTile(7).ToSvg(); // no Label set — the label branch is skipped, no crash
        Assert.Contains("7", svg);
    }

    [Fact]
    public void Registry_DeserializesFromDto_RestoringValueAndColor()
    {
        var dto = new StatTileSeries(9) { AccentColor = Colors.Tab10Blue }.ToSeriesDto();
        var s = (StatTileSeries)SeriesRegistry.Create("stattile", new Axes(), dto)!;
        Assert.Equal(9, s.Value);
        Assert.Equal(Colors.Tab10Blue, s.AccentColor);
    }
}
