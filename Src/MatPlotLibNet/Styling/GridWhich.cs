// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which tick level receives grid lines.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum GridWhich
{
    /// <summary>Draw grid lines only at major tick positions (default).</summary>
    Major = 0,

    /// <summary>Draw grid lines only at minor tick positions.</summary>
    Minor = 1,

    /// <summary>Draw grid lines at both major and minor tick positions.</summary>
    Both = 2,
}
