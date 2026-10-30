// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Avalonia;

/// <summary>What the draw operation needs from the control it draws for, so the chart control and the streaming chart
/// control share one operation: whether the reader can interact, the layout of each paint handed back, and the rubber
/// band to draw over the chart.</summary>
internal interface IInteractiveChart
{
    /// <summary>Whether the reader can interact; the layout is only computed when they can.</summary>
    bool IsInteractive { get; }

    /// <summary>The brush-select drag in progress, or <see langword="null"/>.</summary>
    BrushSelectState? ActiveBrushSelect { get; }

    /// <summary>Called on the render thread after each paint with the layout of that paint.</summary>
    /// <param name="figure">The figure that was painted.</param>
    /// <param name="layoutResult">The layout of the paint.</param>
    void OnRenderCompleted(Figure figure, LayoutResult layoutResult);
}
