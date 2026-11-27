// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Uno;

/// <summary>Counts the presses of a click: WinUI's pointer events carry no click count, so the element counts them the way
/// the desktop does, and a second press soon after the first and near it is the second click of a double click (the count
/// <see cref="Interaction.PointerInputArgs.ClickCount"/> carries, and Home answers). One counter per element.</summary>
internal sealed class ClickCounter
{
    /// <summary>The longest time between two presses of one double click: the desktop's default, half a second.</summary>
    internal const ulong DoubleClickMicroseconds = 500_000;

    /// <summary>The farthest two presses of one double click lie apart, in each direction: the desktop's default.</summary>
    internal const double DoubleClickDistance = 4;

    private ulong _lastAt;
    private double _lastX, _lastY;
    private int _count;

    /// <summary>Counts a press.</summary>
    /// <param name="timestampMicroseconds">When the press happened (a pointer point's timestamp, in microseconds).</param>
    /// <param name="x">Where, across the element.</param>
    /// <param name="y">Where, down the element.</param>
    /// <returns>1 for a press that starts a click, 2 for the second press of a double click, and on for each quick press after.</returns>
    internal int Press(ulong timestampMicroseconds, double x, double y)
    {
        bool follows = _count > 0
            && timestampMicroseconds - _lastAt <= DoubleClickMicroseconds
            && Math.Abs(x - _lastX) <= DoubleClickDistance
            && Math.Abs(y - _lastY) <= DoubleClickDistance;
        _count = follows ? _count + 1 : 1;
        _lastAt = timestampMicroseconds;
        _lastX = x;
        _lastY = y;
        return _count;
    }
}
