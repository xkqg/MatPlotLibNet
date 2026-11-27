// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Uno;

/// <summary>
/// Converts WinUI / Uno Platform input event args into the platform-neutral
/// <see cref="PointerInputArgs"/>, <see cref="ScrollInputArgs"/>, and
/// <see cref="KeyInputArgs"/> records consumed by <see cref="InteractionController"/>. The rules are the library's own
/// (which keys are held, which button a move is made with, how far a notch of the wheel turns); this class only reads
/// WinUI's state for them.
/// </summary>
internal static class UnoInputAdapter
{

    /// <summary>Converts a pointer event (pressed, moved, or released).</summary>
    public static PointerInputArgs ToPointerArgs(PointerRoutedEventArgs e, UIElement source, int clickCount = 1)
    {
        var pp   = e.GetCurrentPoint(source);
        var btn  = PointerButton.MovedWith(
            pp.Properties.IsLeftButtonPressed, pp.Properties.IsRightButtonPressed, pp.Properties.IsMiddleButtonPressed);
        return new PointerInputArgs(pp.Position.X, pp.Position.Y, btn, Map(e.KeyModifiers), clickCount);
    }

    /// <summary>Converts a pointer-wheel event.</summary>
    public static ScrollInputArgs ToScrollArgs(PointerRoutedEventArgs e, UIElement source)
    {
        var pp = e.GetCurrentPoint(source);
        return ScrollInputArgs.OfWheel(pp.Position.X, pp.Position.Y, pp.Properties.MouseWheelDelta, Map(e.KeyModifiers));
    }

    /// <summary>Converts a key-down event, with the modifiers held as it happened. A key event carries no modifiers in
    /// WinUI, so they are read from the keyboard, as the WPF control reads them.</summary>
    public static KeyInputArgs ToKeyArgs(KeyRoutedEventArgs e) =>
        ToKeyArgs(e.Key, HeldModifiers());

    /// <summary>The key <paramref name="key"/> pressed with <paramref name="modifiers"/> held.</summary>
    public static KeyInputArgs ToKeyArgs(VirtualKey key, VirtualKeyModifiers modifiers) =>
        new(key.ToString(), Map(modifiers));

    private static VirtualKeyModifiers HeldModifiers()
    {
        var held = VirtualKeyModifiers.None;
        if (IsDown(VirtualKey.Shift))   held |= VirtualKeyModifiers.Shift;
        if (IsDown(VirtualKey.Control)) held |= VirtualKeyModifiers.Control;
        if (IsDown(VirtualKey.Menu))    held |= VirtualKeyModifiers.Menu;
        return held;
    }

    private static bool IsDown(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

    private static ModifierKeys Map(VirtualKeyModifiers k) =>
        ModifierKeys.Held(
            (k & VirtualKeyModifiers.Shift) != 0,
            (k & VirtualKeyModifiers.Control) != 0,
            (k & VirtualKeyModifiers.Menu) != 0);
}
