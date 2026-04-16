// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Uno;

/// <summary>
/// Converts WinUI / Uno Platform input event args into the platform-neutral
/// <see cref="PointerInputArgs"/>, <see cref="ScrollInputArgs"/>, and
/// <see cref="KeyInputArgs"/> records consumed by <see cref="InteractionController"/>.
/// </summary>
internal static class UnoInputAdapter
{
    private const double WheelClickDelta = 120.0; // WHEEL_DELTA per notch on Windows

    /// <summary>Converts a pointer event (pressed, moved, or released).</summary>
    public static PointerInputArgs ToPointerArgs(PointerRoutedEventArgs e, UIElement source, int clickCount = 1)
    {
        var pp   = e.GetCurrentPoint(source);
        var btn  = pp.Properties.IsLeftButtonPressed   ? PointerButton.Left
                 : pp.Properties.IsRightButtonPressed  ? PointerButton.Right
                 : pp.Properties.IsMiddleButtonPressed ? PointerButton.Middle
                 : PointerButton.None;
        return new PointerInputArgs(pp.Position.X, pp.Position.Y, btn, Map(e.KeyModifiers), clickCount);
    }

    /// <summary>Converts a pointer-wheel event.</summary>
    public static ScrollInputArgs ToScrollArgs(PointerRoutedEventArgs e, UIElement source)
    {
        var pp    = e.GetCurrentPoint(source);
        double dy = pp.Properties.MouseWheelDelta / WheelClickDelta;
        return new ScrollInputArgs(pp.Position.X, pp.Position.Y, 0, dy, Map(e.KeyModifiers));
    }

    /// <summary>Converts a key-down event.</summary>
    public static KeyInputArgs ToKeyArgs(KeyRoutedEventArgs e) =>
        new(e.Key.ToString());

    private static ModifierKeys Map(VirtualKeyModifiers k)
    {
        var r = ModifierKeys.None;
        if ((k & VirtualKeyModifiers.Shift)   != 0) r |= ModifierKeys.Shift;
        if ((k & VirtualKeyModifiers.Control) != 0) r |= ModifierKeys.Ctrl;
        if ((k & VirtualKeyModifiers.Menu)    != 0) r |= ModifierKeys.Alt;
        return r;
    }
}
