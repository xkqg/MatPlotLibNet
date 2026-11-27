// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A legend outside the plot on the edge its colour bar stands at stands past the bar, its tick labels and its label, under the
/// tight and the constrained layout, which keep the room for both: the bar is drawn past that edge as the axis bands are, so what the
/// legend stands past (<c>AxesEdgeNeeds.DrawnPast</c>) holds it. matplotlib 3.11.2 has no outside position for an axes legend (lib/matplotlib/legend.py:716);
/// its constrained layout keeps a figure legend at <c>loc='outside ...'</c> past every axes decoration, a colour bar's axes among
/// them (_constrained_layout.py make_layout_margins:442-458). Read from the drawing itself: the legend's group and the bar's group
/// as the recording surface records them, each text as wide as the surface measures it.</summary>
public class OutsideLegendBesideColorBarTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    private static readonly double[] Y = [1, 3, 2, 5, 4, 6, 3, 7, 5, 8];

    /// <summary>One axes with the legend outside on the edge of its colour bar, the bar drawn when <paramref name="bar"/>.</summary>
    internal static Figure Chart(LegendPosition position, Theme theme, string layout, bool bar = true)
    {
        var builder = Plt.Create().WithSize(600, 400).WithTheme(theme);
        builder = layout switch { "tight" => builder.TightLayout(), "constrained" => builder.ConstrainedLayout(), _ => builder };
        var orientation = position == LegendPosition.OutsideRight ? ColorBarOrientation.Vertical : ColorBarOrientation.Horizontal;
        return builder.AddSubPlot(1, 1, 1, ax => ax
                .Scatter(X, Y, s => { s.Label = "points"; s.C = Y; })
                .Plot(X, X, s => s.Label = "a line with a long name")
                .WithColorBar(c => c with { Orientation = orientation, Label = "value", Visible = bar })
                .WithLegend(position))
            .Build();
    }

    [Theory]
    [InlineData(LegendPosition.OutsideRight, "Default", "tight")]
    [InlineData(LegendPosition.OutsideRight, "Default", "constrained")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibV2", "tight")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibV2", "constrained")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibClassic", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "Default", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "Default", "constrained")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibV2", "tight")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibV2", "constrained")]
    public void AnOutsideLegend_StandsPastTheColourBarOnItsEdge(LegendPosition position, string themeName, string layout)
    {
        var theme = themeName.AsTheme();
        var figure = Chart(position, theme, layout);
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var legend = ctx.DrawnBoxOf("legend");
        var bar = ctx.DrawnBoxOf("colorbar");
        if (position == LegendPosition.OutsideRight)
            Assert.True(legend.X >= bar.X + bar.Width, $"the legend ({legend.X:F2}) stands past the colour bar ({bar.X + bar.Width:F2})");
        else
            Assert.True(legend.Y >= bar.Y + bar.Height, $"the legend ({legend.Y:F2}) stands below the colour bar ({bar.Y + bar.Height:F2})");

        // The layout kept the room: the legend stands inside the figure. Under the default layout, which keeps none, the legend stands at
        // its plot's edge over the bar (OutsideLegendUnderTheDefaultLayoutTests).
        Assert.True(legend.X + legend.Width <= figure.Width + 1e-9, $"the legend's right ({legend.X + legend.Width:F2}) stands inside the figure");
        Assert.True(legend.Y + legend.Height <= figure.Height + 1e-9, $"the legend's bottom ({legend.Y + legend.Height:F2}) stands inside the figure");
    }
}
