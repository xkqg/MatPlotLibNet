// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;

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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("WithAltText_AppearsInSvg", () => Plt.Create()
            .WithAltText("Revenue chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "Revenue chart"),
        new DrawnToken("WithDescription_AppearsInSvg", () => Plt.Create()
            .WithDescription("Quarterly data 2025")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "Quarterly data 2025"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithAltTextAndWithDescription_Chainable(bool altText)
    {
        var builder = altText ? Plt.Create().WithAltText("Test") : Plt.Create().WithDescription("Test");
        Assert.IsType<FigureBuilder>(builder);
    }

    public static TheoryData<Func<FigureBuilder, FigureBuilder>, Func<Figure, string?>, string> TheTextsOfARoundTrip => new()
    {
        { builder => builder.WithAltText("Preserved alt text"), figure => figure.AltText, "Preserved alt text" },
        { builder => builder.WithDescription("Preserved description"), figure => figure.Description, "Preserved description" },
    };

    [Theory]
    [MemberData(nameof(TheTextsOfARoundTrip))]
    public void RoundTrip_PreservesTheAccessibilityText(Func<FigureBuilder, FigureBuilder> set, Func<Figure, string?> read, string expected)
    {
        var figure = set(Plt.Create())
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        var restored = figure.RoundTripped();
        Assert.Equal(expected, read(restored));
    }

    public static TheoryData<Func<FigureBuilder, FigureBuilder>, string, string> TheAccessibilityTexts => new()
    {
        { builder => builder.WithAltText("chart alt"), "altText", "chart alt" },
        { builder => builder.WithDescription("chart desc"), "description", "chart desc" },
    };

    [Theory]
    [MemberData(nameof(TheAccessibilityTexts))]
    public void ToJson_IncludesTheAccessibilityText(Func<FigureBuilder, FigureBuilder> set, string property, string expected)
    {
        var figure = set(Plt.Create()).Build();
        var json = ChartSerializerDefault.Current.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, doc.RootElement.GetProperty(property).GetString());
    }

    [Theory]
    [InlineData("altText")]
    [InlineData("description")]
    public void ANullAccessibilityText_IsOmittedFromJson(string property)
    {
        var figure = Plt.Create().Build();
        var json = ChartSerializerDefault.Current.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty(property, out _));
    }
}
