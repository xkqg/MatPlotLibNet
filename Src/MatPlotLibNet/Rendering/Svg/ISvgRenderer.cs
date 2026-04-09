// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Renders a <see cref="Figure"/> to a standalone SVG document string.</summary>
public interface ISvgRenderer
{
    /// <summary>Renders the figure to a complete SVG string.</summary>
    string Render(Figure figure);
}
