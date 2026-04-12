// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies CSS4 named color constants and <see cref="Color.FromName"/> lookup.</summary>
public class Css4ColorTests
{
    [Fact]
    public void FromName_AliceBlue_ReturnsCorrectColor()
    {
        var color = Color.FromName("aliceblue");
        Assert.Equal(Color.FromHex("#F0F8FF"), color);
    }

    [Fact]
    public void FromName_CaseInsensitive()
    {
        Assert.Equal(Color.FromName("aliceblue"), Color.FromName("AliceBlue"));
        Assert.Equal(Color.FromName("aliceblue"), Color.FromName("ALICEBLUE"));
    }

    [Fact]
    public void FromName_UnknownName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Color.FromName("notacolor"));
    }

    [Fact]
    public void TryFromName_KnownName_ReturnsTrueAndColor()
    {
        bool result = Color.TryFromName("tomato", out Color color);
        Assert.True(result);
        Assert.Equal(Color.FromHex("#FF6347"), color);
    }

    [Fact]
    public void TryFromName_UnknownName_ReturnsFalse()
    {
        bool result = Color.TryFromName("notacolor", out _);
        Assert.False(result);
    }

    [Fact]
    public void Css4Colors_Has148Colors()
    {
        Assert.Equal(148, Css4Colors.All.Count);
    }

    [Theory]
    [InlineData("red",          "#FF0000")]
    [InlineData("green",        "#008000")]
    [InlineData("blue",         "#0000FF")]
    [InlineData("white",        "#FFFFFF")]
    [InlineData("black",        "#000000")]
    [InlineData("cornflowerblue","#6495ED")]
    [InlineData("rebeccapurple","#663399")]
    [InlineData("deeppink",     "#FF1493")]
    [InlineData("gold",         "#FFD700")]
    [InlineData("teal",         "#008080")]
    public void FromName_MatchesCss4Spec(string name, string expectedHex)
    {
        Assert.Equal(Color.FromHex(expectedHex), Color.FromName(name));
    }

    [Fact]
    public void FromName_GreyAlias_SameAsGrayAlias()
    {
        // CSS4 defines grey and gray as identical
        Assert.Equal(Color.FromName("gray"),          Color.FromName("grey"));
        Assert.Equal(Color.FromName("darkgray"),      Color.FromName("darkgrey"));
        Assert.Equal(Color.FromName("lightgray"),     Color.FromName("lightgrey"));
        Assert.Equal(Color.FromName("dimgray"),       Color.FromName("dimgrey"));
        Assert.Equal(Color.FromName("slategray"),     Color.FromName("slategrey"));
        Assert.Equal(Color.FromName("lightslategray"),Color.FromName("lightslategrey"));
        Assert.Equal(Color.FromName("darkslategray"), Color.FromName("darkslategrey"));
    }

    [Fact]
    public void Css4Colors_StaticProperties_MatchFromName()
    {
        Assert.Equal(Css4Colors.AliceBlue,       Color.FromName("aliceblue"));
        Assert.Equal(Css4Colors.CornflowerBlue,  Color.FromName("cornflowerblue"));
        Assert.Equal(Css4Colors.RebeccaPurple,   Color.FromName("rebeccapurple"));
        Assert.Equal(Css4Colors.LawnGreen,       Color.FromName("lawngreen"));
        Assert.Equal(Css4Colors.MidnightBlue,    Color.FromName("midnightblue"));
    }

    [Fact]
    public void Colors_Aliases_MatchCss4()
    {
        // Colors class exposes top aliases for discoverability
        Assert.Equal(Css4Colors.CornflowerBlue, Colors.CornflowerBlue);
        Assert.Equal(Css4Colors.SteelBlue,      Colors.SteelBlue);
        Assert.Equal(Css4Colors.Crimson,        Colors.Crimson);
        Assert.Equal(Css4Colors.ForestGreen,    Colors.ForestGreen);
        Assert.Equal(Css4Colors.Navy,           Colors.Navy);
    }
}
