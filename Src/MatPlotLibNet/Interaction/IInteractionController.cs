// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Receives platform-neutral input events from a native UI control and routes them to
/// the appropriate <see cref="IInteractionModifier"/>. Raises <see cref="InvalidateRequested"/>
/// after each mutation so the host control knows to repaint.</summary>
public interface IInteractionController
{
    /// <summary>Raised after an event is processed that mutates the figure.
    /// The host control should call its repaint/invalidate method in response.</summary>
    event Action? InvalidateRequested;

    /// <summary>Dispatches a pointer-pressed event to the first matching modifier.</summary>
    void HandlePointerPressed(PointerInputArgs args);
    /// <summary>Dispatches a pointer-moved event to the active modifier (or hover modifier).</summary>
    void HandlePointerMoved(PointerInputArgs args);
    /// <summary>Dispatches a pointer-released event to release any active modifier.</summary>
    void HandlePointerReleased(PointerInputArgs args);
    /// <summary>Dispatches a scroll event (typically zoom).</summary>
    void HandleScroll(ScrollInputArgs args);
    /// <summary>Dispatches a key-down event (typically reset).</summary>
    void HandleKeyDown(KeyInputArgs args);

    /// <summary>Returns the active rubber-band rectangle state if a brush select drag is in
    /// progress, or <c>null</c> otherwise. Used by native controls to draw the selection overlay.</summary>
    BrushSelectState? ActiveBrushSelect { get; }

    /// <summary>Returns hover tooltip content when a nearest data point has been found under
    /// the cursor, or <c>null</c> when no point is within range. Used by native controls to
    /// show a platform tooltip.</summary>
    HoverTooltipContent? ActiveTooltip { get; }

    /// <summary>Rebuilds the layout snapshot from the current figure state.
    /// Call after each render so modifiers have fresh axis limits and plot areas.</summary>
    void UpdateLayout(IChartLayout layout);
}
