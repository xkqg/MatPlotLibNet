// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>A renderer of the caller's, as a test hands one to an exporter or installs it: it draws what <paramref name="render"/> says,
/// lays out what <paramref name="layout"/> gives (left out, no plot area) and counts both.</summary>
/// <param name="render">What the renderer draws on a surface.</param>
/// <param name="layout">The layout the renderer works out; left out, none.</param>
internal sealed class RecordingChartRenderer(Action<Figure, IRenderContext> render, Func<LayoutResult>? layout = null) : IChartRenderer
{
    /// <summary>How many times the renderer drew.</summary>
    public int Renders { get; private set; }

    /// <summary>How many times the renderer laid out.</summary>
    public int Layouts { get; private set; }

    public void Render(Figure figure, IRenderContext ctx)
    {
        Renders++;
        render(figure, ctx);
    }

    public LayoutResult ComputeLayout(Figure figure, IRenderContext measureCtx)
    {
        Layouts++;
        return layout?.Invoke() ?? new LayoutResult([], []);
    }
}
