// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.GraphQL;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.GraphQL.Tests;

public class ChartQueryTypeTests
{
    private readonly IChartSerializer _serializer = new ChartSerializer();
    private readonly ISvgRenderer _svgRenderer = new SvgTransform(new ChartRenderer());

    [Fact]
    public void GetChartSvg_ReturnsSvgContainingSvgTag()
    {
        var factory = new ChartFigureFactory(_ =>
            Plt.Create().WithTitle("Query Test").Plot([1.0, 2.0], [3.0, 4.0]).Build());
        var query = new ChartQueryType();

        var svg = query.GetChartSvg("test", factory, _svgRenderer);

        Assert.Contains("<svg", svg);
        Assert.Contains("Query Test", svg);
    }

    [Fact]
    public void GetChartJson_ReturnsValidJson()
    {
        var factory = new ChartFigureFactory(_ =>
            Plt.Create().WithTitle("JSON Query").Build());
        var query = new ChartQueryType();

        var json = query.GetChartJson("test", factory, _serializer);

        Assert.Contains("\"title\":\"JSON Query\"", json);
    }

    [Fact]
    public void GetChartSvg_PassesChartIdToFactory()
    {
        string? receivedId = null;
        var factory = new ChartFigureFactory(id =>
        {
            receivedId = id;
            return Plt.Create().Build();
        });
        var query = new ChartQueryType();

        query.GetChartSvg("sensor-42", factory, _svgRenderer);

        Assert.Equal("sensor-42", receivedId);
    }
}
