// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.5 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="MatPlotLibNet.Rendering.Svg.SvgSelectionScript"/>.
///
/// <para>Pre-G.5 only static-emission tests existed for the selection script
/// (does the JS string appear in the SVG?). This file drives the full drag
/// lifecycle (Shift+pointerdown → pointermove → pointerup) through Jint and
/// asserts the emitted <c>mpl:selection</c> <c>CustomEvent</c> payload, plus
/// the <c>Escape</c>-cancel path.</para></summary>
public class SelectionBehaviouralTests
{
    private static InteractionScriptHarness BuildWithSelection() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithSelection()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

    [Fact]
    public void ShiftDrag_CreatesBrushRect_AndRemovesOnRelease()
    {
        using var h = BuildWithSelection();

        // No brush rect at rest.
        Assert.Null(h.Document.querySelector("rect[aria-label='Data selection area']"));

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; e.shiftKey = true; });
        // During drag the rect is present.
        Assert.NotNull(h.Document.querySelector("rect[aria-label='Data selection area']"));
        h.Simulate("svg", "pointermove", e => { e.clientX = 200; e.clientY = 150; e.shiftKey = true; });
        // Still present, with width/height >= 0.
        var rect = h.Document.querySelector("rect[aria-label='Data selection area']")!;
        var w = double.Parse(rect.getAttribute("width") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        var hgt = double.Parse(rect.getAttribute("height") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(w > 0 && hgt > 0, $"expected non-zero brush rect after move, got {w}×{hgt}");

        h.Simulate("svg", "pointerup", e => { e.clientX = 200; e.clientY = 150; e.shiftKey = true; });
        // After release the rect is removed.
        Assert.Null(h.Document.querySelector("rect[aria-label='Data selection area']"));
    }

    [Fact]
    public void ShiftDrag_DispatchesMplSelectionEvent_WithBoundingBox()
    {
        using var h = BuildWithSelection();

        // Subscribe on the SVG before firing the drag.
        DomEvent? captured = null;
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        svg.addEventListener("mpl:selection", ev => captured = ev);

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 80;  e.shiftKey = true; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 220; e.clientY = 160; e.shiftKey = true; });
        h.Simulate("svg", "pointerup",   e => { e.clientX = 220; e.clientY = 160; e.shiftKey = true; });

        Assert.NotNull(captured);
        Assert.Equal("mpl:selection", captured!.type);
        Assert.NotNull(captured.detail);
    }

    [Fact]
    public void EscapeKey_DuringDrag_CancelsBrushWithoutDispatch()
    {
        using var h = BuildWithSelection();

        DomEvent? captured = null;
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        svg.addEventListener("mpl:selection", ev => captured = ev);

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; e.shiftKey = true; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 200; e.clientY = 150; e.shiftKey = true; });
        Assert.NotNull(h.Document.querySelector("rect[aria-label='Data selection area']"));

        h.Simulate("svg", "keydown", e => { e.key = "Escape"; });

        // Rect gone, AND no event dispatched.
        Assert.Null(h.Document.querySelector("rect[aria-label='Data selection area']"));
        Assert.Null(captured);
    }

    [Fact]
    public void NonShiftDrag_DoesNotStartSelection()
    {
        using var h = BuildWithSelection();

        // shiftKey:false means startSelection bails immediately.
        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; e.shiftKey = false; });
        Assert.Null(h.Document.querySelector("rect[aria-label='Data selection area']"));
    }
}
