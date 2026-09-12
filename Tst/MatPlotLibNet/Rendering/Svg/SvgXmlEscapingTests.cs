// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The one escaper serves text nodes AND double-quoted attributes: a series label with a quote in it
/// used to land unescaped inside <c>aria-label="…"</c> and break the document. The apostrophe stays as it is —
/// the library writes no single-quoted attribute, and escaping it would change every title that has one.</summary>
public class SvgXmlEscapingTests
{
    [Theory]
    [InlineData("a & b", "a &amp; b")]
    [InlineData("<b>", "&lt;b&gt;")]
    [InlineData("say \"hi\"", "say &quot;hi&quot;")]
    [InlineData("it's", "it's")]
    [InlineData("plain", "plain")]
    public void EscapeForXml_EscapesWhatAnAttributeCannotHold(string input, string expected)
    {
        Assert.Equal(expected, input.EscapeForXml());
    }

    [Fact]
    public void ALabelWithAQuote_StillYieldsAWellFormedDocument()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Q1 \"net\"")
            .ToSvg();

        var doc = XDocument.Parse(svg);

        var group = doc.Descendants().First(e => e.Attribute("aria-label") is not null && e.Attribute("class")?.Value == "series");
        Assert.Equal("Q1 \"net\"", group.Attribute("aria-label")!.Value);
    }

    [Fact]
    public void ATitleWithAQuote_IsReadableAsText()
    {
        string svg = Plt.Create().WithTitle("The \"quick\" chart").Plot([1.0], [2.0]).ToSvg();

        var doc = XDocument.Parse(svg);

        Assert.Equal("The \"quick\" chart", doc.Descendants().First(e => e.Name.LocalName == "title").Value);
    }
}
