// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>Controls how a chart is displayed within a UI component.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. New values get the next unused ordinal at the end. See
/// <c>EnumOrdinalContractTests</c> for the CI enforcement gate.</remarks>
public enum DisplayMode
{
    /// <summary>Chart renders inline as SVG within the component (default).</summary>
    Inline = 0,

    /// <summary>Chart renders inline with a fullscreen overlay toggle button.</summary>
    Expandable = 1,

    /// <summary>Chart renders as a thumbnail with a link to open in a new browser tab.</summary>
    Popup = 2,
}
