// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>The gate of a drawing tool: a press opens it while the toolbar holds the tool, with the left button, over an axes. The
/// press is then read through the layout (<see cref="ChartLayoutViewExtensions"/>'s <c>PressAt</c>). The trendline, the level and the
/// Fibonacci retracement each hold one.</summary>
/// <param name="Toolbar">The toolbar that says which tool is active.</param>
/// <param name="Tool">The tool the gate opens for.</param>
internal readonly record struct ToolGate(InteractionToolbar Toolbar, ToolMode Tool)
{
    /// <summary>Whether <paramref name="args"/> opens the gate: its tool is the active one and it is a left press on an axes of
    /// <paramref name="layout"/>.</summary>
    /// <param name="args">The press.</param>
    /// <param name="layout">The layout of the rendered figure.</param>
    internal bool Opens(PointerInputArgs args, IChartLayout layout) => Toolbar.ActiveTool == Tool && layout.IsLeftPressOnAxes(args);
}
