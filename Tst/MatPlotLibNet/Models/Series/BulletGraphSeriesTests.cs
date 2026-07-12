// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

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

    // ── Model ────────────────────────────────────────────────────────────────

    /// <summary>The value is the series' identity: it goes in the constructor, everything else is optional.</summary>
    [Fact]
    public void Value_IsSetByTheConstructor()
    {
        Assert.Equal(2412, new BulletGraphSeries(2412).Value);
    }

    /// <summary>Without a target there is no comparative tick — the bar alone is a legitimate, if weaker, form.</summary>
    [Fact]
    public void Target_DefaultsToNull()
    {
        Assert.Null(new BulletGraphSeries(2412).Target);
    }

    /// <summary>Bands default to none: a bullet graph with no qualitative ranges is still a bullet graph.</summary>
    [Fact]
    public void Bands_DefaultToNull()
    {
        Assert.Null(new BulletGraphSeries(2412).Bands);
    }

    /// <summary>Horizontal by default — the reading direction of the strip an operator scans down a column of.</summary>
    [Fact]
    public void Orientation_DefaultsToHorizontal()
    {
        Assert.Equal(Orientation.Horizontal, new BulletGraphSeries(2412).Orientation);
    }

    // ── Data range ───────────────────────────────────────────────────────────

    /// <summary>The data range spans zero to the largest of value, target and the outermost band, so the whole
    /// comparison is on screen without the caller computing limits.</summary>
    [Fact]
    public void DataRange_CoversValueTargetAndBands()
    {
        var s = new BulletGraphSeries(2412) { Target = 2500, Bands = Bands };

        var range = s.ComputeDataRange(new NullContext());

        Assert.Equal(0, range.XMin);
        Assert.Equal(2800, range.XMax);   // the outermost band, beyond both value and target
    }

    /// <summary>A value beyond every band still fits: the range follows the data, not the decoration.</summary>
    [Fact]
    public void DataRange_FollowsAValueBeyondTheBands()
    {
        var s = new BulletGraphSeries(3500) { Bands = Bands };

        Assert.Equal(3500, s.ComputeDataRange(new NullContext()).XMax);
    }

    /// <summary>Vertical orientation puts the same range on Y instead of X.</summary>
    [Fact]
    public void DataRange_SwapsAxesWhenVertical()
    {
        var s = new BulletGraphSeries(2412) { Orientation = Orientation.Vertical };

        var range = s.ComputeDataRange(new NullContext());

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
        ChartDiagnostic? seen = null;
        void Capture(ChartDiagnostic d) => seen = d;
        ChartDiagnostics.Emitted += Capture;
        try
        {
            Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b => b.Bands =
                [
                    new(80, Colors.Gray),
                    new(20, Colors.Gray)      // lower than its predecessor
                ]))
                .ToSvg();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Capture;
        }

        Assert.NotNull(seen);
        Assert.Contains("ascending", seen!.Value.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    /// <summary>The three layered parts reach the canvas: the qualitative bands behind, the feature bar over
    /// them, and the target as a perpendicular tick.</summary>
    [Fact]
    public void Rendering_DrawsBandsBarAndTargetTick()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(2412, b =>
            {
                b.Target = 2500;
                b.Bands = Bands;
            }))
            .ToSvg();

        Assert.Contains("<rect", svg);                      // bands + bar
        Assert.Contains("<line", svg);                      // the target tick
        Assert.Contains(Bands[0].Color.ToHex(), svg);
    }

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

        Assert.Equal(CountOf(without, "<line") + 1, CountOf(withTarget, "<line"));
    }

    /// <summary>A vertical bullet renders too — same three parts, rotated.</summary>
    [Fact]
    public void VerticalOrientation_Renders()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(50, b =>
            {
                b.Orientation = Orientation.Vertical;
                b.Target = 80;
                b.Bands = [new(100, Colors.Gray)];
            }))
            .ToSvg();

        Assert.Contains("<rect", svg);
        Assert.Contains("<line", svg);
    }

    // ── Serialization ────────────────────────────────────────────────────────

    /// <summary>Every property survives the round-trip — including the bands, which the gauge used to lose.</summary>
    [Fact]
    public void EveryProperty_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bullet(2412, b =>
            {
                b.Label = "Messages/s";
                b.Target = 2500;
                b.Bands = Bands;
                b.BarColor = Colors.Black;
                b.Orientation = Orientation.Vertical;
            }))
            .Build();

        var serializer = new ChartSerializer();
        var s = serializer.FromJson(serializer.ToJson(figure))
            .SubPlots[0].Series.OfType<BulletGraphSeries>().Single();

        Assert.Equal(2412, s.Value);
        Assert.Equal("Messages/s", s.Label);
        Assert.Equal(2500, s.Target);
        Assert.Equal(Bands, s.Bands);
        Assert.Equal(Colors.Black, s.BarColor);
        Assert.Equal(Orientation.Vertical, s.Orientation);
    }

    /// <summary>Minimal axes context: the bullet graph's range depends only on its own value, target and
    /// bands — it never consults the axes.</summary>
    private sealed class NullContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    private static int CountOf(string haystack, string needle)
    {
        int count = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }
}
