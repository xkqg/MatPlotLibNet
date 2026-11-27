// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>Verifies <see cref="CrosshairModifier"/>: a passive modifier that claims no press, scroll or key, and whose crosshair
/// follows the pointer over the plot and is cleared outside it.</summary>
public class CrosshairModifierTests
{
    /// <summary>CrosshairModifier no-op IInteractionModifier members.
    /// Crosshair is a passive modifier (HandlesPointerPressed always false), so the
    /// generic <see cref="ModifierTestBase{T}"/> doesn't include it (different ctor).</summary>
    [Fact]
    public void Crosshair_NoOpInteractionMembers_DoNotThrow()
    {
        var (_, layout) = FigureLayout.Plain();
        var m = new CrosshairModifier(layout);

        var press = new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(press));
        m.OnPointerPressed(press);
        m.OnPointerMoved(press);
        m.OnPointerReleased(press);

        var scroll = new ScrollInputArgs(60, 35, 0, -1);
        Assert.False(m.HandlesScroll(scroll));
        m.OnScroll(scroll);

        const string EscapeKey = "Escape";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None));
    }

    /// <summary>CrosshairModifier UpdatePosition outside plot → state cleared
    /// (`axesIndex is null` true arm). Pins the false-arm short-circuit.</summary>
    [Fact]
    public void Crosshair_UpdatePosition_OutsidePlot_ClearsState()
    {
        var (_, layout) = FigureLayout.Plain();
        var m = new CrosshairModifier(layout);
        m.UpdatePosition(60, 35);
        Assert.NotNull(m.ActiveCrosshair);
        m.UpdatePosition(500, 500);
        Assert.Null(m.ActiveCrosshair);
    }

}
