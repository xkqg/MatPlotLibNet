// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Platform-neutral data for a pointer pressed, moved, or released event.</summary>
/// <param name="X">Pixel X position relative to the control's top-left corner.</param>
/// <param name="Y">Pixel Y position relative to the control's top-left corner.</param>
/// <param name="Button">Which button is involved.</param>
/// <param name="Modifiers">Keyboard modifier keys held at the time of the event.</param>
/// <param name="ClickCount">Number of consecutive clicks (1 = single, 2 = double-click).</param>
public readonly record struct PointerInputArgs(
    double X,
    double Y,
    PointerButton Button,
    ModifierKeys Modifiers,
    int ClickCount = 1);
