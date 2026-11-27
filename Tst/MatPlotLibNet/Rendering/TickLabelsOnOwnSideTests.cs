// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Whether an axis' tick labels stand on its own side (the bottom of an X axis, the left of a Y axis), matplotlib's
/// <c>tick_params(labelbottom=...)</c> and <c>labelleft</c>. matplotlib 3.11.2, read with python: after <c>ax.matshow(...)</c> a major X tick has
/// <c>label1</c> (bottom) not visible and <c>label2</c> (top) visible, both marks visible; its Y ticks <c>label1</c> (left) visible. With the
/// labels off its own side an axis keeps its marks there, its mirrored side keeps marks and labels, and the layout keeps no room for labels
/// that are not drawn.</summary>
public class TickLabelsOnOwnSideTests
{
    private static List<(string Text, Point At)> Texts(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Of<DrawTextCall>().Select(call => (call.Text, call.Position))];
    }

    /// <summary>A line whose tick labels are 60 points, large enough that the room they need, not the layout's least margins, sets the plot.</summary>
    private static Figure Line(Theme theme, Action<AxesBuilder> more)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(theme).TightLayout()
            .AddSubPlot(1, 1, 1, ax => { ax.Plot([0.0, 10.0], [0.0, 10.0]); more(ax); })
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelSize = 60 };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { LabelSize = 60 };
        return figure;
    }

    [Fact]
    public void XLabelsOnTop_StandAboveThePlotOnly()
    {
        var figure = Line(Theme.Default, ax => ax.WithXTickLabelsOnTop());
        var plot = figure.FirstPlotArea();

        // The X axis' numbers: those along the plot, not the Y axis' left of it.
        var numbers = Texts(figure).Where(t => t.Text is "0" or "2" or "4" or "6" or "8" or "10" && t.At.X >= plot.X).ToList();

        Assert.Contains(numbers, t => t.At.Y < plot.Y);
        Assert.DoesNotContain(numbers, t => t.At.Y > plot.Y + plot.Height);
        Assert.True(figure.SubPlots[0].XAxis.MajorTicks is { Mirror: true, LabelsOnOwnSide: false });
    }

    [Fact]
    public void XLabelsOffTheBottom_LeaveTheBottomBandToTheTitleAlone()
    {
        var labelled = Line(Theme.Default, ax => ax.WithXTicksMirrored()).FirstPlotArea();
        var onTop = Line(Theme.Default, ax => ax.WithXTickLabelsOnTop()).FirstPlotArea();

        Assert.True(onTop.Y + onTop.Height > labelled.Y + labelled.Height, "the plot reaches lower when no label stands below it");
    }

    [Fact]
    public void YLabelsOnTheRight_StandRightOfThePlotOnly()
    {
        var figure = Line(Theme.Default, ax => ax.WithYTickLabelsOnRight());
        var plot = figure.FirstPlotArea();

        var numbers = Texts(figure).Where(t => t.Text is "0" or "2" or "4" or "6" or "8" or "10" && t.At.Y >= plot.Y - 1 && t.At.Y <= plot.Y + plot.Height + 8).ToList();

        Assert.Contains(numbers, t => t.At.X > plot.X + plot.Width);
        Assert.DoesNotContain(numbers, t => t.At.X < plot.X);
        Assert.True(figure.SubPlots[0].YAxis.MajorTicks is { Mirror: true, LabelsOnOwnSide: false });
    }

    [Fact]
    public void YLabelsOffTheLeft_LeaveTheLeftBandEmpty()
    {
        var labelled = Line(Theme.Default, ax => ax.WithYTicksMirrored()).FirstPlotArea();
        var onRight = Line(Theme.Default, ax => ax.WithYTickLabelsOnRight()).FirstPlotArea();

        Assert.True(onRight.X < labelled.X, "the plot starts further left when no label stands left of it");
    }

    [Fact]
    public void TheOffsetText_StandsWithTheLabels_OnTheMirroredSide()
    {
        // Values near a million carry an offset; with the X labels on top the offset stands above the plot, with the Y labels on the right
        // it ends at the plot's right.
        var figure = Plt.Create().WithSize(600, 400).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1_000_000.0, 1_000_001.0], [1_000_000.0, 1_000_001.0]).WithXTickLabelsOnTop().WithYTickLabelsOnRight())
            .Build();
        var plot = figure.FirstPlotArea();

        var offsets = Texts(figure).Where(t => t.Text.Contains("1e6") || t.Text.StartsWith("+")).ToList();

        Assert.NotEmpty(offsets);
        Assert.All(offsets, t => Assert.True(t.At.Y < plot.Y, $"{t.Text} at {t.At} stands above the plot"));
    }

    [Fact]
    public void AStackedYSlot_WithItsLabelsOff_KeepsNoBandForThem()
    {
        static Figure WithRight(bool labels)
        {
            var figure = Plt.Create().WithSize(600, 400).WithTheme(Theme.Default).TightLayout()
                .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
                    .WithAddRightYAxis(y => y.Plot([0.0, 10.0], [100.0, 200.0])))
                .Build();
            var slot = figure.SubPlots[0].YAxes.Right[0].Axis;
            if (!labels) slot.MajorTicks = slot.MajorTicks with { LabelsOnOwnSide = false };
            return figure;
        }

        var labelled = WithRight(labels: true).FirstPlotArea();
        var bare = WithRight(labels: false).FirstPlotArea();

        Assert.True(bare.X + bare.Width > labelled.X + labelled.Width, "the plot reaches further right when the slot draws no label");
    }

    /// <summary>A line with a top X axis of its own series, its tick labels 30 points, the top axis' labels on its own side or not.</summary>
    private static Figure WithTop(bool labels)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(Theme.Default).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
                .WithSecondaryXAxis(top => top.PlotXSecondary([100.0, 700.0], [0.0, 10.0])))
            .Build();
        var top = figure.SubPlots[0].SecondaryXAxis!;
        top.MajorTicks = top.MajorTicks with { LabelSize = 30, LabelsOnOwnSide = labels };
        return figure;
    }

    /// <summary>matplotlib 3.11.2, read with python: <c>ax.secondary_xaxis("top", ...).tick_params(labeltop=False)</c> leaves a major tick's
    /// <c>tick2line</c> visible and its <c>label2</c> not; the top axis keeps its marks, draws no label and the layout keeps no room for
    /// labels it does not draw, as a stacked Y axis does.</summary>
    [Fact]
    public void TheTopAxis_WithItsLabelsOff_KeepsItsMarks_DrawsNoLabel_AndKeepsNoBandForThem()
    {
        var bare = WithTop(labels: false);
        var plot = bare.FirstPlotArea();

        Assert.DoesNotContain(Texts(bare), t => t.At.Y < plot.Y);
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(bare, drawn);
        Assert.Contains(drawn.Of<DrawLineCall>(), call => call.P1.Y < plot.Y - 0.5);
        Assert.True(plot.Y < WithTop(labels: true).FirstPlotArea().Y, "the plot reaches higher when the top axis draws no label");
    }

    /// <summary>A line with a linked X axis below the plot, its tick labels 30 points, on its own side or not.</summary>
    private static Figure WithBottom(bool labels)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(Theme.Default).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
                .WithAddBottomXAxis(b => b.Link(AxisTransform.Linear(3, 0))))
            .Build();
        var axis = figure.SubPlots[0].YAxes.Bottom[0].Axis;
        axis.MajorTicks = axis.MajorTicks with { LabelSize = 30, LabelsOnOwnSide = labels };
        return figure;
    }

    /// <summary>matplotlib 3.11.2, read with python: <c>ax.secondary_xaxis(-0.2, ...).tick_params(labelbottom=False)</c> leaves a major
    /// tick's <c>tick1line</c> visible and its <c>label1</c> not; a linked axis below the plot draws no label and keeps no band for one.</summary>
    [Fact]
    public void ALinkedAxisBelowThePlot_WithItsLabelsOff_DrawsNoLabel_AndKeepsNoBandForThem()
    {
        var bare = WithBottom(labels: false);
        var plot = bare.FirstPlotArea();

        // The primary's labels stand within a 30-point band below the plot; the linked axis' would stand past it.
        Assert.DoesNotContain(Texts(bare), t => t.At.Y > plot.Y + plot.Height + 40);
        Assert.True(plot.Y + plot.Height > WithBottom(labels: true).FirstPlotArea().Y + WithBottom(labels: true).FirstPlotArea().Height,
            "the plot reaches lower when the linked axis draws no label");
    }

    [Fact]
    public void LabelsOffTheOwnSide_AreCarriedByTheJson_AndLeftOutAtRest()
    {
        var figure = Line(Theme.Default, ax => ax.WithXTickLabelsOnTop());

        var back = figure.RoundTripped();

        Assert.False(back.SubPlots[0].XAxis.MajorTicks.LabelsOnOwnSide);
        Assert.True(back.SubPlots[0].YAxis.MajorTicks.LabelsOnOwnSide);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.DoesNotContain("labelsOnOwnSide", Line(Theme.Default, _ => { }).ToJson());
    }
}
