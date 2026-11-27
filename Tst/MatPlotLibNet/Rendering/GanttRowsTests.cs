// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A Gantt chart names its rows by its tasks, the first task on top, and draws each task in a colour of its own, as the chart
/// matplotlib users draw: one <c>ax.barh(i, duration, left=start)</c> per task, <c>set_yticks</c> with the task names and
/// <c>invert_yaxis()</c>. matplotlib 3.11.2, read with python: four <c>barh</c> calls draw '#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', the
/// patch cycle moving once a call. Under every theme: the rows stand in the category slots every chart of rows stands in
/// (<c>CategorySlot.FromTop</c>).</summary>
public class GanttRowsTests
{
    private static readonly string[] Tasks = ["Design", "Build", "Test"];

    private static Figure Gantt(Theme theme, Action<GanttSeries>? configure = null, Action<AxesBuilder>? more = null) =>
        Plt.Create().WithSize(600, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Gantt(Tasks, [0.0, 3.0, 8.0], [3.0, 8.0, 11.0], configure);
                more?.Invoke(ax);
            })
            .Build();

    private static List<(string Text, Point At)> Texts(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Of<DrawTextCall>().Select(call => (call.Text, call.Position))];
    }

    /// <summary>The fills of the bars, top to bottom: every filled rectangle inside the plot that is not the plot's background.</summary>
    private static List<Color> BarFills(Figure figure)
    {
        var plot = figure.FirstPlotArea();
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Of<DrawRectangleCall>()
            .Select(call => (Rect: call.Rect, Fill: call.Fill))
            .Where(r => r.Fill is { } f && f != figure.Theme.AxesBackground && f != figure.Theme.Background
                        && r.Rect.Width < plot.Width - 1 && r.Rect.X >= plot.X - 1)
            .OrderBy(r => r.Rect.Y)
            .Select(r => r.Fill!.Value)];
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("default")]
    public void TheRows_AreNamedByTheTasks_TheFirstOnTop(string themeId)
    {
        var figure = Gantt(themeId.AsTheme());
        var plot = figure.FirstPlotArea();

        var names = Texts(figure).Where(t => Tasks.Contains(t.Text) && t.At.X < plot.X).ToList();

        Assert.Equal(Tasks, names.OrderBy(t => t.At.Y).Select(t => t.Text));
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("default")]
    public void EachTask_WearsTheNextColourOfTheCycle_TheFirstOnTop(string themeId)
    {
        var theme = themeId.AsTheme();

        Assert.Equal([theme.CycleColors[0], theme.CycleColors[1], theme.CycleColors[2]], BarFills(Gantt(theme)));
    }

    [Fact]
    public void AColourTheCallerGives_IsEveryTasksColour()
    {
        Assert.Equal([Colors.Red, Colors.Red, Colors.Red], BarFills(Gantt(Theme.MatplotlibV2, s => s.Color = Colors.Red)));
    }

    [Fact]
    public void ASeriesAfterTheGantt_TakesTheColourAfterItsTasks()
    {
        var axes = Gantt(Theme.MatplotlibV2, more: ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).SubPlots[0];

        // The Gantt holds three slots of the patch cycle; the line asks the lines cycle (matplotlib keeps the two apart), so it takes C0,
        // and a bar after the Gantt takes C3.
        var bars = Gantt(Theme.MatplotlibV2, more: ax => ax.Bar(["x"], [1.0])).SubPlots[0];
        Assert.Equal(3, bars.ColorSlots(Theme.MatplotlibV2)[1]);
        Assert.Equal(0, axes.ColorSlots(Theme.MatplotlibV2)[1]);
    }

    [Fact]
    public void AnInvertedYAxis_PutsTheFirstTaskAtTheBottom()
    {
        var figure = Gantt(Theme.MatplotlibV2);
        figure.SubPlots[0].YAxis.Inverted = true;
        var plot = figure.FirstPlotArea();

        var names = Texts(figure).Where(t => Tasks.Contains(t.Text) && t.At.X < plot.X).OrderBy(t => t.At.Y).Select(t => t.Text);

        Assert.Equal(Tasks.Reverse(), names);
    }

    [Fact]
    public void TheGantt_NamesItsCategoriesAlongY_TopDown()
    {
        var series = new GanttSeries(Tasks, [0.0, 1.0, 2.0], [1.0, 2.0, 3.0]);

        Assert.Equal(Tasks, ((ICategoryLabeled)series).CategoryLabels);
        Assert.True(((ICategoryPlacement)series) is { CategoriesAlongY: true, CategoriesTopDown: true });
        Assert.Equal(3, series.ColorSlotSpan);
        Assert.Equal(1, new GanttSeries([], [], []).ColorSlotSpan);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Gantt(Theme.MatplotlibV2);

        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }
}
