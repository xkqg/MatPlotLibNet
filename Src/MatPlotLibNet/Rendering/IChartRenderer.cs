// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a <see cref="Figure"/> onto an <see cref="IRenderContext"/>.</summary>
public interface IChartRenderer
{
    /// <summary>Renders the entire figure including background, title, and all subplots.</summary>
    void Render(Figure figure, IRenderContext ctx);
}
