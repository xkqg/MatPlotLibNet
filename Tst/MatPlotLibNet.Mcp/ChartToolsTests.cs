// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The tool surface is a wire contract: a host addresses these four names, a model fills these parameter
/// names, and renaming either is a break no compiler reports. So the names are pinned, and so is the rule that
/// every failure leaves as an <see cref="McpException"/> — the SDK forwards the message of that one type and
/// replaces every other exception with "An error occurred invoking '&lt;tool&gt;'", which tells a model nothing it
/// can correct.</summary>
public class ChartToolsTests : IDisposable
{
    private readonly DirectoryInfo _output = Directory.CreateTempSubdirectory("mcp-tool-tests");
    private readonly ChartTools _tools;

    public ChartToolsTests()
    {
        var catalog = new ChartTypeCatalog();
        var reader = new ChartSpecReader(catalog, RenderLimits.Default);
        _tools = new ChartTools(
            new ChartRendering(reader, new ChartSummarizer()),
            new ChartTabulation(reader, RenderLimits.Default),
            catalog,
            new ChartSchemaDescription(catalog),
            new OutputPathResolver(_output.FullName));
    }

    public void Dispose()
    {
        _output.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private static JsonElement Spec(string json) => JsonDocument.Parse(json).RootElement;

    private static readonly string LineJson =
        """{"width":400,"height":300,"title":"Revenue","subPlots":[{"series":[{"type":"line","xData":[1,2,3],"yData":[2,4,3]}]}]}""";

    // ---- the four names and their parameters, frozen ---------------------------------------------------------

    private static MethodInfo[] ToolMethods() =>
        [.. typeof(ChartTools).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)];

    [Fact]
    public void TheFourToolNames_AreExactlyTheAgreedOnes()
    {
        var names = ToolMethods()
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()!.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(["chart_data_table", "describe_chart_schema", "list_chart_types", "render_chart", "save_chart"], names);
    }

    [Theory]
    [InlineData("chart_data_table", "spec")]
    [InlineData("render_chart", "spec,format")]
    [InlineData("save_chart", "spec,path,format,overwrite")]
    [InlineData("list_chart_types", "")]
    [InlineData("describe_chart_schema", "seriesType")]
    public void EveryToolParameterName_IsExactlyTheAgreedOne(string tool, string parameters)
    {
        var method = ToolMethods().Single(m => m.GetCustomAttribute<McpServerToolAttribute>()!.Name == tool);

        Assert.Equal(parameters, string.Join(',', method.GetParameters().Select(p => p.Name)));
    }

    [Fact]
    public void EveryTool_TellsTheModelWhatItIsFor()
    {
        foreach (var method in ToolMethods())
        {
            Assert.NotNull(method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>());
            Assert.All(method.GetParameters(),
                p => Assert.NotNull(p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()));
        }
    }

    private static McpServerToolAttribute Annotation(string tool) =>
        ToolMethods().Single(m => m.GetCustomAttribute<McpServerToolAttribute>()!.Name == tool)
            .GetCustomAttribute<McpServerToolAttribute>()!;

    /// <summary>Each tool, and the four things a host is allowed to assume about it: whether it changes anything,
    /// whether it can destroy something, whether calling it twice does more than calling it once, and whether it
    /// reaches out to an unpredictable world. Left unsaid, two of the four are assumed the WRONG way round — the
    /// protocol defaults a tool to destructive and to open-world — so a host may warn a user about a tool that
    /// only draws a picture, or refuse to auto-run one that reads nothing but its own arguments.</summary>
    [Theory]
    //          tool                     read-only  destructive  idempotent  open world
    [InlineData("render_chart",          true,      false,       true,       false)]
    [InlineData("save_chart",            false,     true,        true,       false)]
    [InlineData("chart_data_table",      true,      false,       true,       false)]
    [InlineData("list_chart_types",      true,      false,       true,       false)]
    [InlineData("describe_chart_schema", true,      false,       true,       false)]
    public void EveryTool_SaysWhatItDoesToItsEnvironment(string tool, bool readOnly, bool destructive, bool idempotent, bool openWorld)
    {
        var annotation = Annotation(tool);

        // save_chart writes a file, and with overwrite: true it replaces one it did not make — that is destructive
        // and the annotation says so rather than softening it. It is still idempotent: the same call twice leaves
        // the same bytes on disk, because the second one either refuses or writes the same picture again.
        Assert.Equal(readOnly, annotation.ReadOnly);
        Assert.Equal(destructive, annotation.Destructive);
        Assert.Equal(idempotent, annotation.Idempotent);
        Assert.Equal(openWorld, annotation.OpenWorld);
    }

