// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>How many steps a colour bar's gradient is drawn in. matplotlib 3.11.2, <c>Colorbar._process_values</c>: with no
/// boundaries of its own, "make the boundaries from the size of the cmap: N = self.cmap.N + 1; b, _ = self._uniform_y(N)", one
/// step for each colour of the map: 256 for viridis, jet and every map built from segments (<c>LinearSegmentedColormap</c>'s
/// <c>N=256</c>), as many as it lists for a <c>ListedColormap</c>. The matplotlib themes draw a bar so; the library's own themes
/// draw 50 steps. The step count decides which of the five near-equal colours of a matrix's picture count as its dominant ones.</summary>
public sealed class ColorBarStepsTests
{
    private static readonly IColorMap FiveListed = new ListedColorMap("five",
        [Colors.Red, Colors.Green, Colors.Blue, Colors.Black, Colors.White]);

    [Fact]
    public void MatplotlibThemes_DrawOneStepForEachColourOfTheMap()
    {
        Assert.Equal(256, ColorBarStrip.StepsFor(ColorMaps.Viridis, Theme.MatplotlibV2));
        Assert.Equal(256, ColorBarStrip.StepsFor(ColorMaps.Jet, Theme.MatplotlibClassic));
        Assert.Equal(5, ColorBarStrip.StepsFor(FiveListed, Theme.MatplotlibV2));
        Assert.Equal(5, ColorBarStrip.StepsFor(new ReversedColorMap(FiveListed), Theme.MatplotlibV2));
    }

    [Fact]
    public void TheLibrarysThemes_DrawFiftySteps()
    {
        Assert.Equal(50, ColorBarStrip.StepsFor(ColorMaps.Viridis, Theme.Default));
        Assert.Equal(50, ColorBarStrip.StepsFor(FiveListed, Theme.Dark));
    }

    private static int Rects(Theme theme, bool bar) => Plt.Create().WithTheme(theme).WithSize(400, 300)
        .AddSubPlot(1, 1, 1, ax => { var heatmap = ax.Heatmap(new double[,] { { 1, 2 } }); if (bar) heatmap.WithColorBar(); })
        .ToSvg().Split("<rect").Length;

    [Fact]
    public void TheAxesBar_IsDrawnInThatManySteps()
    {
        Assert.Equal(256, Rects(Theme.MatplotlibV2, bar: true) - Rects(Theme.MatplotlibV2, bar: false) - 1);
        Assert.Equal(50, Rects(Theme.Default, bar: true) - Rects(Theme.Default, bar: false) - 1);
    }

}
