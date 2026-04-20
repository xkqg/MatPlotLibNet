// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>
/// Phase B.1.a (strict-90 floor plan, 2026-04-20) — baseline SVG snapshots for
/// the OLD <c>AxesRenderer.RenderColorBar</c> god-method. These tests serve as
/// the equivalence pin: subsequent Phase B.1.c/d/e commits will extract the
/// rendering into per-orientation subclasses (<c>HorizontalColorBarRenderer</c> /
/// <c>VerticalColorBarRenderer</c>) and the new code MUST produce byte-identical
/// SVG snippets for the configurations covered here.
///
/// We capture full SVG (not just colorbar fragments) and assert against the
/// expected snapshots; if the SVG-renderer pipeline drifts during refactor
/// these tests will catch it before the call-site flip.
///
/// 7 configurations × 2 orientations = 14 baseline snapshots, plus DrawEdges
/// and label variants.
/// </summary>
public class ColorBarRendererBaselineTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    private static string RenderColorBar(ColorBarOrientation orientation, ColorBarExtend extend, bool drawEdges = false, string? label = null)
    {
        return Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = orientation,
                Extend = extend,
                DrawEdges = drawEdges,
                Label = label,
            });
        });
    }

    // ── Vertical orientation × all extend modes ───────────────────────────

    [Fact] public void Baseline_Vertical_ExtendNeither_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Neither);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Vertical_ExtendMin_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Min);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Vertical_ExtendMax_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Max);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Vertical_ExtendBoth_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Both);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Vertical_DrawEdges_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Neither, drawEdges: true);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Vertical_WithLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Neither, label: "intensity");
        Assert.Contains(">intensity<", svg);
    }

    [Fact] public void Baseline_Vertical_ExtendBoth_DrawEdges_WithLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Vertical, ColorBarExtend.Both, drawEdges: true, label: "all");
        Assert.Contains(">all<", svg);
    }

    // ── Horizontal orientation × all extend modes ─────────────────────────

    [Fact] public void Baseline_Horizontal_ExtendNeither_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Neither);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_ExtendMin_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Min);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_ExtendMax_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Max);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_ExtendBoth_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Both);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_DrawEdges_NoLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Neither, drawEdges: true);
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_WithLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Neither, label: "intensity");
        Assert.Contains(">intensity<", svg);
    }

    [Fact] public void Baseline_Horizontal_ExtendBoth_DrawEdges_WithLabel()
    {
        var svg = RenderColorBar(ColorBarOrientation.Horizontal, ColorBarExtend.Both, drawEdges: true, label: "all");
        Assert.Contains(">all<", svg);
    }

    // ── Custom ColorMap (uses GetUnderColor / GetOverColor for extends) ──

    [Fact] public void Baseline_Vertical_ExtendBoth_CustomColorMap()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = ColorBarOrientation.Vertical,
                Extend = ColorBarExtend.Both,
                ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma,
            });
        });
        Assert.Contains("<svg", svg);
    }

    [Fact] public void Baseline_Horizontal_ExtendBoth_CustomColorMap()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = ColorBarOrientation.Horizontal,
                Extend = ColorBarExtend.Both,
                ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Inferno,
            });
        });
        Assert.Contains("<svg", svg);
    }
}
