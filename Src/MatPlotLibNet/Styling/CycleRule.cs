// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>How a theme hands the colours of its cycle to the series of an axes.</summary>
internal enum CycleRule
{
    /// <summary>One cycle: every series takes the next colour, in the order it was added, as many as it colours parts (the library's
    /// own themes).</summary>
    OneCycle,

    /// <summary>matplotlib's: an axes keeps a cycle for its lines and one for its patches, and a series takes the next colour of the
    /// cycle its kind draws from, or of neither (the default style).</summary>
    Matplotlib,

    /// <summary>matplotlib's in classic mode: as <see cref="Matplotlib"/>, but a scatter, a fill between two curves and a violin take
    /// a colour of the style's and none of either cycle (<c>_internal.classic_mode</c>, the classic style).</summary>
    MatplotlibClassic,
}
