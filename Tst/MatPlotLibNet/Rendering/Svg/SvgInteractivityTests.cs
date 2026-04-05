// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

public class SvgInteractivityTests
{
    [Fact]
    public void EnableZoomPan_InjectsScriptElement()
    {
        var figure = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<script", svg);
    }

    [Fact]
    public void DisabledZoomPan_NoScriptElement()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.DoesNotContain("<script", svg);
    }

    [Fact]
    public void ScriptContains_ViewBoxManipulation()
    {
        var figure = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("viewBox", svg);
        Assert.Contains("setAttribute", svg);
    }

    [Fact]
    public void ScriptContains_WheelEventHandler()
    {
        var figure = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("wheel", svg);
    }
}
