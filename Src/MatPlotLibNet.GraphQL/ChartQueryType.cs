// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.GraphQL;

/// <summary>GraphQL query type for fetching chart data as SVG or JSON.</summary>
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

    /// <summary>Returns the chart's DATA as HTML tables - the accessible alternative to the rendered picture,
    /// over the same chart id. Empty when the chart has nothing tabular to say.</summary>
    public string GetChartDataTable(
        string chartId,
        [Service] ChartFigureFactory factory)
    {
        var figure = factory.Create(chartId);
        return string.Concat(figure.ToDataTables().Select(table => table.ToHtml()));
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
