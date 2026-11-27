// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>The rules a native control follows to turn what its platform reports into the neutral input records: which keys
/// are held, which button a move is made with, and how far one notch of the wheel turns. The WPF, Avalonia and Uno controls
/// each read their platform's flags and hand them here, so the three cannot drift apart.</summary>
internal static class NativeInputExtensions
{
    /// <summary>How far one notch of a mouse wheel reports on the desktop (Windows' <c>WHEEL_DELTA</c>).</summary>
    internal const double WheelDeltaPerNotch = 120.0;

    extension(ModifierKeys)
    {
        /// <summary>The modifier keys held, from the three a desktop platform says are down.</summary>
        /// <param name="shift">Whether Shift is down.</param>
        /// <param name="ctrl">Whether Control is down.</param>
        /// <param name="alt">Whether Alt (Menu) is down.</param>
        internal static ModifierKeys Held(bool shift, bool ctrl, bool alt) =>
            (shift ? ModifierKeys.Shift : ModifierKeys.None)
            | (ctrl ? ModifierKeys.Ctrl : ModifierKeys.None)
            | (alt ? ModifierKeys.Alt : ModifierKeys.None);
    }

    extension(PointerButton)
    {
        /// <summary>The button a pointer move is made with: the left if it is held, else the right, else the middle, else
        /// none, which makes the move a hover.</summary>
        /// <param name="left">Whether the left button is down.</param>
        /// <param name="right">Whether the right button is down.</param>
        /// <param name="middle">Whether the middle button is down.</param>
        internal static PointerButton MovedWith(bool left, bool right, bool middle) =>
            left ? PointerButton.Left : right ? PointerButton.Right : middle ? PointerButton.Middle : PointerButton.None;
    }

    extension(ScrollInputArgs)
    {
        /// <summary>A turn of a mouse wheel that reports in desktop wheel units, where one notch is a delta of 1.</summary>
        /// <param name="x">Where the pointer is, across the control.</param>
        /// <param name="y">Where the pointer is, down the control.</param>
        /// <param name="wheelDelta">What the platform reported (120 a notch).</param>
        /// <param name="modifiers">The modifier keys held.</param>
        internal static ScrollInputArgs OfWheel(double x, double y, double wheelDelta, ModifierKeys modifiers) =>
            new(x, y, 0, wheelDelta / WheelDeltaPerNotch, modifiers);
    }
}
