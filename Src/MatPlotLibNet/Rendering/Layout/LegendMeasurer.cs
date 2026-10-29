// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Measures the bounding box of an axes legend without drawing it. Shared between
/// <see cref="ConstrainedLayoutEngine"/> (which needs the size to reserve outside-legend
/// margin before layout is finalised) and <see cref="AxesRenderer"/> (which needs the
/// size to position the box during the actual draw pass).
/// </summary>
/// <remarks>
/// The size is the one <see cref="LegendLayoutCalculator"/> lays the box out at, the calculator the draw pass and the
/// hit-test read as well, so the margin an outside legend reserves and the box that is drawn can not drift apart: if
/// they did, outside legends would either clip (layout under-reserved) or leave a gap (layout over-reserved). The
/// calculator builds the legend font itself from the theme, so a caller cannot pass a size other than the one the
/// legend is drawn at: measuring with a font two points smaller than the drawn one under-reports a 4-entry legend by
/// about 20 px, enough to clip an outside legend.
/// </remarks>
internal static class LegendMeasurer
{
    /// <summary>Returns the font the renderer uses for legend entries. Delegates to
    /// <see cref="ThemedFontProvider.TickFont"/> (the single source of truth shared with
    /// <see cref="AxesRenderer.RenderLegend"/>), then applies any explicit
    /// <see cref="Legend.FontSize"/> override.</summary>
    internal static Font LegendFont(Axes axes, Theme theme)
    {
        var tickFont = ThemedFontProvider.TickFont(theme);
        var legend = axes.Legend;
        return legend.FontSize.HasValue ? tickFont with { Size = legend.FontSize.Value } : tickFont;
    }

    /// <summary>Measures the legend box for an axes, returning zero if the legend is hidden
    /// or has no labelled series. The returned width / height are already in pixels and
    /// include frame padding on all sides.</summary>
    /// <param name="axes">The axes whose legend should be measured.</param>
    /// <param name="ctx">Render context used for <see cref="IRenderContext.MeasureText"/>.</param>
    /// <param name="theme">The active theme — used to derive the same legend font
    /// <see cref="AxesRenderer.RenderLegend"/> will use at draw time.</param>
    internal static Size MeasureBox(Axes axes, IRenderContext ctx, Theme theme)
    {
        if (!axes.Legend.Visible) return Size.Empty;

        // The SAME entry list AxesRenderer.RenderLegend draws and ComputeLegendBounds hit-tests. Whether an end-label
        // series leaves the legend is the renderer's answer, asked of the renderer the factory builds for these axes —
        // the one that will draw them — so the margin is reserved for exactly the box that is drawn.
        bool directLabelsDrawn = AxesRenderer.Create(axes, default, ctx, theme).DrawsDirectLabels;
        var labels = axes.LegendEntries(directLabelsDrawn).Select(e => e.Label).ToList();
        if (labels.Count == 0) return Size.Empty;

        // The box is the one the legend is laid out with, so what is reserved is what is drawn. Where it stands is not
        // asked here, only how big it is.
        var layout = new LegendLayoutCalculator(theme, ctx).Compute(axes.Legend, labels, default);
        return new Size(layout.BoxWidth, layout.BoxHeight);
    }

    /// <summary>Returns <see langword="true"/> if the legend position places the box
    /// outside the plot area — used by the layout engine to decide whether to reserve
    /// extra margin beyond what the tick labels / titles need.</summary>
    internal static bool IsOutsidePosition(LegendPosition position) => position switch
    {
        LegendPosition.OutsideRight  => true,
        LegendPosition.OutsideLeft   => true,
        LegendPosition.OutsideTop    => true,
        LegendPosition.OutsideBottom => true,
        _                            => false,
    };
}
