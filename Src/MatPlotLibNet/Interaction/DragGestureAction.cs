// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>What a one-finger drag does on a touch surface. A mouse says what it means with a modifier key (Shift for a
/// band, Ctrl for a rectangle zoom, Alt for a span, the right button for a rotation); a finger has none, so the control says.</summary>
public enum DragGestureAction
{
    /// <summary>The data follows the finger.</summary>
    Pan = 0,

    /// <summary>The drag draws a rectangle, and the chart zooms into it.</summary>
    RectangleZoom = 1,

    /// <summary>The drag draws a band, and the data inside it is selected.</summary>
    BrushSelect = 2,

    /// <summary>The drag selects a span of X.</summary>
    SpanSelect = 3,

    /// <summary>The drag turns the camera of a 3-D chart.</summary>
    Rotate = 4,
}
