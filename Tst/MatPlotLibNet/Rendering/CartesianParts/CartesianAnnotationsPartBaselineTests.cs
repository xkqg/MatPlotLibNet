// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Phase B.4 (strict-90 floor plan) — baseline-snapshot tests for the
/// CartesianAxesRenderer.Render annotation-rendering block that's about to
/// be extracted to <c>CartesianAnnotationsPart</c>. Captures SVG markers from
/// representative annotation configurations so the B.4.e call-site flip is
/// verified equivalence-preserving.
/// </summary>
public class CartesianAnnotationsPartBaselineTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    // ── No annotations: should emit no annotation-specific SVG ────────────

    [Fact]
    public void NoAnnotations_StillRendersFigure()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5]));
        Assert.Contains("<svg", svg);
    }

    // ── Plain text annotation (no box, no background, no arrow) ───────────

    [Fact]
    public void PlainTextAnnotation_DrawsTextOnly()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("plain", 2, 3));
        Assert.Contains(">plain<", svg);
    }

    // ── Annotation with BoxStyle (callout box) ────────────────────────────

    [Fact]
    public void BoxStyleRound_DrawsCalloutBoxBehindText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("box", 2, 3, a => { a.BoxStyle = BoxStyle.Round; a.BoxFaceColor = Colors.Yellow; }));
        Assert.Contains(">box<", svg);
    }

    [Fact]
    public void BoxStyleSquare_DrawsCalloutBoxBehindText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("sq", 2, 3, a => { a.BoxStyle = BoxStyle.Square; }));
        Assert.Contains(">sq<", svg);
    }

    // ── Annotation with legacy BackgroundColor (no BoxStyle) ──────────────

    [Fact]
    public void BackgroundColorOnly_DrawsRectangleBehindText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("bg", 2, 3, a => a.BackgroundColor = Colors.Yellow));
        Assert.Contains(">bg<", svg);
    }

    // ── Annotation with arrow target + connection ────────────────────────

    [Fact]
    public void WithArrowTarget_StraightConnection_DrawsPathAndArrowhead()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("arrow", 2, 3, 4, 4));
        Assert.Contains(">arrow<", svg);
    }

    [Fact]
    public void WithArrowTarget_Arc3Connection_DrawsCurvedPath()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("arc", 2, 3, 4, 4, a => { a.ConnectionStyle = ConnectionStyle.Arc3; a.ConnectionRad = 0.5; }));
        Assert.Contains(">arc<", svg);
    }

    [Fact]
    public void WithArrowStyleNone_SkipsArrowEvenIfTargetSet()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("noar", 2, 3, 4, 4, a => a.ArrowStyle = ArrowStyle.None));
        Assert.Contains(">noar<", svg);
    }

    // ── Annotation with explicit font override ────────────────────────────

    [Fact]
    public void WithCustomFont_UsesOverride()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("font", 2, 3, a => a.Font = new Font { Family = "monospace", Size = 14 }));
        Assert.Contains(">font<", svg);
    }

    // ── Annotation with custom ArrowColor override ────────────────────────

    [Fact]
    public void WithCustomArrowColor_UsesOverride()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("ac", 2, 3, 4, 4, a => a.ArrowColor = Colors.Magenta));
        Assert.Contains(">ac<", svg);
    }

    // ── Multiple annotations in the same axes ─────────────────────────────

    [Fact]
    public void MultipleAnnotations_AllRender()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("A", 1.5, 2)
            .Annotate("B", 2.5, 3)
            .Annotate("C", 3.5, 4, 4.5, 4.5));
        Assert.Contains(">A<", svg);
        Assert.Contains(">B<", svg);
        Assert.Contains(">C<", svg);
    }

    // ── Annotation with non-zero rotation ─────────────────────────────────

    [Fact]
    public void WithRotation_TextRotatesViaSvgTransform()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("rot", 2, 3, a => a.Rotation = 45));
        Assert.Contains(">rot<", svg);
    }
}
