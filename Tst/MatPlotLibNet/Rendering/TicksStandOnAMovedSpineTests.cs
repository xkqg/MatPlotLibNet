// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Tick marks stand on the line of the spine they belong to: a spine moved to a data value or to a fraction of the
/// axes takes its ticks and their labels with it, as matplotlib's do.</summary>
public class TicksStandOnAMovedSpineTests
{
    private static Figure Moved(SpinePosition position, double value) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10)
            .WithSpines(s => s with { Bottom = s.Bottom with { Position = position, PositionValue = value } }))
        .Build();

    private static List<(Point From, Point To)> VerticalMarks(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var marks = new List<(Point From, Point To)>();
        foreach (var call in recording.Of<DrawLineCall>())
        {
            Point from = call.P1, to = call.P2;
            if (from.X == to.X && Math.Abs(to.Y - from.Y) is > 2 and < 12) marks.Add((from, to));
        }

        return marks;
    }

    [Fact]
    public void WhenTheBottomSpineStandsAtADataValue_TheXTickMarksStandOnIt()
    {
        var figure = Moved(SpinePosition.Data, 5);
        var plot = figure.FirstPlotArea();
        double spineY = plot.Y + plot.Height / 2;

        var marks = VerticalMarks(figure).Where(mark => mark.From.X > plot.X + 1 && mark.From.X < plot.X + plot.Width - 1).ToArray();

        Assert.NotEmpty(marks);
        Assert.Contains(marks, mark => Math.Abs(Math.Min(mark.From.Y, mark.To.Y) - spineY) < 1.5 || Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - spineY) < 1.5);
        Assert.DoesNotContain(marks, mark => Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - (plot.Y + plot.Height + 3.5)) < 0.6);
    }

    [Fact]
    public void WhenTheBottomSpineStandsAtAFractionOfTheAxes_TheXTickMarksStandOnIt()
    {
        var figure = Moved(SpinePosition.Axes, 0.25);
        var plot = figure.FirstPlotArea();
        double spineY = plot.Y + 0.75 * plot.Height;

        var marks = VerticalMarks(figure).Where(mark => mark.From.X > plot.X + 1 && mark.From.X < plot.X + plot.Width - 1).ToArray();

        Assert.Contains(marks, mark => Math.Abs(Math.Min(mark.From.Y, mark.To.Y) - spineY) < 1.5 || Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - spineY) < 1.5);
    }

    [Fact]
    public void WithTheSpineAtTheEdge_TheTicksAreWhereTheyAlwaysWere()
    {
        var edge = Moved(SpinePosition.Edge, 0);
        var plain = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10)).Build();

        Assert.Equal(plain.ToSvg(), edge.ToSvg());
    }
}
