// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Identifies which mouse/pointer button is involved in an event.</summary>
public enum PointerButton
{
    /// <summary>No button — e.g. pure hover/move.</summary>
    None,
    /// <summary>Primary (left) mouse button.</summary>
    Left,
    /// <summary>Middle (scroll-wheel) mouse button.</summary>
    Middle,
    /// <summary>Secondary (right) mouse button.</summary>
    Right,
}
