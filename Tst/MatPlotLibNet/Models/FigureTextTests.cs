// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A text placed on the figure itself (matplotlib's <c>figtext</c>): its defaults, the rule that reads its position
/// as fractions of the figure counted from the bottom-left corner, the texts that have no place, and the builder verb.</summary>
public class FigureTextTests
{
    [Fact]
    public void ANewText_StartsAtItsPoint_OnItsBaseline_Unturned_InTheThemesFont()
    {
        var text = new FigureText("Note", 0.25, 0.75);

        Assert.Equal("Note", text.Text);
        Assert.Equal(0.25, text.X);
        Assert.Equal(0.75, text.Y);
        Assert.Null(text.Font);
        Assert.Null(text.Color);
        Assert.Equal(TextAlignment.Left, text.Alignment);
        Assert.Equal(TextVerticalAlignment.Baseline, text.VerticalAlignment);
        Assert.Equal(0, text.Rotation);
    }

    [Fact]
    public void ATextWithNoWords_IsRefused() =>
        Assert.Throws<ArgumentNullException>("text", () => new FigureText(null!, 0.5, 0.5));

    /// <summary>The fractions count up from the bottom of the figure, as matplotlib's figure fraction does: 0.97 is near the
    /// top (18 px down a 600 px figure), not near the bottom.</summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 600.0)]
    [InlineData(1.0, 1.0, 800.0, 0.0)]
    [InlineData(0.25, 0.75, 200.0, 150.0)]
    [InlineData(0.5, 0.97, 400.0, 18.0)]
    public void TheFractions_CountFromTheBottomLeftCorner(double x, double y, double pixelX, double pixelY)
    {
        var anchor = new FigureText("A", x, y).AnchorOn(800, 600);

        Assert.NotNull(anchor);
        Assert.Equal(pixelX, anchor.Value.X, 9);
        Assert.Equal(pixelY, anchor.Value.Y, 9);
    }

    [Theory]
    [InlineData(-0.01, 0.5)]
    [InlineData(1.01, 0.5)]
    [InlineData(0.5, -0.01)]
    [InlineData(0.5, 1.01)]
    [InlineData(double.NaN, 0.5)]
    [InlineData(0.5, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.5)]
    [InlineData(0.5, double.NegativeInfinity)]
    public void ATextOutsideTheFigure_OrAtAMissingPosition_HasNoPlace(double x, double y) =>
        Assert.Null(new FigureText("A", x, y).AnchorOn(800, 600));

    /// <summary>A text shows when it has a place on the figure and something in it that is not a blank. The data table and the MCP
    /// reader both ask this one rule what the picture carries of its own.</summary>
    [Theory]
    [InlineData("A", 0.5, 0.5, true)]
    [InlineData("A", 0.0, 1.0, true)]
    [InlineData("", 0.5, 0.5, false)]
    [InlineData("   ", 0.5, 0.5, false)]
    [InlineData("A", 1.01, 0.5, false)]
    [InlineData("A", 0.5, -0.01, false)]
    [InlineData("A", double.NaN, 0.5, false)]
    [InlineData("A", 0.5, double.PositiveInfinity, false)]
    public void ATextShows_WhenItHasAPlaceAndSomethingToRead(string words, double x, double y, bool shows) =>
        Assert.Equal(shows, new FigureText(words, x, y).IsShownOn(800, 600));

    [Fact]
    public void AFigure_HasNoTexts_UntilOneIsAdded() => Assert.Empty(new Figure().Texts);

    [Fact]
    public void AddText_AddsTheTextsInOrder_AndReturnsEach()
    {
        var figure = new Figure();

        var first = figure.AddText("first", 0.1, 0.2);
        var second = figure.AddText("second", 0.3, 0.4);

        Assert.Equal(new[] { first, second }, figure.Texts);
        Assert.Equal("second", second.Text);
        Assert.Equal(0.3, second.X);
        Assert.Equal(0.4, second.Y);
    }

    [Fact]
    public void FigText_AddsAConfiguredText_AndReturnsTheSameBuilder()
    {
        var builder = Plt.Create();

        var returned = builder.FigText("Note", 0.1, 0.2, t =>
        {
            t.Color = Colors.Red;
            t.Alignment = TextAlignment.Center;
            t.VerticalAlignment = TextVerticalAlignment.Top;
            t.Rotation = 90;
        });

        Assert.Same(builder, returned);
        var text = Assert.Single(builder.Build().Texts);
        Assert.Equal("Note", text.Text);
        Assert.Equal(0.1, text.X);
        Assert.Equal(0.2, text.Y);
        Assert.Equal(Colors.Red, text.Color);
        Assert.Equal(TextAlignment.Center, text.Alignment);
        Assert.Equal(TextVerticalAlignment.Top, text.VerticalAlignment);
        Assert.Equal(90, text.Rotation);
    }

    [Fact]
    public void FigText_WithoutConfigure_KeepsTheDefaults_AndAddsNoAxes()
    {
        var figure = Plt.Create().FigText("Note", 0.5, 0.5).Build();

        var text = Assert.Single(figure.Texts);
        Assert.Null(text.Color);
        Assert.Equal(TextAlignment.Left, text.Alignment);
        Assert.Empty(figure.SubPlots);
    }

    [Fact]
    public void FigText_WithNoWords_IsRefusedAtTheCall() =>
        Assert.Throws<ArgumentNullException>("text", () => Plt.Create().FigText(null!, 0.5, 0.5));

    [Fact]
    public void EachBuild_HasATextOfItsOwn()
    {
        var builder = Plt.Create().FigText("Note", 0.5, 0.5);

        Assert.NotSame(builder.Build().Texts[0], builder.Build().Texts[0]);
    }
}
