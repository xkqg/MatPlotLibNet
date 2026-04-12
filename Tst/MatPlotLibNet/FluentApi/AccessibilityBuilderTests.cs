// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FigureBuilder.WithAltText"/> and <see cref="FigureBuilder.WithDescription"/> builder methods and serialization round-trip.</summary>
public class AccessibilityBuilderTests
{
    [Fact]
    public void WithAltText_SetsAltText()
    {
        var figure = Plt.Create().WithAltText("My chart").Build();
        Assert.Equal("My chart", figure.AltText);
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var figure = Plt.Create().WithDescription("Detailed data").Build();
        Assert.Equal("Detailed data", figure.Description);
    }

    [Fact]
    public void WithAltText_AppearsInSvg()
    {
        var svg = Plt.Create()
            .WithAltText("Revenue chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("Revenue chart", svg);
    }

    [Fact]
    public void WithDescription_AppearsInSvg()
    {
        var svg = Plt.Create()
            .WithDescription("Quarterly data 2025")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("Quarterly data 2025", svg);
    }

    [Fact]
    public void WithAltText_Chainable()
    {
        var builder = Plt.Create().WithAltText("Test");
        Assert.IsType<FigureBuilder>(builder);
    }

    [Fact]
    public void WithDescription_Chainable()
    {
        var builder = Plt.Create().WithDescription("Test");
        Assert.IsType<FigureBuilder>(builder);
    }

    [Fact]
    public void RoundTrip_PreservesAltText()
    {
        var figure = Plt.Create()
            .WithAltText("Preserved alt text")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal("Preserved alt text", restored.AltText);
    }

    [Fact]
    public void RoundTrip_PreservesDescription()
    {
        var figure = Plt.Create()
            .WithDescription("Preserved description")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal("Preserved description", restored.Description);
    }

    [Fact]
    public void ToJson_IncludesAltText()
    {
        var figure = Plt.Create().WithAltText("chart alt").Build();
        var json = ChartServices.Serializer.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.Equal("chart alt", doc.RootElement.GetProperty("altText").GetString());
    }

    [Fact]
    public void ToJson_IncludesDescription()
    {
        var figure = Plt.Create().WithDescription("chart desc").Build();
        var json = ChartServices.Serializer.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.Equal("chart desc", doc.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public void NullAltText_OmittedFromJson()
    {
        var figure = Plt.Create().Build();
        var json = ChartServices.Serializer.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("altText", out _));
    }

    [Fact]
    public void NullDescription_OmittedFromJson()
    {
        var figure = Plt.Create().Build();
        var json = ChartServices.Serializer.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("description", out _));
    }
}
