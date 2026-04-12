// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies <see cref="SvgRenderContext.SetNextElementData"/> emits and clears data attributes.</summary>
public class SetNextElementDataTests
{
    [Fact]
    public void SetNextElementData_EmitsDataAttribute_OnPolygon()
    {
        var ctx = new SvgRenderContext();
        ctx.SetNextElementData("v3d", "0.1,0.2,0.3");

        var pts = new[] { new Point(10, 10), new Point(50, 10), new Point(50, 50), new Point(10, 50) };
        ctx.DrawPolygon(pts, Color.FromHex("#FF0000"), null, 0);

        var sb = new System.Text.StringBuilder();
        ctx.WriteTo(sb);
        var svg = sb.ToString();

        Assert.Contains("data-v3d=\"0.1,0.2,0.3\"", svg);
    }

    [Fact]
    public void SetNextElementData_ClearedAfterUse()
    {
        var ctx = new SvgRenderContext();
        ctx.SetNextElementData("v3d", "test-value");

        var pts = new[] { new Point(10, 10), new Point(50, 10), new Point(50, 50) };
        ctx.DrawPolygon(pts, Color.FromHex("#FF0000"), null, 0);
        // Second polygon should NOT have the data attribute
        ctx.DrawPolygon(pts, Color.FromHex("#0000FF"), null, 0);

        var sb = new System.Text.StringBuilder();
        ctx.WriteTo(sb);
        var svg = sb.ToString();

        // data-v3d should appear exactly once
        Assert.Equal(1, CountOccurrences(svg, "data-v3d="));
    }

    [Fact]
    public void SetNextElementData_DefaultInterface_NoOp()
    {
        // IRenderContext default implementation should not throw
        IRenderContext ctx = new SvgRenderContext();
        var exception = Record.Exception(() => ctx.SetNextElementData("key", "value"));
        Assert.Null(exception);
    }

    private static int CountOccurrences(string source, string pattern)
    {
        int count = 0, idx = 0;
        while ((idx = source.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
