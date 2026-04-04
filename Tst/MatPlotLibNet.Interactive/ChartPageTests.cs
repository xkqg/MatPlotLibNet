// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interactive;

namespace MatPlotLibNet.Interactive.Tests;

public class ChartPageTests
{
    private const string TestChartId = "test-chart-123";
    private const string TestSvg = "<svg><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";
    private const int TestPort = 5123;

    [Fact]
    public void Generate_ContainsDoctype()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.StartsWith("<!DOCTYPE html>", html);
    }

    [Fact]
    public void Generate_ContainsInitialSvg()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains(TestSvg, html);
    }

    [Fact]
    public void Generate_ContainsChartIdInScript()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains($"\"{TestChartId}\"", html);
    }

    [Fact]
    public void Generate_ContainsCorrectPort()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains($"localhost:{TestPort}", html);
    }

    [Fact]
    public void Generate_ContainsSignalRScriptReference()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("/js/signalr.min.js", html);
    }

    [Fact]
    public void Generate_ContainsSubscribeCall()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("Subscribe", html);
    }

    [Fact]
    public void Generate_ContainsUpdateChartSvgHandler()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("UpdateChartSvg", html);
    }

    [Fact]
    public void Generate_EncodesChartIdForHtml()
    {
        var html = ChartPage.Generate("<script>alert(1)</script>", TestSvg, TestPort);
        Assert.DoesNotContain("<script>alert(1)</script>", html.Split("</head>")[0]);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Generate_ContainsChartContainer()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("chart-container", html);
    }

    [Fact]
    public void Generate_ContainsAutoReconnect()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("withAutomaticReconnect", html);
    }
}
