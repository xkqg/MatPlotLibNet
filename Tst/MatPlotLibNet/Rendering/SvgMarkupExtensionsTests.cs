// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering;

public class SvgMarkupExtensionsTests
{
    private const string Markup =
        """
        <svg width="40" height="30"><defs><clipPath id="clip-0"><rect x="1" y="2" width="3.5" height="4"/></clipPath></defs>
        <g clip-path="url(#clip-0)"><rect x="10" y="20" width="30" height="40" fill="#ff0000"/>
        <ellipse cx="5" cy="6" rx="7" ry="8"></ellipse><gradient id="g1"/><text x="1" y="2">a &gt; b</text></g></svg>
        """;

    [Fact]
    public void TheElementsOfAName_AreItsStartTags_InDocumentOrder()
    {
        var rects = Markup.Elements("rect");

        Assert.Equal(2, rects.Count);
        Assert.Equal("1", rects[0]["x"]);
        Assert.Equal("#ff0000", rects[1]["fill"]);
    }

    [Fact]
    public void ANumber_IsReadWithTheInvariantCulture()
    {
        Assert.Equal(3.5, Markup.Elements("rect")[0].Number("width"));
    }

    [Fact]
    public void AnAttributeTheTagDoesNotCarry_IsNull()
    {
        Assert.Null(Markup.Elements("ellipse")[0]["fill"]);
    }

    [Fact]
    public void ANameIsNotAPrefixOfAnotherName()
    {
        // "g" must not find <gradient ...>, and "text" finds its tag though the content carries an escaped ">".
        Assert.Single(Markup.Elements("g"));
        Assert.Single(Markup.Elements("gradient"));
        Assert.Single(Markup.Elements("text"));
    }

    [Fact]
    public void AClosingTag_IsNotAStartTag()
    {
        Assert.Single(Markup.Elements("ellipse"));
    }

    [Fact]
    public void TheIds_AreEveryIdTheDocumentDefines()
    {
        Assert.Equal(["clip-0", "g1"], Markup.Ids());
    }

    [Fact]
    public void TheUrlReferences_AreEveryIdAReferencePointsAt()
    {
        Assert.Equal(["clip-0"], Markup.UrlReferences());
    }

    [Fact]
    public void ANumberTheTagLacks_IsRefusedByName()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => Markup.Elements("ellipse")[0].Number("fill"));

        Assert.Contains("fill", ex.Message, StringComparison.Ordinal);
    }
}
