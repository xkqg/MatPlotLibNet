// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor;

/// <summary>Extension methods for rendering figures in Blazor components.</summary>
public static class BlazorExtensions
{
    /// <summary>Renders the figure as a Blazor <see cref="MarkupString"/> containing SVG markup for use in Razor templates.</summary>
    /// <returns>A <see cref="MarkupString"/> containing the SVG output.</returns>
    public static MarkupString ToMarkupString(this Figure figure) =>
        new(ChartServices.SvgRenderer.Render(figure));
}
