// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which tick level receives grid lines.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum GridWhich
{
    /// <summary>Draw grid lines only at major tick positions (default).</summary>
    Major = 0,

    /// <summary>Draw grid lines only at minor tick positions. Under the matplotlib themes an axis draws them only where its minor
    /// ticks stand: when they are on (<c>WithMinorTicks</c>), or on a log or a logit scale unless they were switched off, as
    /// matplotlib's minor grid does.</summary>
    Minor = 1,

    /// <summary>Draw grid lines at both major and minor tick positions; the minor ones as <see cref="Minor"/> says.</summary>
    Both = 2,
}
