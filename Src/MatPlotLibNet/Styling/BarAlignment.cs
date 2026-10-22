// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies how bar positions are aligned relative to their X coordinate.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum BarAlignment
{
    /// <summary>Bar is centered on the X coordinate.</summary>
    Center = 0,

    /// <summary>Bar's left edge is at the X coordinate.</summary>
    Edge = 1,
}
