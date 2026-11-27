// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series keeps its name on whichever axis it is plotted: on every Y axis of the container and on the top X
/// axis it is named exactly once, in the legend or, when it asked for one, by an end label at the end of its line.</summary>
public class YAxisSlotIdentityTests
{
    private static readonly double[] X = [0.0, 1, 2, 3];
    private static readonly Rect Plot = new(200, 50, 400, 300);

    private static int NameCount(string svg, string name) => svg.Elements("text").Count(text => text.Text == name);

    [Fact]
    public void ALeftSlotSeries_WithDirectLabel_KeepsItsName()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2, 3, 4], s => s.Label = "P")
                .WithAddLeftYAxis(left => left.Plot(X, [10.0, 20, 30, 40], s => s.Label = "L0line").WithDirectLabel())
                .WithLegend())
            .Build().ToSvg();

        Assert.Equal(1, NameCount(svg, "L0line"));
    }

    /// <summary>A line ends at the right of the plot whatever axis it is measured on, so its end label stands in the one
    /// column of end labels right of the plot, past the right axis stack, at the height its OWN axis puts the end of the
    /// line at: the same column a right-axis line's label stands in.</summary>
    [Fact]
    public void ALeftSlotSeries_WithDirectLabel_DrawsAnEndLabelAtItsLineEnd_InTheColumnPastTheRightAxisStack()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2, 3, 4]);
        var right = axes.YAxes.Right.Add();
        right.Axis.Label = "Right";
        var rightLine = right.AddSeries(new LineSeries(X, [10.0, 20, 30, 90]) { Label = "R0line", LabelPlacement = SeriesLabelPlacement.LineEnd });
        var left = axes.YAxes.Left.Add();
        var leftLine = left.AddSeries(new LineSeries(X, [90.0, 50, 20, 10]) { Label = "L0line", LabelPlacement = SeriesLabelPlacement.LineEnd });
        var ctx = axes.Drawn(Plot);

        var calls = ctx.Calls;
        int labelAt = calls.FindIndex(c => c is DrawTextCall { Text: "L0line" });
        Assert.True(labelAt >= 0, "the left-axis line has no end label");
        var label = (DrawTextCall)calls[labelAt];
        // Drawn right after its own line, inside its series' turn, at the height of that line's last point.
        var line = calls.Take(labelAt).OfType<DrawLinesCall>().Last().Points;
        var font = label.Font;
        Assert.Equal(line[^1].Y + font.Size * 0.35, label.Position.Y, 6);
        // In the same column as the right-axis line's label, past the right axis stack.
        var rightLabel = ctx.Of<DrawTextCall>().Single(c => c.Text == "R0line");
        Assert.Equal(rightLabel.Position.X, label.Position.X, 6);
        Assert.True(label.Position.X > Plot.X + Plot.Width + DirectLabelMetrics.Gap);
        Assert.NotNull(rightLine);
        Assert.NotNull(leftLine);
    }

    [Fact]
    public void ALeftSlotSeries_AppearsInTheLegend()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2, 3, 4], s => s.Label = "P")
                .WithAddLeftYAxis(left => left.Plot(X, [10.0, 20, 30, 40], s => s.Label = "L0line"))
                .WithLegend())
            .Build();
        var axes = figure.SubPlots[0];

        var entry = Assert.Single(axes.LegendEntries(Theme.Default, directLabelsDrawn: true), e => e.Label == "L0line");
        Assert.Equal(axes.AllSeries.ToList().IndexOf(entry.Series), entry.OriginalIndex);
        Assert.Equal(1, NameCount(figure.ToSvg(), "L0line"));
    }

    [Fact]
    public void ATopXSeries_WithALabel_AppearsInTheLegend()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2, 3, 4], s => s.Label = "P")
                .WithAddRightYAxis(right => right.Plot(X, [10.0, 20, 30, 40], s => s.Label = "R0line"))
                .WithSecondaryXAxis(top => top.PlotXSecondary([100.0, 200, 300, 400], [1.0, 3, 2, 4], s => s.Label = "TopLine"))
                .WithLegend())
            .Build();
        var axes = figure.SubPlots[0];

        var entries = axes.LegendEntries(Theme.Default, directLabelsDrawn: true);
        var top = Assert.Single(entries, e => e.Label == "TopLine");
        Assert.Same(axes.XSecondarySeries[0], top.Series);
        // Its index is its place in the subplot's identity order: the primary's, then every Y slot's, then the top axis'.
        Assert.Equal(axes.AllSeries.ToList().IndexOf(top.Series), top.OriginalIndex);
        Assert.Equal(["P", "R0line", "TopLine"], entries.Select(e => e.Label));
        Assert.Equal(1, NameCount(figure.ToSvg(), "TopLine"));
    }

    /// <summary>A top-axis series that asked for an end label keeps its legend entry: end labels are drawn for the lines of
    /// the Y axes, so its name would otherwise be drawn nowhere.</summary>
    [Fact]
    public void ATopXSeries_WithALineEndPlacement_KeepsItsLegendEntry()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(new LineSeries(X, [1.0, 3, 2, 4]) { Label = "TopLine", LabelPlacement = SeriesLabelPlacement.LineEnd });

        Assert.Single(axes.LegendEntries(Theme.Default, directLabelsDrawn: true), e => e.Label == "TopLine");
    }

    [Fact]
    public void EverySlotPastThePrimary_IsOffThePrimaryY_AndThePrimaryAndTheTopXAreOnIt()
    {
        var axes = new Axes();
        var primary = axes.Plot(X, [1.0, 2, 3, 4]);
        var right0 = axes.YAxes.Right.Add().AddSeries(new LineSeries(X, [1.0, 2, 3, 4]));
        var right1 = axes.YAxes.Right.Add().AddSeries(new LineSeries(X, [1.0, 2, 3, 4]));
        var left1 = axes.YAxes.Left.Add().AddSeries(new LineSeries(X, [1.0, 2, 3, 4]));
        var left2 = axes.YAxes.Left.Add().AddSeries(new LineSeries(X, [1.0, 2, 3, 4]));
        var top = axes.AddXSecondarySeries(new LineSeries(X, [1.0, 2, 3, 4]));

        Assert.True(AxisNames.Of(axes, primary).OnPrimaryY);
        Assert.True(AxisNames.Of(axes, top).OnPrimaryY);
        Assert.False(AxisNames.Of(axes, top).OnPrimaryX);
        Assert.All(new ISeries[] { right0, right1, left1, left2 }, s => Assert.False(AxisNames.Of(axes, s).OnPrimaryY));
        Assert.All(new ISeries[] { primary, right0, right1, left1, left2 }, s => Assert.True(AxisNames.Of(axes, s).OnPrimaryX));
    }
}
