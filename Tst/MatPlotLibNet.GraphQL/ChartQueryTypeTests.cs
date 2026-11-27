// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.GraphQL;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="ChartQueryType"/> behavior.</summary>
public class ChartQueryTypeTests
{
    private static readonly IChartSerializer _serializer = new ChartSerializer();
    private static readonly ISvgRenderer _svgRenderer = new SvgTransform(new ChartRenderer());

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that GetChartSvg returns SVG markup containing the chart title.
        new DrawnToken("GetChartSvg_ReturnsSvgContainingSvgTag", () =>
        {
            var factory = new ChartFigureFactory(_ =>
                Plt.Create().WithTitle("Query Test").Plot([1.0, 2.0], [3.0, 4.0]).Build());
            var query = new ChartQueryType();
            return query.GetChartSvg("test", factory, _svgRenderer);
        }, "<svg", "Query Test"),
        //  A client that cannot render an SVG - a screen reader, a text client, an agent - asks for the same chart as a table, over the same id.
        new DrawnToken("GetChartDataTable_ReturnsTheChartAsHtmlRows", () =>
        {
            var factory = new ChartFigureFactory(_ => Plt.Create()
                .WithTitle("Query Table")
                .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Quarter").Plot([1.0, 2.0], [12.0, 18.0], s => s.Label = "value"))
                .Build());
            return new ChartQueryType().GetChartDataTable("test", factory);
        }, "<caption>Query Table</caption>", "<th scope=\"col\">Quarter</th>", "<td>18</td>"),
        //  Verifies that GetChartJson returns valid JSON containing the chart title.
        new DrawnToken("GetChartJson_ReturnsValidJson", () =>
        {
            var factory = new ChartFigureFactory(_ =>
                Plt.Create().WithTitle("JSON Query").Build());
            return new ChartQueryType().GetChartJson("test", factory, _serializer);
        }, "\"title\":\"JSON Query\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A figure with nothing to table answers with an empty string, never a broken fragment.</summary>
    [Fact]
    public void GetChartDataTable_EmptyInput_IsAnEmptyString()
    {
        var factory = new ChartFigureFactory(_ => Plt.Create().Build());

        Assert.Equal("", new ChartQueryType().GetChartDataTable("test", factory));
    }

    /// <summary>Verifies that GetChartSvg passes the chart ID to the figure factory.</summary>
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
