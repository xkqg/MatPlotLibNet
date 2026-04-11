// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which side of the violin to draw.</summary>
public enum ViolinSide
{
    /// <summary>Draw both sides (full violin).</summary>
    Both,

    /// <summary>Draw only the left/lower side.</summary>
    Low,

    /// <summary>Draw only the right/upper side.</summary>
    High
}
