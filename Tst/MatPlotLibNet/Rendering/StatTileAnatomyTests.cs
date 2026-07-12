// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies the stat tile's full anatomy: value, target, gap and trend.
/// <para>A bare big number is a failed dashboard pattern. "68%" tells a reader nothing — without a
/// comparative they must supply the missing context from memory, and mostly they cannot. So the tile carries
/// four things: the value, the target it is measured against, the gap between them stated in words, and a
/// trend that says which way it is moving. The gap is the important one: it answers "is this good or bad"
/// without anyone doing arithmetic on a wall.</para></summary>
public class StatTileAnatomyTests
{
    private static readonly double[] Trend = [4.0, 4.5, 5.0, 6.0, 8.0, 12.0];

    /// <summary>A tile with a trend draws the sparkline inside its own area — an extra polyline that a plain
    /// tile does not have.</summary>
    [Fact]
    public void ATileWithATrend_DrawsASparkline()
    {
        string plain = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(12.0)).ToSvg();
        string withTrend = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(12.0, s => s.Trend = Trend)).ToSvg();

        Assert.DoesNotContain("<polyline", plain);
        Assert.Contains("<polyline", withTrend);
    }

    /// <summary>The trend is a Tufte sparkline: no axis, no frame, no ticks. It contributes nothing to the
    /// axes' data range, so a tile's headline scale is never dragged around by its own history.</summary>
    [Fact]
    public void TheTrend_DoesNotContributeToTheDataRange()
    {
        var tile = new StatTileSeries(12.0) { Trend = Trend };

        var range = tile.ComputeDataRange(new NullContext());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    /// <summary>The caption — the gap line — is drawn beneath the value.</summary>
    [Fact]
    public void TheCaption_IsRendered()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(28.1, s =>
            {
                s.Label = "RFx p99";
                s.Caption = "target 25 ms · +3.1 over";
            }))
            .ToSvg();

        Assert.Contains("target 25 ms", svg);
        Assert.Contains("RFx p99", svg);
    }

    /// <summary>A hatched tile reads as "no information" — the source has gone silent, and that is a different
    /// fault from a bad value. It is a pattern, never a colour.</summary>
    [Fact]
    public void AHatchedTile_RendersAPattern()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(0, s => s.Hatch = HatchPattern.ForwardDiagonal))
            .ToSvg();

        Assert.Contains("<pattern", svg);
    }

    /// <summary>A tile at rest carries no colour from the series cycle — the neutral shade of the theme's
    /// alarm palette. Colour is reserved for what needs attention; a wall of cheerful tiles leaves the one
    /// that matters nothing to stand out against.</summary>
    [Fact]
    public void ATileAtRest_WearsTheNeutralShade_NotACycleColour()
    {
        string svg = Plt.Create()
            .WithTheme(Theme.OpsNight)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42))
            .ToSvg();

        Assert.Contains(Theme.OpsNight.Alarm.Resting.ToHex(), svg);
        Assert.DoesNotContain(Theme.OpsNight.CycleColors[0].ToHex(), svg);
    }

    /// <summary>The whole anatomy survives a round-trip: target, caption, trend and hatch all persist.</summary>
    [Fact]
    public void TheWholeAnatomy_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(28.1, s =>
            {
                s.Target = 25;
                s.Caption = "target 25 ms · +3.1 over";
                s.Trend = Trend;
                s.Hatch = HatchPattern.BackDiagonal;
            }))
            .Build();

        var serializer = new ChartSerializer();
        var tile = serializer.FromJson(serializer.ToJson(figure))
            .SubPlots[0].Series.OfType<StatTileSeries>().Single();

        Assert.Equal(25, tile.Target);
        Assert.Equal("target 25 ms · +3.1 over", tile.Caption);
        Assert.Equal(Trend, tile.Trend);
        Assert.Equal(HatchPattern.BackDiagonal, tile.Hatch);
    }

    private sealed class NullContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
