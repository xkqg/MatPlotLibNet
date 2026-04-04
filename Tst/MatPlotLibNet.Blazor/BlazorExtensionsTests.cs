// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;

namespace MatPlotLibNet.Blazor.Tests;

public class BlazorExtensionsTests
{
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

    [Fact]
    public void ToMarkupString_EmptyFigure_ReturnsValidSvg()
    {
        var figure = Plt.Create().Build();

        MarkupString markup = figure.ToMarkupString();

        Assert.StartsWith("<svg", markup.Value.TrimStart());
        Assert.Contains("</svg>", markup.Value);
    }

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
