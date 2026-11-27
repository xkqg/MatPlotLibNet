// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Where the free end of an axis that scales itself to its data stops, as matplotlib's <c>axes.autolimit_mode</c>
/// says it.</summary>
internal enum AutolimitMode
{
    /// <summary>Where the data and the margin end (<c>'data'</c>, matplotlib's default style).</summary>
    Data,

    /// <summary>Out at the outermost ticks that hold the data and the margin, a sticky edge included (<c>'round_numbers'</c>, the
    /// classic style).</summary>
    RoundNumbers,
}
