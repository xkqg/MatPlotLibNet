// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Identifies which mouse/pointer button is involved in an event.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum PointerButton
{
    /// <summary>No button — e.g. pure hover/move.</summary>
    None = 0,
    /// <summary>Primary (left) mouse button.</summary>
    Left = 1,
    /// <summary>Middle (scroll-wheel) mouse button.</summary>
    Middle = 2,
    /// <summary>Secondary (right) mouse button.</summary>
    Right = 3,
}
