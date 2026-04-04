// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>Controls how a chart is displayed within a UI component.</summary>
public enum DisplayMode
{
    /// <summary>Chart renders inline as SVG within the component (default).</summary>
    Inline,

    /// <summary>Chart renders inline with a fullscreen overlay toggle button.</summary>
    Expandable,

    /// <summary>Chart renders as a thumbnail with a link to open in a new browser tab.</summary>
    Popup
}
