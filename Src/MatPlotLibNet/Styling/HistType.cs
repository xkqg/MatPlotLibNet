// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the histogram rendering style.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum HistType
{
    /// <summary>Traditional bar histogram.</summary>
    Bar = 0,

    /// <summary>Unfilled step outline.</summary>
    Step = 1,

    /// <summary>Filled step outline.</summary>
    StepFilled = 2,
}
