// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Platform-neutral data for a scroll-wheel event.</summary>
/// <param name="X">Pixel X position of the cursor relative to the control.</param>
/// <param name="Y">Pixel Y position of the cursor relative to the control.</param>
/// <param name="DeltaX">Horizontal scroll amount (positive = right).</param>
/// <param name="DeltaY">Vertical scroll amount (positive = down / away from user).</param>
/// <param name="Modifiers">Keyboard modifier keys held at the time of the event.</param>
public readonly record struct ScrollInputArgs(
    double X,
    double Y,
    double DeltaX,
    double DeltaY,
    ModifierKeys Modifiers = ModifierKeys.None);
