// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using MatPlotLibNet.Interaction;
using ModifierKeys = MatPlotLibNet.Interaction.ModifierKeys;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;

namespace MatPlotLibNet.Wpf;

/// <summary>Converts WPF mouse and key events into the platform-neutral <see cref="PointerInputArgs"/>,
/// <see cref="ScrollInputArgs"/> and <see cref="KeyInputArgs"/> records an <see cref="InteractionHost"/> takes. The
/// button of a move is the button that is held, so a move with none held is a hover, which is what lets the control show
/// a tooltip.</summary>
internal static class WpfInputAdapter
{
    /// <summary>Converts a mouse-down event.</summary>
    public static PointerInputArgs ToPressedArgs(MouseButtonEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return new PointerInputArgs(pos.X, pos.Y, ButtonOf(e.ChangedButton), Modifiers(), e.ClickCount);
    }

    /// <summary>Converts a mouse-move event.</summary>
    public static PointerInputArgs ToMovedArgs(MouseEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        var button = e.LeftButton == MouseButtonState.Pressed ? PointerButton.Left
                   : e.RightButton == MouseButtonState.Pressed ? PointerButton.Right
                   : e.MiddleButton == MouseButtonState.Pressed ? PointerButton.Middle
                   : PointerButton.None;
        return new PointerInputArgs(pos.X, pos.Y, button, Modifiers());
    }

    /// <summary>Converts a mouse-up event.</summary>
    public static PointerInputArgs ToReleasedArgs(MouseButtonEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return new PointerInputArgs(pos.X, pos.Y, ButtonOf(e.ChangedButton), Modifiers());
    }

    /// <summary>Converts a wheel event; one notch of the wheel is a delta of 1.</summary>
    public static ScrollInputArgs ToScrollArgs(MouseWheelEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return new ScrollInputArgs(pos.X, pos.Y, 0, e.Delta / 120.0, Modifiers());
    }

    /// <summary>Converts a key-down event.</summary>
    public static KeyInputArgs ToKeyArgs(KeyEventArgs e) => new(e.Key.ToString(), Modifiers());

    private static PointerButton ButtonOf(MouseButton button) => button switch
    {
        MouseButton.Right => PointerButton.Right,
        MouseButton.Middle => PointerButton.Middle,
        _ => PointerButton.Left,
    };

    private static ModifierKeys Modifiers()
    {
        var held = Keyboard.Modifiers;
        var keys = ModifierKeys.None;
        if ((held & WpfModifierKeys.Shift) != 0) keys |= ModifierKeys.Shift;
        if ((held & WpfModifierKeys.Control) != 0) keys |= ModifierKeys.Ctrl;
        if ((held & WpfModifierKeys.Alt) != 0) keys |= ModifierKeys.Alt;
        return keys;
    }
}
