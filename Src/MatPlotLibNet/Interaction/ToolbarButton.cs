// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Defines a single button in the interactive toolbar overlay.</summary>
/// <param name="Id">Unique identifier (e.g. "pan", "zoom", "rotate3d", "home", "save").</param>
/// <param name="Label">Tooltip text displayed on hover.</param>
/// <param name="IsToggle">Whether this button represents a modal tool (pan vs zoom vs rotate).</param>
public sealed record ToolbarButton(string Id, string Label, bool IsToggle = false);
