// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A chart whose Y axis reads nothing (every column or layer is on a scale of its own) keeps its grid off that scale too.</summary>
public class UnreadScaleGridTests
{
    public static TheoryData<Action<AxesBuilder>> VerbsWhoseYScaleReadsNothing => new()
    {
        ax => ax.ParallelCoordinates(["length", "width", "height"], [new("dog", [2, 30, 0]), new("cat", [1, 10, 100]), new("dog", [3, 20, 50])]),
        ax => ax.StreamGraph([0, 1, 2, 3], [new("a", [1, 2, 2, 1]), new("b", [1, 3, 2, 2]), new("c", [4, 5, 8, 7])]),
    };

    /// <summary>Under a theme that draws a grid (ggplot), no horizontal line stands on a value that reads nothing.</summary>
    [Theory]
    [MemberData(nameof(VerbsWhoseYScaleReadsNothing))]
    public void UnderAGridTheme_NoHorizontalGridLine_StandsOnAScaleThatReadsNothing(Action<AxesBuilder> verb)
    {
        string svg = Plt.Create().WithSize(700, 500).WithTheme(Theme.Ggplot).AddSubPlot(1, 1, 1, verb).Build().ToSvg();

        var plot = svg.PlotArea();
        Assert.DoesNotContain(svg.Direct("line"), l => l.Number("y1") == l.Number("y2") && l.Number("x1") <= plot.X + 1 && l.Number("x2") >= plot.X + plot.Width - 1);
    }
}
