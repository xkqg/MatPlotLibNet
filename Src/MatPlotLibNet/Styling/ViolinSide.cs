// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which side of the violin to draw.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum ViolinSide
{
    /// <summary>Draw both sides (full violin).</summary>
    Both = 0,

    /// <summary>Draw only the left/lower side.</summary>
    Low = 1,

    /// <summary>Draw only the right/upper side.</summary>
    High = 2,
}
