// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>Provides default service instances for non-DI scenarios (console apps, scripts).</summary>
public static class ChartServices
{
    private static volatile IChartSerializer _serializer = new ChartSerializer();
    private static volatile IChartRenderer _renderer = new ChartRenderer();
    private static volatile ISvgRenderer _svgRenderer = new SvgTransform(new ChartRenderer());

    /// <summary>Gets or sets the default chart serializer.</summary>
    public static IChartSerializer Serializer
    {
        get => _serializer;
        set => _serializer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the default chart renderer.</summary>
    public static IChartRenderer Renderer
    {
        get => _renderer;
        set => _renderer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the default SVG renderer.</summary>
    public static ISvgRenderer SvgRenderer
    {
        get => _svgRenderer;
        set => _svgRenderer = value ?? throw new ArgumentNullException(nameof(value));
    }
}
