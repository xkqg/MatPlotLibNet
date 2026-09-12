// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MatPlotLibNet.Mcp;

/// <summary>The five tools, and the only file in this package that the MCP SDK touches. Everything it calls is a
/// plain class a test can drive without a protocol host; what lives here is the wire contract — the tool names, the
/// parameter names and the one error boundary.
/// <para>That boundary is load-bearing: the SDK forwards the message of an <see cref="McpException"/> and replaces
/// every other exception with "An error occurred invoking '&lt;tool&gt;'." So a refusal that names the offending
/// field only reaches the model if it is rethrown as one, and a model that is told nothing retries the same
/// mistake.</para></summary>
internal sealed class ChartTools
{
    private readonly ChartRendering _rendering;
    private readonly ChartTabulation _tabulation;
    private readonly ChartTypeCatalog _catalog;
    private readonly ChartSchemaDescription _schema;
    private readonly OutputPathResolver _output;

    /// <summary>Creates the tool surface over the collaborators the host resolves.</summary>
    public ChartTools(
        ChartRendering rendering,
        ChartTabulation tabulation,
        ChartTypeCatalog catalog,
        ChartSchemaDescription schema,
        OutputPathResolver output)
    {
        _rendering = rendering;
        _tabulation = tabulation;
        _catalog = catalog;
        _schema = schema;
        _output = output;
    }

    [McpServerTool(Name = "render_chart", ReadOnly = true)]
    [Description("Renders a chart from a MatPlotLibNet figure-JSON spec and returns it as a PNG image, with a short "
        + "text summary of what was drawn. Call describe_chart_schema for the shape of the spec and list_chart_types "
        + "for the chart types. For SVG or PDF, use save_chart — they are too large to return inline.")]
    public IEnumerable<ContentBlock> RenderChart(
        [Description("The chart spec: MatPlotLibNet figure JSON, e.g. {\"width\":800,\"height\":600,\"subPlots\":[{\"series\":[{\"type\":\"line\",\"xData\":[1,2],\"yData\":[3,4]}]}]}")]
        JsonElement spec,
        [Description("The image format. Only png can be returned inline.")]
        ChartFormat format = ChartFormat.Png) =>
        Guarded(() =>
        {
            var result = _rendering.Render(ChartSpec.From(spec), format);
            return (IEnumerable<ContentBlock>)
            [
                ImageContentBlock.FromBytes(result.Bytes, result.MediaType),
                new TextContentBlock { Text = result.Summary.ToText() },
            ];
        });

    [McpServerTool(Name = "save_chart")]
    [Description("Renders a chart from a MatPlotLibNet figure-JSON spec and writes it to a file, returning the path "
        + "it wrote. Use this for SVG and PDF, and for images too large to return inline. The file is written under "
        + "the server's output directory; an existing file is kept unless overwrite is true.")]
    public string SaveChart(
        [Description("The chart spec: MatPlotLibNet figure JSON.")]
        JsonElement spec,
        [Description("File name or relative path, with the extension of the format (.png, .svg or .pdf).")]
        string path,
        [Description("The output format; it must match the file extension.")]
        ChartFormat format,
        [Description("Replace the file when it already exists. Off by default: the server never destroys a file it did not make.")]
        bool overwrite = false) =>
        Guarded(() =>
        {
            var result = _rendering.Save(ChartSpec.From(spec), format, path, overwrite, _output);
            return $"Wrote {result.Path} ({result.ByteCount:N0} bytes, {result.FormatWritten.ToString().ToLowerInvariant()}).\n{result.Summary.ToText()}";
        });

    [McpServerTool(Name = "chart_data_table", ReadOnly = true)]
    [Description("Returns the DATA of a chart spec as markdown tables - the numbers the picture is drawn from, "
        + "which an image cannot be read for. One table per group of series that share an x. Long charts are "
        + "refused with the limit in the message; save the chart and read the file instead.")]
    public string ChartDataTable(
        [Description("The chart spec: MatPlotLibNet figure JSON, the same document render_chart takes.")]
        JsonElement spec) =>
        Guarded(() => _tabulation.Describe(ChartSpec.From(spec)));

    [McpServerTool(Name = "list_chart_types", ReadOnly = true)]
    [Description("Lists the chart types a spec may name in a series' \"type\" field.")]
    public string ListChartTypes() => Guarded(() =>
    {
        string listed = $"{_catalog.Listed.Count} chart types:\n  {string.Join("\n  ", _catalog.Listed)}";
        return _catalog.Excluded.Count == 0
            ? listed
            : listed + "\n\nNot available in this version:\n  "
                + string.Join("\n  ", _catalog.Excluded.Select(pair => $"{pair.Key} — {pair.Value}"));
    });

    [McpServerTool(Name = "describe_chart_schema", ReadOnly = true)]
    [Description("Describes the fields of a chart spec, with a worked example. Pass a chart type to see the fields "
        + "of that type; omit it for the shape of the whole spec.")]
    public string DescribeChartSchema(
        [Description("A chart type from list_chart_types, or omit for the whole spec.")]
        string? seriesType = null) =>
        Guarded(() => _schema.Describe(seriesType));

    /// <summary>Turns a refusal into the one exception type whose message the SDK forwards to the model, and keeps
    /// anything unforeseen from reaching it as an unexplained "An error occurred". No tool here takes a
    /// cancellation token and nothing below throws <see cref="McpException"/> itself, so there is no third arm to
    /// guard against — the day a tool gains one, it gains a test with it.</summary>
    private static T Guarded<T>(Func<T> act)
    {
        try
        {
            return act();
        }
        catch (ToolRefusalException refusal)
        {
            throw new McpException(refusal.Message);
        }
        catch (Exception ex)
        {
            throw new McpException($"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
