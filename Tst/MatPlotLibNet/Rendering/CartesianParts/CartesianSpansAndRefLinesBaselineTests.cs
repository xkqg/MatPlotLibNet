// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Phase B.5 + B.6 — baseline snapshots for Spans + ReferenceLines rendering.
/// Captures representative SVG output before the inline blocks are extracted
/// into <c>CartesianSpansPart</c> and <c>CartesianReferenceLinesPart</c>.
/// </summary>
public class CartesianSpansAndRefLinesBaselineTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    // ── Spans ─────────────────────────────────────────────────────────────

    [Fact] public void HorizontalSpan_SolidFill_RendersRectangle()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, s => { s.Alpha = 0.3; s.LineStyle = LineStyle.None; }));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void HorizontalSpan_WithBorder_RendersBorderLines()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, s => { s.Alpha = 0.3; s.LineStyle = LineStyle.Dashed; s.LineWidth = 2; }));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void HorizontalSpan_WithLabel_RendersLabelText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, s => { s.Label = "zone"; }));
        Assert.Contains(">zone<", svg);
    }

    [Fact] public void VerticalSpan_SolidFill_RendersRectangle()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, s => { s.Alpha = 0.3; s.LineStyle = LineStyle.None; }));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void VerticalSpan_WithBorder_RendersBorderLines()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, s => { s.LineStyle = LineStyle.Solid; s.LineWidth = 2; }));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void VerticalSpan_WithLabel_RendersLabelText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, s => { s.Label = "band"; }));
        Assert.Contains(">band<", svg);
    }

    [Fact] public void MultipleSpans_AllRender()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(1.5, 2.5, s => s.Label = "h1")
            .AxHSpan(3.5, 4.5, s => s.Label = "h2")
            .AxVSpan(1.5, 2.5, s => s.Label = "v1"));
        Assert.Contains(">h1<", svg);
        Assert.Contains(">h2<", svg);
        Assert.Contains(">v1<", svg);
    }

    // ── Reference Lines ────────────────────────────────────────────────────

    [Fact] public void HorizontalReferenceLine_Plain_RendersLine()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(3.0));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void HorizontalReferenceLine_WithLabel_RendersLabel()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(3.0, r => { r.Label = "hthresh"; r.LineStyle = LineStyle.Dashed; }));
        Assert.Contains(">hthresh<", svg);
    }

    [Fact] public void VerticalReferenceLine_Plain_RendersLine()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVLine(2.5));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void VerticalReferenceLine_WithLabel_RendersLabel()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVLine(2.5, r => { r.Label = "vthresh"; r.LineStyle = LineStyle.Dotted; }));
        Assert.Contains(">vthresh<", svg);
    }

    [Fact] public void ReferenceLine_CustomColor_UsesOverride()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(3.0, r => { r.Color = Colors.Red; r.LineWidth = 3; }));
        Assert.Contains("<svg", svg);
    }

    [Fact] public void MultipleReferenceLines_AllRender()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(2, r => r.Label = "low")
            .AxHLine(4, r => r.Label = "high")
            .AxVLine(3, r => r.Label = "mid"));
        Assert.Contains(">low<", svg);
        Assert.Contains(">high<", svg);
        Assert.Contains(">mid<", svg);
    }
}
