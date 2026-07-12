// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Covers the branches the v1.14 work introduced. The coverage gate found each of these the moment it
/// ran, which is what it is for — every arm below is one an operator could reach and nobody had walked.</summary>
public class V14BranchCoverageTests
{
    // ── BulletGraphSeriesRenderer ────────────────────────────────────────────

    /// <summary>A vertical bullet with neither bands nor a target still renders — the bar alone.</summary>
    [Fact]
    public void VerticalBullet_WithoutBandsOrTarget_RendersJustTheBar()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Orientation = Orientation.Vertical))
            .ToSvg();

        Assert.Contains("<rect", svg);
    }

    /// <summary>A horizontal bullet without bands renders the bar over the plain background.</summary>
    [Fact]
    public void HorizontalBullet_WithoutBands_Renders()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50))
            .ToSvg();

        Assert.Contains("<rect", svg);
    }

    /// <summary>A single band cannot be out of order, so no diagnostic is raised — the ascending check needs at
    /// least two thresholds to have an opinion.</summary>
    [Fact]
    public void ASingleBand_RaisesNoDiagnostic()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands = [new(100, Colors.Gray)]))
            .ToSvg();

        Assert.Contains("<rect", svg);
    }

    /// <summary>A zero-valued bullet renders: the upper bound falls back to 1 rather than collapsing the strip.</summary>
    [Fact]
    public void AZeroValuedBullet_StillRenders()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(0))
            .ToSvg();

        Assert.Contains("<svg", svg);
    }

    /// <summary>Bands in ascending order raise nothing — the happy path of the ascending check.</summary>
    [Fact]
    public void AscendingBands_RaiseNoDiagnostic()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands =
                [new(30, Colors.Gray), new(70, Colors.Gray), new(100, Colors.Gray)]))
            .ToSvg();

        Assert.Contains("<rect", svg);
    }

    // ── ContourfSeriesRenderer ───────────────────────────────────────────────

    /// <summary>A filled contour can hatch its bands — and a hatch array shorter than the band count leaves the
    /// remaining bands plain rather than throwing.</summary>
    [Fact]
    public void ContourFill_HatchesTheBandsItWasGiven_AndLeavesTheRestPlain()
    {
        double[] x = [0, 1, 2];
        double[] y = [0, 1, 2];
        double[,] z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(x, y, z, configure: s =>
                s.Hatches = [HatchPattern.ForwardDiagonal]))    // one hatch, many bands
            .ToSvg();

        Assert.Contains("<pattern", svg);
    }

    /// <summary>Without hatches a filled contour paints flat bands — no pattern machinery at all.</summary>
    [Fact]
    public void ContourFill_WithoutHatches_PaintsFlatBands()
    {
        double[] x = [0, 1, 2];
        double[] y = [0, 1, 2];
        double[,] z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(x, y, z))
            .ToSvg();

        Assert.DoesNotContain("<pattern", svg);
    }

    // ── AutoDateLocator ──────────────────────────────────────────────────────

    /// <summary>Calendar intervals — months and years — have no fixed length, so they thin by index. A window of
    /// many years does not slide frame by frame, so there is nothing to keep phase-stable.</summary>
    [Fact]
    public void ACalendarSpan_ThinsByIndex()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2000, 1, 1).ToOADate();
        var end = new DateTime(2030, 1, 1).ToOADate();

        double[] ticks = locator.Locate(start, end);

        Assert.NotEmpty(ticks);
        Assert.Equal(DateInterval.Years, locator.ChosenInterval);
    }

    /// <summary>A multi-year span in months also thins by index.</summary>
    [Fact]
    public void AMultiYearSpan_ChoosesMonths()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 1, 1).ToOADate();
        var end = new DateTime(2028, 6, 1).ToOADate();

        double[] ticks = locator.Locate(start, end);

        Assert.NotEmpty(ticks);
        Assert.Equal(DateInterval.Months, locator.ChosenInterval);
    }

    /// <summary>A span so long that the step runs off the end of the human ladder still produces round ticks —
    /// it rounds up to whole minutes rather than picking an arbitrary number.</summary>
    [Fact]
    public void AVeryLongSecondSpan_RoundsTheStepUpToWholeMinutes()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 0, 0).ToOADate();
        var end = new DateTime(2026, 7, 12, 14, 1, 59).ToOADate();   // 119 s — 6 labels wants a ~20 s step

        double[] ticks = locator.Locate(start, end);

        Assert.NotEmpty(ticks);
        foreach (double tick in ticks)
        {
            Assert.True(DateTime.FromOADate(tick).Millisecond < 2);
        }
    }

    /// <summary>A window with only a handful of ticks is left alone — nothing to thin.</summary>
    [Fact]
    public void AShortWindow_KeepsEveryTick()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 0, 0).ToOADate();
        var end = new DateTime(2026, 7, 12, 14, 0, 4).ToOADate();

        Assert.NotEmpty(locator.Locate(start, end));
    }
}
