// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Granularity level chosen by <see cref="AutoDateLocator"/> based on the visible date range.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum DateInterval
{
    /// <summary>Tick at the start of each year (January 1st).</summary>
    Years   = 0,

    /// <summary>Tick at the start of each month (1st day).</summary>
    Months  = 1,

    /// <summary>Tick at the start of each week (Monday midnight).</summary>
    Weeks   = 2,

    /// <summary>Tick at midnight of each day.</summary>
    Days    = 3,

    /// <summary>Tick at the start of each hour.</summary>
    Hours   = 4,

    /// <summary>Tick at the start of each minute.</summary>
    Minutes = 5,

    /// <summary>Tick at the start of each second.</summary>
    Seconds = 6,
}
