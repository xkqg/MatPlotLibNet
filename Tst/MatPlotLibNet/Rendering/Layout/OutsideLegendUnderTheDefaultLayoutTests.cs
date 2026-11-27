// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Under the default layout, which keeps no room at the figure's edges, a legend outside the plot stands at its plot's edge
/// beside a colour bar on that edge, the axes' own bar or the figure's, as it stands with no bar there: the layout moves neither the
/// legend past the bar nor the bar. Pushed past the bar it ran off the figure (on a 600 x 400 figure: 0 % of it on the figure for
/// OutsideRight under the library theme and MatplotlibV2, 4 % under MatplotlibClassic; 0, 26 and 16 % below the plot).</summary>
/// <remarks>matplotlib 3.11.2 (<c>python -I</c>), an axes legend anchored past its plot (<c>ax.legend(loc='center left',
/// bbox_to_anchor=(1, 0.5))</c>) beside <c>fig.colorbar</c> without a layout engine: the bar stands where the grid puts it and the legend
/// over it, at its plot's edge (figure bar: legend 453.94..668.94 against the bar 470.25..524.26; 68 % of the legend on the figure,
/// 46 % under classic). The share on the figure is held at least at the one the library drew before the legend was moved past the
/// figure's bar (44, 52, 42 % beside; 100 % below); the axes' bar rows at the share they have standing at the plot's edge.</remarks>
public class OutsideLegendUnderTheDefaultLayoutTests
{
    [Theory]
    [InlineData(false, LegendPosition.OutsideRight, "Default", 0.43)]
    [InlineData(false, LegendPosition.OutsideRight, "MatplotlibV2", 0.52)]
    [InlineData(false, LegendPosition.OutsideRight, "MatplotlibClassic", 0.41)]
    [InlineData(false, LegendPosition.OutsideBottom, "Default", 1.0)]
    [InlineData(false, LegendPosition.OutsideBottom, "MatplotlibV2", 1.0)]
    [InlineData(false, LegendPosition.OutsideBottom, "MatplotlibClassic", 1.0)]
    [InlineData(true, LegendPosition.OutsideRight, "Default", 0)]
    [InlineData(true, LegendPosition.OutsideRight, "MatplotlibV2", 0)]
    [InlineData(true, LegendPosition.OutsideRight, "MatplotlibClassic", 0)]
    [InlineData(true, LegendPosition.OutsideBottom, "Default", 0)]
    [InlineData(true, LegendPosition.OutsideBottom, "MatplotlibV2", 0)]
    [InlineData(true, LegendPosition.OutsideBottom, "MatplotlibClassic", 0)]
    public void AnOutsideLegend_StandsAtItsPlotsEdge_OverTheBar(bool axesBar, LegendPosition position, string themeName, double parentShare)
    {
        var theme = themeName.AsTheme();
        var withBar = Gap(axesBar, position, theme, bar: true, out var share);
        var withoutBar = Gap(axesBar, position, theme, bar: false, out _);

        Assert.True(Math.Abs(withBar - withoutBar) < 1e-6,
            $"the legend stands {withBar:F2} px past its plot's edge with the bar, {withoutBar:F2} px without it ({share:P0} on the figure)");
        Assert.True(share >= parentShare, $"{share:P0} of the legend stands on the figure, {parentShare:P0} before it was moved past the bar");
    }

    /// <summary>How far past the edge of its plot the legend's box starts, drawn on the recording surface, and the share of the box that
    /// stands on the figure.</summary>
    private static double Gap(bool axesBar, LegendPosition position, Theme theme, bool bar, out double share)
    {
        var figure = axesBar
            ? OutsideLegendBesideColorBarTests.Chart(position, theme, "default", bar)
            : OutsideLegendBesideFigureColorBarTests.Chart(position, theme, "default", bar);
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        var plot = new ChartRenderer().FrameOf(figure, new RecordingRenderContext()).PlotAreas[^1];
        var legend = ctx.DrawnBoxOf("legend");
        share = ctx.DrawnShareOnFigure("legend", figure.Width, figure.Height);
        return position == LegendPosition.OutsideRight ? legend.X - (plot.X + plot.Width) : legend.Y - (plot.Y + plot.Height);
    }
}
