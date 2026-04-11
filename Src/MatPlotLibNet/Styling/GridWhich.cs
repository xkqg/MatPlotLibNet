// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which tick level receives grid lines.</summary>
public enum GridWhich
{
    /// <summary>Draw grid lines only at major tick positions (default).</summary>
    Major,

    /// <summary>Draw grid lines only at minor tick positions.</summary>
    Minor,

    /// <summary>Draw grid lines at both major and minor tick positions.</summary>
    Both
}
