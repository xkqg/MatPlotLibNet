// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// Converts Avalonia input event args into the platform-neutral
/// <see cref="PointerInputArgs"/>, <see cref="ScrollInputArgs"/>, and
/// <see cref="KeyInputArgs"/> records consumed by <see cref="InteractionController"/>.
/// </summary>
internal static class AvaloniaInputAdapter
{
    /// <summary>Converts a pointer-pressed event.</summary>
    public static PointerInputArgs ToPointerArgs(PointerPressedEventArgs e, Control source)
    {
        var pos  = e.GetPosition(source);
        var kind = e.GetCurrentPoint(source).Properties.PointerUpdateKind;
        var btn  = kind switch
        {
            PointerUpdateKind.LeftButtonPressed   => PointerButton.Left,
            PointerUpdateKind.RightButtonPressed  => PointerButton.Right,
            PointerUpdateKind.MiddleButtonPressed => PointerButton.Middle,
            _                                     => PointerButton.None,
        };
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers), e.ClickCount);
    }

    /// <summary>Converts a pointer-moved event.</summary>
    public static PointerInputArgs ToPointerArgs(PointerEventArgs e, Control source)
    {
        var pos   = e.GetPosition(source);
        var props = e.GetCurrentPoint(source).Properties;
        var btn   = props.IsLeftButtonPressed   ? PointerButton.Left
                  : props.IsRightButtonPressed  ? PointerButton.Right
                  : props.IsMiddleButtonPressed ? PointerButton.Middle
                  : PointerButton.None;
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-released event.</summary>
    public static PointerInputArgs ToPointerArgs(PointerReleasedEventArgs e, Control source)
    {
        var pos   = e.GetPosition(source);
        var kind  = e.GetCurrentPoint(source).Properties.PointerUpdateKind;
        var btn   = kind switch
        {
            PointerUpdateKind.LeftButtonReleased   => PointerButton.Left,
            PointerUpdateKind.RightButtonReleased  => PointerButton.Right,
            PointerUpdateKind.MiddleButtonReleased => PointerButton.Middle,
            _                                      => PointerButton.None,
        };
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-wheel event.</summary>
    public static ScrollInputArgs ToScrollArgs(PointerWheelEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        return new ScrollInputArgs(pos.X, pos.Y, e.Delta.X, e.Delta.Y, Map(e.KeyModifiers));
    }

    /// <summary>Converts a key-down event.</summary>
    public static KeyInputArgs ToKeyArgs(KeyEventArgs e) =>
        new(e.Key.ToString(), Map(e.KeyModifiers));

    private static ModifierKeys Map(KeyModifiers k)
    {
        var r = ModifierKeys.None;
        if ((k & KeyModifiers.Shift)   != 0) r |= ModifierKeys.Shift;
        if ((k & KeyModifiers.Control) != 0) r |= ModifierKeys.Ctrl;
        if ((k & KeyModifiers.Alt)     != 0) r |= ModifierKeys.Alt;
        return r;
    }
}
