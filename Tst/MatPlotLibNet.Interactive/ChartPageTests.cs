// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interactive;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="ChartPage"/> behavior.</summary>
public class ChartPageTests
{
    private const string TestChartId = "test-chart-123";
    private const string TestSvg = "<svg><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";
    private const int TestPort = 5123;

    /// <summary>Verifies that the generated HTML starts with a DOCTYPE declaration.</summary>
    [Fact]
    public void Generate_ContainsDoctype()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.StartsWith("<!DOCTYPE html>", html);
    }

    /// <summary>Verifies that the generated HTML contains the initial SVG content.</summary>
    [Fact]
    public void Generate_ContainsInitialSvg()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains(TestSvg, html);
    }

    /// <summary>Verifies that the generated HTML includes the chart ID in the script section.</summary>
    [Fact]
    public void Generate_ContainsChartIdInScript()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains($"\"{TestChartId}\"", html);
    }

    /// <summary>Verifies that the generated HTML references the correct localhost port.</summary>
    [Fact]
    public void Generate_ContainsCorrectPort()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains($"localhost:{TestPort}", html);
    }

    /// <summary>Verifies that the generated HTML includes a reference to the SignalR JavaScript file.</summary>
    [Fact]
    public void Generate_ContainsSignalRScriptReference()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("/js/signalr.min.js", html);
    }

    /// <summary>Verifies that the generated HTML contains a Subscribe call for real-time updates.</summary>
    [Fact]
    public void Generate_ContainsSubscribeCall()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("Subscribe", html);
    }

    /// <summary>Verifies that the generated HTML contains an UpdateChartSvg handler for live SVG updates.</summary>
    [Fact]
    public void Generate_ContainsUpdateChartSvgHandler()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("UpdateChartSvg", html);
    }

    /// <summary>Verifies that the chart ID is HTML-encoded to prevent script injection.</summary>
    [Fact]
    public void Generate_EncodesChartIdForHtml()
    {
        var html = ChartPage.Generate("<script>alert(1)</script>", TestSvg, TestPort);
        Assert.DoesNotContain("<script>alert(1)</script>", html.Split("</head>")[0]);
        Assert.Contains("&lt;script&gt;", html);
    }

    /// <summary>Verifies that the generated HTML contains a chart-container element.</summary>
    [Fact]
    public void Generate_ContainsChartContainer()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("chart-container", html);
    }

    /// <summary>Verifies that the generated HTML includes automatic reconnect configuration for SignalR.</summary>
    [Fact]
    public void Generate_ContainsAutoReconnect()
    {
        var html = ChartPage.Generate(TestChartId, TestSvg, TestPort);
        Assert.Contains("withAutomaticReconnect", html);
    }
}
