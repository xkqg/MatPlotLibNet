// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A legend outside the plot of an axes on the edge the figure's colour bar stands at (<c>FigureBuilder.WithColorBar</c>,
/// matplotlib's <c>fig.colorbar(im, ax=axs)</c>) stands past that bar, its tick labels and its label, as it stands past the axes' own
/// bar, under the tight and the constrained layout, which keep the room for both: the figure's bar is drawn past that edge too, so what
/// the legend stands past (<c>AxesEdgeNeeds.DrawnPast</c>) holds it.</summary>
/// <remarks>matplotlib 3.11.2 on a 600 x 400 figure, two subplots side by side, <c>fig.colorbar(sc, ax=list(axs), label='value')</c>,
/// boxes in pixels from the left. Its outside legend, the position the library's outside positions follow (<c>fig.legend(...,
/// loc='outside right center')</c>, constrained layout), stands past the bar: the legend from 378.06, the bar with its text to 371.62
/// (classic: 282.00 against 278.95); below, <c>'outside lower center'</c> with a horizontal bar, the legend from 346.33, the bar to
/// 343.10 (classic: 324.00 against 323.01), the figure legend's margin outside the grid that holds the bar
/// (lib/matplotlib/_constrained_layout.py make_layout_margins:441-458). An axes legend anchored past the plot
/// (<c>axs[-1].legend(loc='center left', bbox_to_anchor=(1, 0.5))</c>) is drawn over the bar under the default layout (legend
/// 453.94..668.94, bar 470.25..524.26) and <c>tight_layout</c> (463.00..678.00 against 470.25..524.26, "not compatible with
/// tight_layout"), the overlap this library does not copy. Read from the drawing itself: the legend's group and the figure bar's group
/// as the recording surface records them.</remarks>
public class OutsideLegendBesideFigureColorBarTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    private static readonly double[] Y = [1, 3, 2, 5, 4, 6, 3, 7, 5, 8];

    /// <summary>Two subplots on the bar's edge's other axis (side by side for a bar beside, stacked for a bar below), the legend outside
    /// the last one, on the bar's edge, and the figure's bar over both when <paramref name="bar"/>.</summary>
    internal static Figure Chart(LegendPosition position, Theme theme, string layout, bool bar = true)
    {
        bool beside = position == LegendPosition.OutsideRight;
        var builder = Plt.Create().WithSize(600, 400).WithTheme(theme);
        builder = layout switch { "tight" => builder.TightLayout(), "constrained" => builder.ConstrainedLayout(), _ => builder };
        int rows = beside ? 1 : 2, cols = beside ? 2 : 1;
        builder = builder.AddSubPlot(rows, cols, 1, ax => ax.Scatter(X, Y, s => s.C = Y));
        builder = builder.AddSubPlot(rows, cols, 2, ax => ax
            .Scatter(X, Y, s => { s.Label = "points"; s.C = Y; })
            .Plot(X, X, s => s.Label = "a line with a long name")
            .WithLegend(position));
        var orientation = beside ? ColorBarOrientation.Vertical : ColorBarOrientation.Horizontal;
        return bar ? builder.WithColorBar(c => c with { Orientation = orientation, Label = "value" }).Build() : builder.Build();
    }

    [Theory]
    [InlineData(LegendPosition.OutsideRight, "Default", "tight")]
    [InlineData(LegendPosition.OutsideRight, "Default", "constrained")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibV2", "tight")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibV2", "constrained")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibClassic", "tight")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibClassic", "constrained")]
    [InlineData(LegendPosition.OutsideBottom, "Default", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "Default", "constrained")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibV2", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibV2", "constrained")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibClassic", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibClassic", "constrained")]
    public void AnOutsideLegend_StandsPastTheFigureColourBarOnItsEdge(LegendPosition position, string themeName, string layout)
    {
        var theme = themeName.AsTheme();
        var figure = Chart(position, theme, layout);
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var legend = ctx.DrawnBoxOf("legend");
        var bar = ctx.DrawnBoxOf("figure-colorbar");
        string boxes = $"legend {legend.X:F2}..{legend.X + legend.Width:F2} x {legend.Y:F2}..{legend.Y + legend.Height:F2}, "
            + $"bar {bar.X:F2}..{bar.X + bar.Width:F2} x {bar.Y:F2}..{bar.Y + bar.Height:F2}";
        if (position == LegendPosition.OutsideRight)
            Assert.True(legend.X >= bar.X + bar.Width, $"the legend stands past the figure's colour bar: {boxes}");
        else
            Assert.True(legend.Y >= bar.Y + bar.Height, $"the legend stands below the figure's colour bar: {boxes}");

        // The layout kept the room: the legend stands inside the figure. Under the default layout, which keeps none, the legend stands at
        // its plot's edge over the bar (OutsideLegendUnderTheDefaultLayoutTests).
        Assert.True(legend.X + legend.Width <= figure.Width + 1e-9, $"the legend stands inside the figure: {boxes}");
        Assert.True(legend.Y + legend.Height <= figure.Height + 1e-9, $"the legend stands inside the figure: {boxes}");
    }

    /// <summary>A legend of the figure outside on the bar's edge stands past the bar under the constrained layout, as matplotlib's
    /// <c>fig.legend(loc='outside right center')</c> does (the legend from 378.06, the bar to 371.62): the layout keeps the legend's
    /// room past everything the subplots and the figure's bar need there.</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    public void ALegendOfTheFigure_StandsPastTheFiguresBarUnderTheConstrainedLayout(string themeName)
    {
        var theme = themeName.AsTheme();
        var figure = Plt.Create().WithSize(600, 400).WithTheme(theme).ConstrainedLayout()
            .AddSubPlot(1, 2, 1, ax => ax.Scatter(X, Y, s => { s.Label = "points"; s.C = Y; }).WithLegend(visible: false))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, X, s => s.Label = "a line with a long name").WithLegend(visible: false))
            .WithColorBar(c => c with { Label = "value" })
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight })
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var legend = ctx.DrawnBoxOf("legend");
        var bar = ctx.DrawnBoxOf("figure-colorbar");
        Assert.True(legend.X >= bar.X + bar.Width, $"the figure's legend ({legend.X:F2}) stands past the bar ({bar.X + bar.Width:F2})");
    }

    /// <summary>A legend outside an axes that does not stand on the bar's edge (the first of two side by side) is not moved past the
    /// figure's bar: it stands in the gap beside its own plot, the bar past the last column.</summary>
    [Fact]
    public void ALegendOffTheBarsEdge_IsNotMovedPastTheBar()
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "points").WithLegend(LegendPosition.OutsideRight))
            .AddSubPlot(1, 2, 2, ax => ax.Scatter(X, Y, s => s.C = Y))
            .WithColorBar(c => c with { Label = "value" })
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var legend = ctx.DrawnBoxOf("legend");
        var bar = ctx.DrawnBoxOf("figure-colorbar");
        Assert.True(legend.X + legend.Width < bar.X, $"the legend ({legend.X:F2}..{legend.X + legend.Width:F2}) stands before the bar ({bar.X:F2})");
    }
}
