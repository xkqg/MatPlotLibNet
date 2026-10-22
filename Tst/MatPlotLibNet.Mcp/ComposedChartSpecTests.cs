// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>An agent hands the reader the JSON a figure writes. A composed chart is just series, axes and overlays, so
/// the reader must take every shape the core suite draws: a refusal here would turn a verb's output into a chart no
/// agent can send back.</summary>
public class ComposedChartSpecTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void TheJsonOfAComposedChart_IsAcceptedByTheSpecReader(string name)
    {
        string json;
        try { json = ComposedChartShapes.Named(name).Build(false).ToJson(); }
        catch (Exception ex) { Assert.Fail($"{name}: ToJson threw {ex.GetType().Name}: {ex.Message}"); throw; }

        var refusal = Record.Exception(() => Reader.Read(new ChartSpec(json)));

        Assert.True(refusal is null, $"{name}: the spec reader refused the JSON: {refusal?.Message}");
    }
}
