// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The frame of a colour bar under the matplotlib themes, as matplotlib's. matplotlib 3.11.2's outline is a spine of the bar's
/// axes (<c>_ColorbarSpine</c>) drawn <c>axes.linewidth</c> wide in <c>axes.edgecolor</c> (printed: <c>cb.outline.get_linewidth()</c>
/// 0.8 under the default style, 1.0 under the classic one, black), and its edges (<c>drawedges=True</c>) are a <c>LineCollection</c> of
/// <c>0.5 * axes.linewidth</c> (<c>Colorbar.__init__</c>). The library drew a 0.5 px frame and 0.3 px edges under every theme; its own
/// themes keep them.</summary>
public sealed class ColorBarFrameTests
{
    private static readonly double Pt = 100.0 / 72;

    private static List<CallRecord> BarCalls(Theme theme, bool drawEdges = false)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 1 }, { 2, 3 } }).WithColorBar(cb => cb with { DrawEdges = drawEdges })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return ctx.Calls.GetRange(start + 1, end - start - 1);
    }

    [Theory]
    [InlineData("v2", 0.8)]
    [InlineData("classic", 1.0)]
    public void UnderTheMatplotlibThemes_TheFrameIsAsWideAsTheAxesLine(string style, double points)
    {
        var bar = BarCalls(style.AsTheme());

        Assert.Equal(points * Pt, bar.Frame().StrokeThickness, 9);
    }

    [Fact]
    public void UnderV2_TheEdgesAreHalfTheAxesLine()
    {
        var bar = BarCalls(Theme.MatplotlibV2, drawEdges: true);

        var edges = bar.OfType<DrawLineCall>().Select(c => c.Thickness)
            .Where(width => Math.Abs(width - 0.8 * Pt) > 1e-9).Distinct().ToList();   // the tick marks are 0.8 pt wide
        Assert.Equal([0.4 * Pt], edges, new Tolerance(1e-9));
    }

    [Fact]
    public void UnderTheLibrarysOwnTheme_TheFrameAndTheEdgesKeepTheirWidths()
    {
        var bar = BarCalls(Theme.Default, drawEdges: true);

        Assert.Equal(0.5, bar.Frame().StrokeThickness, 9);
        Assert.All(bar.OfType<DrawLineCall>(), c => Assert.Equal(0.3, c.Thickness, 9));
    }

    private sealed class Tolerance(double eps) : IEqualityComparer<double>
    {
        public bool Equals(double a, double b) => Math.Abs(a - b) <= eps;
        public int GetHashCode(double value) => 0;
    }
}
