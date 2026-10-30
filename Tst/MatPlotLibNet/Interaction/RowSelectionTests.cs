// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>A brush is a rectangle of the plot; what a reader means is the rows inside it. When the chart has rows with keys, a
/// brush also tells the application <em>which rows</em>: <see cref="RowSelectionEvent"/>, derived from the brush by the one rule
/// that reads the rectangle against the points, whether the brush was made in a native control or reached a server. A chart with
/// no keyed rows has nothing to say about rows, and the brush stands alone.</summary>
public class RowSelectionTests
{
    private static readonly double[] X = [1, 2, 3, 4];
    private static readonly double[] Y = [10, 20, 30, 40];
    private static readonly string[] Keys = ["a", "b", "c", "d"];

    private static Figure Keyed()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "c" };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y).RowKeys = Keys;
        return figure;
    }

    // ── the rule ────────────────────────────────────────────────────────────────

    [Fact]
    public void TheRowsInsideTheRectangle_AreTheRowsSelected_InTheOrderOfTheData()
    {
        var selection = Keyed().RowSelectionOf(new BrushSelectEvent("c", 0, 1.5, 5, 3.5, 35));

        Assert.NotNull(selection);
        Assert.Equal(["b", "c"], selection!.Rows);
        Assert.Equal("c", selection.ChartId);
        Assert.Equal(0, selection.AxesIndex);
    }

    [Fact]
    public void TheEdgesOfTheRectangle_BelongToIt()
    {
        var selection = Keyed().RowSelectionOf(new BrushSelectEvent("c", 0, 2, 20, 3, 30));

        Assert.Equal(["b", "c"], selection!.Rows);
    }

    [Fact]
    public void ARectangleThatCoversNoPoint_IsAnEmptySelection_NotNothing()
    {
        var selection = Keyed().RowSelectionOf(new BrushSelectEvent("c", 0, 100, 100, 200, 200));

        Assert.NotNull(selection);
        Assert.Empty(selection!.Rows);   // the reader brushed an empty place: the selection is cleared
    }

    [Fact]
    public void APointThatCannotBePlaced_IsNeverSelected()
    {
        var figure = new Figure { Width = 500, Height = 400 };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, double.NaN, 3.0, double.PositiveInfinity], Y).RowKeys = Keys;

        var selection = figure.RowSelectionOf(new BrushSelectEvent("c", 0, -1e9, -1e9, 1e9, 1e9));

        Assert.Equal(["a", "c"], selection!.Rows);
    }

    [Fact]
    public void TwoSeriesOfTheSameRows_GiveEachRowOnce()
    {
        var figure = Keyed();
        figure.SubPlots[0].Plot(X, Y).RowKeys = Keys;   // the same rows drawn as a line too

        var selection = figure.RowSelectionOf(new BrushSelectEvent("c", 0, 0, 0, 5, 50));

        Assert.Equal(["a", "b", "c", "d"], selection!.Rows);
    }

    [Fact]
    public void ASeriesWithNoKeys_HasNothingToSay_SoItsAxesGivesNoSelection()
    {
        var figure = new Figure { Width = 500, Height = 400 };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y);

        Assert.Null(figure.RowSelectionOf(new BrushSelectEvent("c", 0, 0, 0, 5, 50)));
    }

    [Fact]
    public void ABrushOnASubplotTheFigureLacks_GivesNoSelection()
    {
        Assert.Null(Keyed().RowSelectionOf(new BrushSelectEvent("c", 7, 0, 0, 5, 50)));
        Assert.Null(Keyed().RowSelectionOf(new BrushSelectEvent("c", -1, 0, 0, 5, 50)));
    }

    [Fact]
    public void AHiddenSeries_IsNotSelectedFrom()
    {
        var figure = Keyed();
        figure.SubPlots[0].Series[0].Visible = false;

        var selection = figure.RowSelectionOf(new BrushSelectEvent("c", 0, 0, 0, 5, 50));

        Assert.Empty(selection!.Rows);
    }

    [Fact]
    public void TheEvent_IsANotificationThatTouchesNoFigure()
    {
        var figure = Keyed();
        string before = figure.ToJson();

        new RowSelectionEvent("c", 0, ["a"]).ApplyTo(figure);

        Assert.Equal(before, figure.ToJson());
        Assert.IsAssignableFrom<FigureNotificationEvent>(new RowSelectionEvent("c", 0, ["a"]));
    }

    // ── the native control ──────────────────────────────────────────────────────

    private sealed class Collect(List<FigureInteractionEvent> into) : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            into.Add(evt);
            return default;
        }
    }

    private static (InteractionHost Host, List<FigureInteractionEvent> Seen, IChartLayout Layout) Attach(Figure figure)
    {
        var seen = new List<FigureInteractionEvent>();
        var host = new InteractionHost { Handler = new Collect(seen) };
        var layout = ChartLayout.Create(figure, new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()));
        host.Update(figure, layout);
        return (host, seen, layout);
    }

    private static void Brush(InteractionHost host, IChartLayout layout, (double X, double Y) from, (double X, double Y) to)
    {
        var view = layout.ViewOf(0);
        var a = view.DataToPixel(from.X, from.Y);
        var b = view.DataToPixel(to.X, to.Y);
        host.HandlePointerPressed(new PointerInputArgs(a.X, a.Y, PointerButton.Left, ModifierKeys.Shift));
        host.HandlePointerMoved(new PointerInputArgs(b.X, b.Y, PointerButton.Left, ModifierKeys.Shift));
        host.HandlePointerReleased(new PointerInputArgs(b.X, b.Y, PointerButton.Left, ModifierKeys.Shift));
    }

    [Fact]
    public void ABrushInAControl_TellsTheHandlerTheBrush_ThenTheRows()
    {
        var (host, seen, layout) = Attach(Keyed());

        Brush(host, layout, (1.5, 12), (3.5, 35));   // inside the plot: rows b and c

        Assert.IsType<BrushSelectEvent>(seen[0]);
        var rows = Assert.IsType<RowSelectionEvent>(seen[1]);
        Assert.Equal(["b", "c"], rows.Rows);
        Assert.Equal(2, seen.Count);
    }

    [Fact]
    public void ABrushInAControlWithNoKeyedRows_TellsTheHandlerTheBrushAlone()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "c" };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y);
        var (host, seen, layout) = Attach(figure);

        Brush(host, layout, (1.5, 12), (3.5, 35));   // inside the plot: rows b and c

        Assert.IsType<BrushSelectEvent>(Assert.Single(seen));
    }
}
