// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A waterfall chart can mark the level each step was meant to reach: <c>Targets</c>, one absolute level for each step on
/// the scale the bars stand on (not a change), a short mark across the bar at that level and a little past it on both sides, drawn
/// over the bar. A missing level is a step with no mark, the value axis makes room for the levels, the data table gains a column
/// only when there are targets, and the marks have no legend entry.</summary>
public class WaterfallTargetTests
{
    private static readonly string[] Steps = ["Start", "Sales", "Refunds", "Costs", "End"];
    private static readonly double[] Values = [100, 30, -20, 40, -10];

    /// <summary>The running totals of <see cref="Values"/>: 100, 130, 110, 150, 140. Targets are levels on that scale.</summary>
    private static readonly double[] Levels = [100, 125, 110, 150, 145];

    private static Figure Of(Action<WaterfallSeries>? configure = null) => Plt.Create().WithSize(700, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Waterfall(Steps, Values, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The bars as drawn, in order: the rectangles filled with one of the waterfall's own colours.</summary>
    private static List<Rect> Bars(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawRectangle").Select(call => (dynamic)call.Args!)
        .Where(args => (Color?)args.fill == Colors.Green || (Color?)args.fill == Colors.Red)
        .Select(args => (Rect)args.rect)];

    /// <summary>The target marks of a figure as drawn, in order: the horizontal lines 2.5 pixels wide in the colour its own theme gives
    /// text, or in <paramref name="colour"/> when the marks were given one.</summary>
    private static List<(Point From, Point To)> Marks(Figure figure, Color? colour = null) => MarksIn(Drawn(figure), colour ?? figure.Theme.ForegroundText);

    private static List<(Point From, Point To)> MarksIn(RecordingRenderContext drawn, Color colour) => [.. drawn.OfKind("DrawLine")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == colour && (double)args.thickness == 2.5)
        .Select(args => ((Point)args.p1, (Point)args.p2))];

    /// <summary>The pixel row of a level, worked out from the first bar: it stands on zero and reaches 100, a rising bar whose
    /// top is at <c>rect.Y</c> and whose foot is at <c>rect.Y + rect.Height</c>.</summary>
    private static Func<double, double> RowOf(List<Rect> bars)
    {
        double zero = bars[0].Y + bars[0].Height, perUnit = bars[0].Height / 100;
        return level => zero - level * perUnit;
    }

    [Fact]
    public void TheMarkOfAStep_StandsAtItsAbsoluteLevel_NotAtAChangeFromTheRunningTotal()
    {
        var figure = Of(s => s.Targets = Levels);
        var row = RowOf(Bars(Drawn(figure)));

        var marks = Marks(figure);

        Assert.Equal(5, marks.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(row(Levels[i]), marks[i].From.Y, 6);
            Assert.Equal(marks[i].From.Y, marks[i].To.Y, 9);
        }

        // Index 2: the running total is 110 and the target 110 — the mark is on the end of the bar, not 110 above it.
        Assert.Equal(row(110), marks[2].From.Y, 6);
    }

    [Fact]
    public void TheMark_SpansTheBarAndALittlePastItOnBothSides_DrawnOverTheBar()
    {
        var figure = Of(s => s.Targets = Levels);
        var drawn = Drawn(figure);
        var bars = Bars(drawn);
        var marks = Marks(figure);

        for (int i = 0; i < 5; i++)
        {
            double left = Math.Min(marks[i].From.X, marks[i].To.X), right = Math.Max(marks[i].From.X, marks[i].To.X);
            Assert.True(left < bars[i].X && right > bars[i].X + bars[i].Width, $"step {i}: the mark reaches past both sides of its bar");
            Assert.Equal(4.0, bars[i].X - left, 6);
            Assert.Equal(4.0, right - (bars[i].X + bars[i].Width), 6);
        }

        // Over the bar: the mark of a step is drawn after the bar of that step and before the bar of the next.
        var order = drawn.Calls.Select((call, index) => (call, index)).ToList();
        var barAt = order.Where(o => o.call.Kind == "DrawRectangle" && ((Color?)((dynamic)o.call.Args!).fill == Colors.Green || (Color?)((dynamic)o.call.Args!).fill == Colors.Red)).Select(o => o.index).ToList();
        var markAt = order.Where(o => o.call.Kind == "DrawLine" && (Color)((dynamic)o.call.Args!).color == figure.Theme.ForegroundText && (double)((dynamic)o.call.Args!).thickness == 2.5).Select(o => o.index).ToList();
        for (int i = 0; i < 5; i++)
        {
            Assert.True(markAt[i] > barAt[i], $"step {i}: the mark is drawn over its bar");
            if (i < 4) Assert.True(markAt[i] < barAt[i + 1], $"step {i}: the mark is drawn before the next bar");
        }
    }

    [Fact]
    public void AMissingLevel_IsAStepWithNoMark()
    {
        double[] levels = [100, double.NaN, 110, double.NaN, 145];

        var marks = Marks(Of(s => s.Targets = levels));

        Assert.Equal(3, marks.Count);
    }

    [Fact]
    public void TheValueAxis_MakesRoomForTheTargets()
    {
        var figure = Of(s => s.Targets = [100, 125, 110, 150, 400]);
        var plot = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];

        var marks = Marks(figure);

        Assert.All(marks, mark => Assert.InRange(mark.From.Y, plot.Y - 1e-6, plot.Y + plot.Height + 1e-6));
        // The target of 400 stands above every bar and every other mark.
        Assert.Equal(marks.Min(mark => mark.From.Y), marks[4].From.Y, 9);
    }

