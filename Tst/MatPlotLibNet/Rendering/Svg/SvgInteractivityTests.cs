// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG interactivity (zoom/pan) behavior.</summary>
public class SvgInteractivityTests
{
    /// <summary>Verifies that enabling zoom/pan injects a script element into the SVG output.</summary>
    [Fact]
    public void EnableZoomPan_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that without zoom/pan enabled, no script element is present in the SVG output.</summary>
    [Fact]
    public void DisabledZoomPan_NoScriptElement()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that the injected script contains viewBox manipulation via setAttribute.</summary>
    [Fact]
    public void ScriptContains_ViewBoxManipulation()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("viewBox", svg);
        Assert.Contains("setAttribute", svg);
    }

    /// <summary>Verifies that the injected script includes a wheel event handler for zoom.</summary>
    [Fact]
    public void ScriptContains_WheelEventHandler()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("wheel", svg);
    }
}
