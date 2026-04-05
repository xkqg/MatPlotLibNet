// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using HotChocolate;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.GraphQL;

/// <summary>GraphQL query type for fetching chart data as SVG or JSON.</summary>
[QueryType]
public sealed class ChartQueryType
{
    /// <summary>Returns the chart as pre-rendered SVG markup.</summary>
    public string GetChartSvg(
        string chartId,
        [Service] ChartFigureFactory factory,
        [Service] ISvgRenderer svgRenderer)
    {
        var figure = factory.Create(chartId);
        return svgRenderer.Render(figure);
    }

    /// <summary>Returns the chart as a JSON specification string.</summary>
    public string GetChartJson(
        string chartId,
        [Service] ChartFigureFactory factory,
        [Service] IChartSerializer serializer)
    {
        var figure = factory.Create(chartId);
        return serializer.ToJson(figure);
    }
}