    [Fact]
    public void WithoutTargets_NoMarkIsDrawn_AndThePictureIsTheOneItWas()
    {
        Assert.Empty(Marks(Of()));
        Assert.DoesNotContain("targets", Of().ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheMarkColour_IsTheThemesTextColourUnlessSaidOtherwise()
    {
        var figure = Of(s => { s.Targets = Levels; s.TargetColor = Colors.Blue; });

        Assert.Equal(5, Marks(figure, Colors.Blue).Count);
        Assert.Empty(Marks(figure));
    }

    [Fact]
    public void ATargetPerStepIsRequired_AndRefusedByNameOtherwise()
    {
        var series = new WaterfallSeries(Steps, Values);

        var thrown = Assert.Throws<ArgumentException>(() => series.Targets = [1, 2, 3]);

        Assert.Equal("Targets", thrown.ParamName);
        Assert.Null(series.Targets);
        series.Targets = Levels;
        series.Targets = null;
        Assert.Null(series.Targets);
    }

    [Fact]
    public void TheTable_GainsATargetColumn_OnlyWhenThereAreTargets()
    {
        var without = new WaterfallSeries(Steps, Values).ToDataTable()!;
        var with = new WaterfallSeries(Steps, Values) { Targets = [100, 125, double.NaN, 150, 145] }.ToDataTable()!;

        Assert.Equal(["category", "value"], without.Columns.Select(c => c.Header));
        Assert.Equal(["category", "value", "target"], with.Columns.Select(c => c.Header));
        Assert.Equal(100.0, with.Rows[0][2].Number);
        Assert.True(double.IsNaN(with.Rows[2][2].Number));   // a missing level is a number cell the table spells as missing
    }

    [Fact]
    public void TheMarksHaveNoLegendEntry()
    {
        var figure = Plt.Create().WithSize(700, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Waterfall(Steps, Values, s => { s.Label = "Bridge"; s.Targets = Levels; }).WithLegend()).Build();

        var entries = figure.SubPlots[0].LegendEntries(directLabelsDrawn: false);

        Assert.Equal(["Bridge"], entries.Select(entry => entry.Label));
    }

    [Fact]
    public void TheTargets_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(s => { s.Targets = [100, 125, double.NaN, 150, 145]; s.TargetColor = Colors.Blue; });

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var waterfall = Assert.IsType<WaterfallSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal(Colors.Blue, waterfall.TargetColor);
        Assert.Equal(5, waterfall.Targets!.Length);
        Assert.True(double.IsNaN(waterfall.Targets[2]));
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }
}
