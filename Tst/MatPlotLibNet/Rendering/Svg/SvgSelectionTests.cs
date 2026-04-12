// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG Shift+drag rectangular selection script injection and behavior.</summary>
public class SvgSelectionTests
{
    /// <summary>Verifies that enabling selection injects a script element.</summary>
    [Fact]
    public void WithSelection_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that the selection script requires the Shift key to start a selection.</summary>
    [Fact]
    public void SelectionScript_RequiresShiftKey()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("shiftKey", svg);
    }

    /// <summary>Verifies that the selection script dispatches an mpl:selection custom event.</summary>
    [Fact]
    public void SelectionScript_DispatchesMplSelectionEvent()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("mpl:selection", svg);
    }

    /// <summary>Verifies that the selection script includes x1, y1, x2, y2 in the event detail.</summary>
    [Fact]
    public void SelectionScript_IncludesCoordinatesInEventDetail()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("x1", svg);
        Assert.Contains("y1", svg);
        Assert.Contains("x2", svg);
        Assert.Contains("y2", svg);
    }

    /// <summary>Verifies that the selection script draws a visible selection rectangle.</summary>
    [Fact]
    public void SelectionScript_DrawsSelectionRectangle()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("createElementNS", svg);
        Assert.Contains("'rect'", svg);
    }

    /// <summary>Verifies that without WithSelection(), no selection script is present.</summary>
    [Fact]
    public void WithoutSelection_NoScript()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that WithSelection(false) disables the feature.</summary>
    [Fact]
    public void WithSelectionFalse_NoScript()
    {
        string svg = Plt.Create()
            .WithSelection(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }
}
