// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG interactivity (zoom/pan) behavior.</summary>
public class SvgInteractivityTests
{
    /// <summary>Verifies that enabling zoom/pan injects a script element into the SVG output.</summary>
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

    /// <summary>Verifies that without zoom/pan enabled, no script element is present in the SVG output.</summary>
    [Fact]
    public void DisabledZoomPan_NoScriptElement()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that the injected script contains viewBox manipulation via setAttribute.</summary>
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

    /// <summary>Verifies that the injected script includes a wheel event handler for zoom.</summary>
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
