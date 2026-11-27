// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A locator may return values past the ends of the range an axis shows (matplotlib's own locators do, and so may
/// one an application writes). A major tick is drawn, measured and kept room for only where the plot holds it, by the rule
/// its grid line and the minor ticks already follow (<see cref="AxisTickExtensions"/>' <c>PlaceOnPlot</c>, matplotlib's
/// <c>_interval_contains_close</c>): no mark, no label and no reserved width outside the plot, on every 2-D axis.</summary>
public class MajorTicksOnThePlotTests
{
    private static readonly Rect Plot = new(50, 40, 400, 300);

    /// <summary>One tick <paramref name="reach"/> before the range, its two ends, and one as far past it.</summary>
    private static TestTickLocator BeyondTheEnds(double reach = 1) => new((min, max) => [min - reach, min, max, max + reach]);

    private static string[] LabelsDrawn(Axes axes)
    {
        var ctx = axes.Drawn(Plot);
        return ctx.Of<DrawTextCall>().Select(c => c.Text).ToArray();
    }

    /// <summary>A tick of the X axis (the ends 0 and 2, and a tick beyond each of them) and one of the Y axis (10 and 20) that stands
    /// outside the range has no label; the ticks at the ends have.</summary>
    [Theory]
    [InlineData(true, "0", "2", "-1", "3")]
    [InlineData(false, "10", "20", "9", "21")]
    public void ATickOutsideTheRange_HasNoLabel(bool xAxis, string low, string high, string belowLow, string aboveHigh)
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax =>
        {
            var plotted = ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20);
            if (xAxis) plotted.SetXTickLocator(BeyondTheEnds());
            else plotted.SetYTickLocator(BeyondTheEnds());
        });

        var labels = LabelsDrawn(axes);

        Assert.Contains(low, labels);
        Assert.Contains(high, labels);
        Assert.DoesNotContain(belowLow, labels);
        Assert.DoesNotContain(aboveHigh, labels);
    }

    [Fact]
    public void ATickOfAYSlotOutsideItsRange_HasNoLabel()
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20)
            .WithAddRightYAxis(y2 => y2.Plot([0.0, 2.0], [500.0, 700.0]).SetYLim(500, 700)));
        axes.YAxes.Right[0].Axis.TickLocator = BeyondTheEnds(50);

        var labels = LabelsDrawn(axes);

        Assert.Contains("500", labels);
        Assert.Contains("700", labels);
        Assert.DoesNotContain("450", labels);
        Assert.DoesNotContain("750", labels);
    }

    [Fact]
    public void ATickOfTheTopAxisOutsideItsRange_HasNoLabel()
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20)
            .WithSecondaryXAxis(top => top.PlotXSecondary([100.0, 300.0], [10.0, 20.0]).SetXLim(100, 300)));
        axes.SecondaryXAxis!.TickLocator = BeyondTheEnds(50);

        var labels = LabelsDrawn(axes);

        Assert.Contains("100", labels);
        Assert.Contains("300", labels);
        Assert.DoesNotContain("50", labels);
        Assert.DoesNotContain("350", labels);
    }

    /// <summary>The mirrored ticks are the same ticks on the far edge, by the same rule: a value past the range is labelled
    /// on neither edge.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMirroredTickOutsideTheRange_HasNoLabel(bool alongX)
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax =>
        {
            ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20);
            if (alongX) ax.SetXTickLocator(BeyondTheEnds());
            else ax.SetYTickLocator(BeyondTheEnds());
        });
        var axis = alongX ? axes.XAxis : axes.YAxis;
        axis.MajorTicks = axis.MajorTicks with { Mirror = true };

        var labels = LabelsDrawn(axes);

        string[] inside = alongX ? ["0", "2"] : ["10", "20"];
        string[] outside = alongX ? ["-1", "3"] : ["9", "21"];
        foreach (var label in inside) Assert.Equal(2, labels.Count(l => l == label));
        foreach (var label in outside) Assert.DoesNotContain(label, labels);
    }

    /// <summary>A locator whose every value lies past the range draws no X tick label, so no band is kept for one below the
    /// plot, nor above it when the ticks are mirrored.</summary>
    [Fact]
    public void AnXAxisWithNoTickOnThePlot_KeepsNoBand()
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20)
            .SetXTickLocator(new TestTickLocator(-5.0, 7.0)));
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };

        var bands = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEndBands(default);

        Assert.Equal(0, bands.Bottom);
        Assert.Equal(0, bands.Top);
    }

    private static double[] XLabelRotations(Axes axes)
    {
        var ctx = axes.Drawn(Plot);
        double bottom = Plot.Y + Plot.Height;
        return ctx.Of<DrawTextCall>()
            .Where(a => (a.Position).Y > bottom && (a.Position).X >= Plot.X - 0.5)
            .Select(a => a.Rotation).ToArray();
    }

    /// <summary>A label that is not drawn does not turn the others: a tick a hair past the right end stands a fraction of a
    /// pixel from the last one, which would crowd it, but it is off the plot, so the two labels drawn stay upright.</summary>
    [Fact]
    public void ATickOutsideTheRange_DoesNotTurnTheLabelsOnThePlot()
    {
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20)
            .SetXTickLocator(new TestTickLocator(0.0, 2.0, 2.001)));

        var rotations = XLabelRotations(axes);

        Assert.Equal(2, rotations.Length);
        Assert.All(rotations, r => Assert.Equal(0, r));
    }

    /// <summary>More ticks than the positions kept on the stack: crowded labels still turn, by the same rule.</summary>
    [Fact]
    public void ManyCrowdedTicks_TurnTheLabels()
    {
        var many = Enumerable.Range(0, 201).Select(i => i / 100.0).ToArray();
        var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20)
            .SetXTickLocator(new TestTickLocator(many)));

        var rotations = XLabelRotations(axes);

        Assert.Equal(many.Length, rotations.Length);
        Assert.All(rotations, r => Assert.Equal(30, r));
    }

    /// <summary>A tick outside the plot draws no mark: three X ticks of five stand on the plot, so the bottom edge carries
    /// three marks, as many as with a locator that returns only those three.</summary>
    [Fact]
    public void AnXTickOutsideTheRange_HasNoMark()
    {
        static int MarksOnTheBottomEdge(ITickLocator locator)
        {
            var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20).SetXTickLocator(locator));
            axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Visible = false };
            var ctx = axes.Drawn(Plot);
            double bottom = Plot.Y + Plot.Height;
            return ctx.Of<DrawLineCall>().Count(c =>
            {
                var p1 = c.P1;
                var p2 = c.P2;
                return p1.X == p2.X && Math.Min(p1.Y, p2.Y) <= bottom && Math.Max(p1.Y, p2.Y) > bottom && Math.Max(p1.Y, p2.Y) < bottom + 20;
            });
        }

        Assert.Equal(MarksOnTheBottomEdge(new TestTickLocator(0.0, 1.0, 2.0)),
            MarksOnTheBottomEdge(new TestTickLocator(-1.0, 0.0, 1.0, 2.0, 3.0)));
    }

    /// <summary>A label the plot does not hold is kept no room: ticks far past either end stand out beside the plot no
    /// farther than the labels at its ends do without them, and a wide label past the top of a Y axis widens no band.</summary>
    [Fact]
    public void ATickOutsideTheRange_IsKeptNoRoom()
    {
        MatPlotLibNet.Rendering.Layout.SideOverhang Overhang(ITickLocator locator)
        {
            var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20).SetXTickLocator(locator));
            return new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEndBands(default).Overhang;
        }
        Assert.Equal(Overhang(new TestTickLocator(0.0, 2.0)), Overhang(new TestTickLocator(-100.0, 0.0, 2.0, 102.0)));

        double LeftBand(ITickLocator locator)
        {
            var axes = Plt.Create().WithSize(500, 400).OneAxes(ax => ax.Plot([0.0, 2.0], [10.0, 20.0]).SetXLim(0, 2).SetYLim(10, 20).SetYTickLocator(locator));
            return new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureSideBands().Left;
        }
        Assert.Equal(LeftBand(new TestTickLocator(10.0, 20.0)), LeftBand(new TestTickLocator(10.0, 20.0, 123456789.0)));
    }
}
