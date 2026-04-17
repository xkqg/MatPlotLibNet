// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Readonly state snapshot of the toolbar for platform controls to render as an overlay.
/// Contains button positions, active tool indicator, and layout.</summary>
/// <param name="Buttons">Ordered list of toolbar buttons.</param>
/// <param name="ActiveToolId">ID of the currently active toggle tool.</param>
/// <param name="AnchorX">Pixel X of the toolbar's top-left corner (relative to control).</param>
/// <param name="AnchorY">Pixel Y of the toolbar's top-left corner.</param>
/// <param name="ButtonWidth">Width of each button in pixels.</param>
/// <param name="ButtonHeight">Height of each button in pixels.</param>
public readonly record struct ToolbarState(
    IReadOnlyList<ToolbarButton> Buttons,
    string ActiveToolId,
    double AnchorX, double AnchorY,
    double ButtonWidth, double ButtonHeight);
