// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Verifies <see cref="BlazorExtensions"/> behavior.</summary>
public class BlazorExtensionsTests
{
    /// <summary>Verifies that ToMarkupString returns a MarkupString containing valid SVG with the title.</summary>
    [Fact]
    public void ToMarkupString_ReturnsMarkupString()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        MarkupString markup = figure.ToMarkupString();

        Assert.Contains("<svg", markup.Value);
        Assert.Contains("</svg>", markup.Value);
        Assert.Contains("Test", markup.Value);
    }

    /// <summary>Verifies that ToMarkupString produces valid SVG even for an empty figure.</summary>
    [Fact]
    public void ToMarkupString_EmptyFigure_ReturnsValidSvg()
    {
        var figure = Plt.Create().Build();

        MarkupString markup = figure.ToMarkupString();

        Assert.StartsWith("<svg", markup.Value.TrimStart());
        Assert.Contains("</svg>", markup.Value);
    }

    /// <summary>Verifies that ToMarkupString preserves the configured width and height in the SVG output.</summary>
    [Fact]
    public void ToMarkupString_PreservesDimensions()
    {
        var figure = Plt.Create()
            .WithSize(1200, 900)
            .Build();

        MarkupString markup = figure.ToMarkupString();

        Assert.Contains("1200", markup.Value);
        Assert.Contains("900", markup.Value);
    }
}
