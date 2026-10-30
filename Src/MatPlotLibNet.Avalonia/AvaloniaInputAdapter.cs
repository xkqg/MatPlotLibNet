// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
    /// <summary>The button a pointer-pressed event is for.</summary>
    /// <param name="kind">What changed about the pointer.</param>
    internal static PointerButton ButtonPressedBy(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.LeftButtonPressed   => PointerButton.Left,
        PointerUpdateKind.RightButtonPressed  => PointerButton.Right,
        PointerUpdateKind.MiddleButtonPressed => PointerButton.Middle,
        _                                     => PointerButton.None,
    };

    /// <summary>The button a pointer-released event is for.</summary>
    /// <param name="kind">What changed about the pointer.</param>
    internal static PointerButton ButtonReleasedBy(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.LeftButtonReleased   => PointerButton.Left,
        PointerUpdateKind.RightButtonReleased  => PointerButton.Right,
        PointerUpdateKind.MiddleButtonReleased => PointerButton.Middle,
        _                                      => PointerButton.None,
    };

    /// <summary>The button a pointer-moved event is made with: the left if it is held, else the right, else the middle.</summary>
    /// <param name="left">Whether the left button is down.</param>
    /// <param name="right">Whether the right button is down.</param>
    /// <param name="middle">Whether the middle button is down.</param>
    internal static PointerButton ButtonHeld(bool left, bool right, bool middle) =>
        left ? PointerButton.Left : right ? PointerButton.Right : middle ? PointerButton.Middle : PointerButton.None;

    /// <summary>Converts a pointer-pressed event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    public static PointerInputArgs ToPointerArgs(PointerPressedEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        var btn = ButtonPressedBy(e.GetCurrentPoint(source).Properties.PointerUpdateKind);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers), e.ClickCount);
    }

    /// <summary>Converts a pointer-moved event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    public static PointerInputArgs ToPointerArgs(PointerEventArgs e, Control source)
    {
        var pos   = e.GetPosition(source);
        var props = e.GetCurrentPoint(source).Properties;
        var btn   = ButtonHeld(props.IsLeftButtonPressed, props.IsRightButtonPressed, props.IsMiddleButtonPressed);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-released event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    public static PointerInputArgs ToPointerArgs(PointerReleasedEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        var btn = ButtonReleasedBy(e.GetCurrentPoint(source).Properties.PointerUpdateKind);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-wheel event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    public static ScrollInputArgs ToScrollArgs(PointerWheelEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        return new ScrollInputArgs(pos.X, pos.Y, e.Delta.X, e.Delta.Y, Map(e.KeyModifiers));
    }

    /// <summary>Converts a key-down event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
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
