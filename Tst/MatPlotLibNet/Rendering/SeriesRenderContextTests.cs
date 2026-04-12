// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="SeriesRenderContext"/> camera and lighting fields.</summary>
public class SeriesRenderContextTests
{
    private static SeriesRenderContext CreateContext()
    {
        var ctx = new SvgRenderContext();
        var area = new RenderArea(new Rect(0, 0, 400, 300), ctx);
        return new SeriesRenderContext(new DataTransform(0, 1, 0, 1, new Rect(0, 0, 400, 300)),
            ctx, Color.FromHex("#4477AA"), area);
    }

    [Fact]
    public void SeriesRenderContext_Projection3D_DefaultIsNull()
    {
        var ctx = CreateContext();
        Assert.Null(ctx.Projection3D);
    }

    [Fact]
    public void SeriesRenderContext_Projection3D_CanBeSetViaWith()
    {
        var proj = new Projection3D(45, -30, new Rect(0, 0, 400, 300), 0, 1, 0, 1, 0, 1);
        var ctx = CreateContext() with { Projection3D = proj };
        Assert.Same(proj, ctx.Projection3D);
    }
}
