// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>What a treemap does with a label wider than its rect.</summary>
public enum TreemapLabelFit
{
    /// <summary>Draw every label in full, overflow and all (the default; a static SVG that carries the whole
    /// hierarchy in the DOM, readable by pan/zoom or <c>WithAutoSize</c>).</summary>
    Always = 0,

    /// <summary>Draw a label only when it fits its rect; the rect still carries it in <c>data-treemap-label</c>.
    /// A wall at a fixed size with many small rects: nine of twenty-one labels overflowed at twenty
    /// processes (measured 2026-08-30), painted across their neighbours.</summary>
    Fit = 1,

    /// <summary>Shorten a label to its rect with an ellipsis.</summary>
    Truncate = 2,
}
