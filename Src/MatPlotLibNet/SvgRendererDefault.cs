// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>The default <see cref="ISvgRenderer"/> for non-DI scenarios. One replaceable slot, separated from
/// every other default.</summary>
public static class SvgRendererDefault
{
    private static volatile ISvgRenderer _current = new SvgTransform(new ChartRenderer());

    /// <summary>Gets or sets the default SVG renderer.</summary>
    public static ISvgRenderer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}
