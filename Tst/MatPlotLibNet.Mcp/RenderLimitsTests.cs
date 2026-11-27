// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The bounds a model-authored spec must stay inside are applied by the limits themselves: a missing size takes its
/// default, a size or a picture over the ceiling and a text over the length are refused, each by field and path.</summary>
public class RenderLimitsTests
{
    private static readonly RenderLimits Limits = new(MaxWidth: 400, MaxHeight: 300, MaxTextLength: 5, MaxTableRows: 10);

    private static JsonObject Spec(string json) => (JsonObject)JsonNode.Parse(json)!;

    [Fact]
    public void AMissingSize_TakesItsDefault()
    {
        var figure = Spec("{}");

        Limits.EnsureSize(figure, "width", 800, 400);

        Assert.Equal(800, (double?)figure["width"]);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("250.5")]
    [InlineData("400")]
    public void ASizeWithinTheCeiling_IsKept(string written)
    {
        var figure = Spec($$"""{"width":{{written}}}""");

        Limits.EnsureSize(figure, "width", 800, 400);

        Assert.Equal(double.Parse(written, CultureInfo.InvariantCulture), (double?)figure["width"]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.5")]
    [InlineData("401")]
    [InlineData("100000")]
    public void ASizeOutsideOneAndTheCeiling_IsRefusedNamingTheField(string written)
    {
        var figure = Spec($$"""{"height":{{written}}}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => Limits.EnsureSize(figure, "height", 600, 400));

        Assert.Equal($"'height' must be between 1 and 400 (got {written}): the picture goes into a context window.", refusal.Message);
    }

    [Fact]
    public void ASizeThatIsNotANumber_IsLeftForTheStrictPass()
    {
        var figure = Spec("""{"width":"wide","height":7}""");

        Limits.EnsureSize(figure, "width", 800, 400);

        Assert.Equal("wide", (string?)figure["width"]);
    }

    [Fact]
    public void ADpiIsOnlyBoundedByTheCeilingItsCallerGives()
    {
        var figure = Spec("""{"dpi":300000}""");

        Limits.EnsureSize(figure, "dpi", 96, int.MaxValue);

        Assert.Equal(300000, (double?)figure["dpi"]);
    }

    [Fact]
    public void APictureWithinTheCeilingInPixels_Fits()
    {
        var figure = Spec("""{"width":400,"height":300,"dpi":96}""");

        Limits.EnsurePictureFits(figure);

        Assert.Equal(96, (double?)figure["dpi"]);
    }

    [Theory]
    [InlineData("""{"height":300,"dpi":96}""")]
    [InlineData("""{"width":400,"dpi":96}""")]
    [InlineData("""{"width":400,"height":300}""")]
    [InlineData("""{"width":"wide","height":300,"dpi":900}""")]
    [InlineData("""{"width":400,"height":300,"dpi":"high"}""")]
    public void APictureWhoseSizeIsNotKnown_IsLeftForTheStrictPass(string json) =>
        Limits.EnsurePictureFits(Spec(json));

    [Fact]
    public void ADpiThatDrawsTheWidthPastTheCeiling_IsRefusedWithThePictureItDraws()
    {
        var figure = Spec("""{"width":400,"height":100,"dpi":192}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => Limits.EnsurePictureFits(figure));

        Assert.Equal(
            "'dpi' 192 draws the picture 800 by 200 pixels (width and height times dpi / 96); the ceiling is 400 by 300: "
            + "the picture goes into a context window. Lower the dpi or the size.",
            refusal.Message);
    }

    [Fact]
    public void ADpiThatDrawsOnlyTheHeightPastTheCeiling_IsRefusedToo()
    {
        var figure = Spec("""{"width":100,"height":300,"dpi":144.5}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => Limits.EnsurePictureFits(figure));

        Assert.StartsWith("'dpi' 144.5 draws the picture 151 by 452 pixels", refusal.Message);
    }

    [Fact]
    public void ATextOverTheLength_IsRefusedNamingFieldAndPath()
    {
        var node = Spec("""{"title":"abcdef"}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => Limits.CheckText(node, "$.subPlots[0]", "title"));

        Assert.Equal("'title' at $.subPlots[0] is 6 characters long; the limit is 5.", refusal.Message);
    }

    [Fact]
    public void AnythingThatIsNotATooLongText_Passes()
    {
        var node = Spec("""{"exact":"abcde","empty":"","number":123456789,"nothing":null}""");

        Limits.CheckText(node, "$", "exact");
        Limits.CheckText(node, "$", "empty");
        Limits.CheckText(node, "$", "number");
        Limits.CheckText(node, "$", "nothing");
        Limits.CheckText(node, "$", "absent");

        Assert.Equal("abcde", (string?)node["exact"]);
    }
}
