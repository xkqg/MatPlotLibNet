// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Manages selection and deletion of existing drawing tools. Callers use
/// <see cref="SelectTool"/> to programmatically select a tool; the Delete key then
/// emits a <see cref="RemoveDrawingToolEvent"/> for the selected tool.</summary>
public sealed class DrawingToolSelectionModifier : InteractionModifier
{
    private object? _selectedTool;
    private int _selectedAxesIndex;

    /// <summary>Creates a modifier that removes the selected drawing tool when the Delete key is pressed.</summary>
    /// <param name="chartId">The identifier of the chart that the emitted <see cref="RemoveDrawingToolEvent"/> is addressed to.</param>
    /// <param name="layout">The chart layout of the rendered figure.</param>
    /// <param name="sink">Receives the event this modifier emits when a selected tool is deleted.</param>
    public DrawingToolSelectionModifier(
        string chartId,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <summary>Programmatically selects a drawing tool for deletion.</summary>
    public void SelectTool(object tool, int axesIndex)
    {
        _selectedTool = tool;
        _selectedAxesIndex = axesIndex;
    }

    /// <summary>Clears the current selection.</summary>
    public void ClearSelection() => _selectedTool = null;

    /// <inheritdoc />
    public override bool HandlesKeyDown(KeyInputArgs args) =>
        _selectedTool is not null && args.Key == "Delete";

    /// <inheritdoc />
    public override void OnKeyDown(KeyInputArgs args)
    {
        if (_selectedTool is null) return;
        Emit(new RemoveDrawingToolEvent(ChartId, _selectedAxesIndex, _selectedTool));
        _selectedTool = null;
    }
}
