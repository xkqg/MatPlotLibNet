// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BulletGraphSeries"/> — Stephen Few's designed replacement for the radial dial.
/// <para>A dial spends a quarter of a panel to say what a bar says in a fifth of it, which is why the
/// high-performance-HMI literature rejects it outright. The bullet graph keeps the three things an operator
/// actually needs — where the value is, where it should be, and which range that falls in — in one thin strip
/// that can be stacked twenty deep without the screen turning into a cockpit.</para></summary>
public class BulletGraphSeriesTests
{
    private static readonly GaugeBand[] Bands =
    [
        new(1800, Color.FromHex("#DDDDDD")),
        new(2200, Color.FromHex("#BBBBBB")),
        new(2800, Color.FromHex("#999999"))
    ];

    // ── Data range ───────────────────────────────────────────────────────────

    /// <summary>The data range spans zero to the largest of value, target and the outermost band, so the whole
    /// comparison is on screen without the caller computing limits.</summary>
    [Fact]
    public void DataRange_CoversValueTargetAndBands()
    {
        var s = new BulletGraphSeries(2412) { Target = 2500, Bands = Bands };

        var range = s.ComputeDataRange(new TestAxesContext());

        Assert.Equal(0, range.XMin);
        Assert.Equal(2800, range.XMax);   // the outermost band, beyond both value and target
    }

    /// <summary>A value beyond every band still fits: the range follows the data, not the decoration.</summary>
    [Fact]
    public void DataRange_FollowsAValueBeyondTheBands()
    {
        var s = new BulletGraphSeries(3500) { Bands = Bands };

        Assert.Equal(3500, s.ComputeDataRange(new TestAxesContext()).XMax);
    }

    /// <summary>Vertical orientation puts the same range on Y instead of X.</summary>
    [Fact]
    public void DataRange_SwapsAxesWhenVertical()
    {
        var s = new BulletGraphSeries(2412) { Orientation = Orientation.Vertical };

        var range = s.ComputeDataRange(new TestAxesContext());

        Assert.Equal(0, range.YMin);
        Assert.Equal(2412, range.YMax);
        Assert.Null(range.XMin);
    }

    // ── Diagnostics ──────────────────────────────────────────────────────────

    /// <summary>Bands out of ascending order are a caller mistake that would silently paint nonsense — a later
    /// band would overpaint an earlier one and the ranges would read backwards. The series degrades to drawing
    /// them as given, but says so on the diagnostics channel rather than quietly lying.</summary>
    [Fact]
    public void BandsOutOfOrder_RaiseADiagnostic()
    {
        using var capture = new DiagnosticsCapture();
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands =
            [
                new(80, Colors.Gray),
                new(20, Colors.Gray)      // lower than its predecessor
            ]))
            .ToSvg();

        Assert.NotNull(capture.Last);
        Assert.Contains("ascending", capture.Last!.Value.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  The three layered parts reach the canvas: the qualitative bands behind, the feature bar over them, and the target as a
        // perpendicular tick.
        //  bands + bar
        //  the target tick
        new DrawnToken("Rendering_DrawsBandsBarAndTargetTick", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(2412, b =>
            {
                b.Target = 2500;
                b.Bands = Bands;
            }))
            .ToSvg(), "<rect", "<line", Bands[0].Color.ToHex()),
        //  A vertical bullet renders too — same three parts, rotated.
        new DrawnToken("VerticalOrientation_Renders", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b =>
            {
                b.Orientation = Orientation.Vertical;
                b.Target = 80;
                b.Bands = [new(100, Colors.Gray)];
            }))
            .ToSvg(), "<rect", "<line"),
        // A vertical bullet with neither bands nor a target still renders — the bar alone.
        new DrawnToken("VerticalBullet_WithoutBandsOrTarget_RendersJustTheBar", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Orientation = Orientation.Vertical))
            .ToSvg(), "<rect"),
        // A horizontal bullet without bands renders the bar over the plain background.
        new DrawnToken("HorizontalBullet_WithoutBands_Renders", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50))
            .ToSvg(), "<rect"),
        // A single band cannot be out of order, so no diagnostic is raised — the ascending check needs at least two thresholds to have an
        // opinion.
        new DrawnToken("ASingleBand_RaisesNoDiagnostic", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands = [new(100, Colors.Gray)]))
            .ToSvg(), "<rect"),
        // Bands in ascending order raise nothing — the happy path of the ascending check.
        new DrawnToken("AscendingBands_RaiseNoDiagnostic", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands =
                [new(30, Colors.Gray), new(70, Colors.Gray), new(100, Colors.Gray)]))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Without a target no tick is drawn — the library does not invent a comparative the caller never
    /// supplied. Both figures are given the SAME upper bound (target == value), so the axis geometry is
    /// identical and the only difference in the output is the tick itself.</summary>
    [Fact]
    public void WithoutATarget_NoTickIsDrawn()
    {
        string withTarget = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Target = 50)).ToSvg();
        string without = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50)).ToSvg();

        Assert.Equal(without.CountOf("<line") + 1, withTarget.CountOf("<line"));
    }

    // ── a last band at +Infinity: "and everything above" ─────────────────────────────────────────────────────

    /// <summary>A band whose threshold is +Infinity is kept, and it runs to the end of the strip: the way to say that the last band
    /// covers everything above the one before it. The infinite threshold sizes nothing, so the strip is as long as the value and the
    /// target ask (the Bands documentation says so).</summary>
    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ALastBandAtPositiveInfinity_RunsToTheEndOfTheStrip_WhichTheValueSizes(Orientation orientation)
    {
        var series = new BulletGraphSeries(60)
        {
            Bands = [new GaugeBand(50, Colors.LightGray), new GaugeBand(double.PositiveInfinity, Colors.Gray)],
            Orientation = orientation,
        };
        var bounds = new Rect(0, 0, 400, 300);
        var ctx = new RecordingRenderContext();

        new BulletGraphSeriesRenderer(
            new SeriesRenderContext(new DataTransform(0, 100, 0, 100, bounds), ctx, Colors.Tab10Blue,
                new RenderArea(bounds, ctx))).Render(series);

        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal((double?)60, orientation == Orientation.Horizontal ? range.XMax : range.YMax);
        var bands = ctx.Rects().Take(2).ToList();
        if (orientation == Orientation.Horizontal)
        {
            Assert.Equal(50.0 / 60 * 400, bands[0].Width, 1e-9);                  // the finite band ends at its threshold, 50 of the 60 the strip spans
            Assert.Equal(400, bands[1].X + bands[1].Width, 1e-9);                 // the infinite one reaches the strip's end
        }
        else
        {
            Assert.Equal(50.0 / 60 * 300, bands[0].Height, 1e-9);
            Assert.Equal(0, bands[1].Y, 1e-9);                                    // ... the strip's top, bottom up
        }
    }
}
