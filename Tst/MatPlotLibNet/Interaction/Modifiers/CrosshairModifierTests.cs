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

    /// <summary>The crosshair follows every move it is told of, as the role a controller asks every modifier about: over the plot
    /// it is where the pointer is, and outside the plot it is cleared (the move <c>UpdatePosition</c> takes is the same one).</summary>
    [Fact]
    public void Crosshair_FollowsEveryTrackedMove_AndIsClearedOutsideThePlot()
    {
        var (_, layout) = FigureLayout.Plain();
        InteractionModifier m = new CrosshairModifier(layout);
        m.OnPointerTracked(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Equal(60, m.ActiveCrosshair?.PixelX);
        ((CrosshairModifier)m).UpdatePosition(500, 500);
        Assert.Null(m.ActiveCrosshair);
    }

}