    [Theory]
    [InlineData("render_chart", "Render a chart as an image")]
    [InlineData("save_chart", "Save a chart to a file")]
    [InlineData("chart_data_table", "Read a chart's data as a table")]
    [InlineData("list_chart_types", "List the chart types")]
    [InlineData("describe_chart_schema", "Describe the chart spec")]
    public void EveryTool_CarriesATitleAPersonCanRead(string tool, string title)
    {
        // The name is for the protocol; the title is what a user sees in the list of tools a server offers.
        // Without one, a host shows "chart_data_table".
        Assert.Equal(title, Annotation(tool).Title);
    }

    // ---- what the tools return -------------------------------------------------------------------------------

    [Fact]
    public void RenderChart_ReturnsThePictureAndTheSummaryBesideIt()
    {
        var blocks = _tools.RenderChart(Spec(LineJson)).ToArray();

        var image = Assert.IsType<ImageContentBlock>(blocks[0]);
        Assert.Equal("image/png", image.MimeType);
        Assert.False(image.Data.IsEmpty);
        Assert.Contains("Revenue", Assert.IsType<TextContentBlock>(blocks[1]).Text);
    }

    [Fact]
    public void SaveChart_ReportsThePathItWrote()
    {
        string message = _tools.SaveChart(Spec(LineJson), "chart.svg", ChartFormat.Svg);

        string path = Path.Combine(_output.FullName, "chart.svg");
        Assert.Contains(path, message);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ListChartTypes_NamesTheTypesAndWhatIsUnavailable()
    {
        string listed = _tools.ListChartTypes();

        Assert.Contains("line", listed);
        Assert.Contains("sankey", listed);   // named under "not available", with its reason
        Assert.Contains("Not available", listed);
    }

    [Fact]
    public void DescribeChartSchema_AnswersWithAndWithoutAType()
    {
        Assert.Contains("subPlots", _tools.DescribeChartSchema());
        Assert.Contains("'candlestick'", _tools.DescribeChartSchema("candlestick"));
    }

    // ---- the error boundary ----------------------------------------------------------------------------------

    [Fact]
    public void ARefusal_ReachesTheModelAsAMessageItCanActon()
    {
        var error = Assert.Throws<McpException>(() =>
            _tools.RenderChart(Spec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"lien","xData":[1],"yData":[1]}]}]}""")).ToArray());

        Assert.Contains("'lien'", error.Message);
        Assert.Contains("'line'", error.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"width":400,"height":300}""")]
    [InlineData("""{"width":99999,"height":99999,"subPlots":[{"series":[{"type":"line","xData":[1],"yData":[1]}]}]}""")]
    public void EveryWayASpecCanBeWrong_LeavesAsAnMcpException(string json)
    {
        var element = json == "not json" ? Spec("\"not json\"") : Spec(json);

        Assert.Throws<McpException>(() => _tools.RenderChart(element).ToArray());
    }

    [Fact]
    public void APathOutsideTheOutputRoot_LeavesAsAnMcpException()
    {
        var error = Assert.Throws<McpException>(() => _tools.SaveChart(Spec(LineJson), "../escape.png", ChartFormat.Png));

        Assert.Contains(_output.FullName, error.Message);
    }

    [Fact]
    public void AnUnknownChartTypeInTheSchemaTool_LeavesAsAnMcpException() =>
        Assert.Throws<McpException>(() => _tools.DescribeChartSchema("nope"));
}
