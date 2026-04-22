// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Manages selection and deletion of existing drawing tools. Callers use
/// <see cref="SelectTool"/> to programmatically select a tool; the Delete key then
/// emits a <see cref="RemoveDrawingToolEvent"/> for the selected tool.</summary>
public sealed class DrawingToolSelectionModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly InteractionToolbar _toolbar;

    private object? _selectedTool;
    private int _selectedAxesIndex;

    public DrawingToolSelectionModifier(
        string chartId,
        IChartLayout layout,
        Figure figure,
        Action<FigureInteractionEvent> sink,
        InteractionToolbar toolbar)
    {
        _chartId = chartId;
        _layout  = layout;
        _figure  = figure;
        _sink    = sink;
        _toolbar = toolbar;
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
    public bool HandlesPointerPressed(PointerInputArgs args) => false;
    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) { }
    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) =>
        _selectedTool is not null && args.Key == "Delete";

    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args)
    {
        if (_selectedTool is null) return;
        _sink(new RemoveDrawingToolEvent(_chartId, _selectedAxesIndex, _selectedTool));
        _selectedTool = null;
    }
}
