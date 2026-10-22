// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The SVG settings of <see cref="SaveOptions"/>, applied to a document text: what gets rounded and how, what
/// is left alone, where the metadata goes and what an indented document looks like.</summary>
public class SvgDocumentExtensionsTests
{
    private const string Ns = "xmlns=\"http://www.w3.org/2000/svg\"";

    private static string Doc(string body) => $"<svg {Ns} viewBox=\"0 0 100 50\"><title id=\"chart-title\">T</title>{body}</svg>";

    private static string RoundedAttribute(string attribute, string value, int decimals)
    {
        string svg = Doc($"<rect {attribute}=\"{value}\"/>").WithSaveOptions(new SaveOptions { SvgDecimalPrecision = decimals });
        return XDocument.Parse(svg).Descendants().First(e => e.Name.LocalName == "rect").Attribute(attribute)!.Value;
    }

    // ── nothing asked ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OptionsThatAskForNothing_ReturnTheDocumentItself()
    {
        string svg = Doc("<rect x=\"1.23456\"/>");

        Assert.Same(svg, svg.WithSaveOptions(new SaveOptions()));
        Assert.Same(svg, svg.WithSaveOptions(new SaveOptions { Dpi = 300, Title = "", Author = "" }));
    }

    [Fact]
    public void NullOptions_AreRefusedByName()
    {
        var refusal = Assert.Throws<ArgumentNullException>(() => Doc("").WithSaveOptions(null!));

        Assert.Equal("options", refusal.ParamName);
    }

    [Fact]
    public void ANumberOfDecimalsOutsideTheRange_IsRefusedEvenWhenNothingElseIsAsked()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Doc("").WithSaveOptions(new SaveOptions { SvgDecimalPrecision = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Doc("").WithSaveOptions(new SaveOptions { SvgDecimalPrecision = 16 }));
    }

    // ── rounding ─────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1.25", 1, "1.3")]
    [InlineData("0.15", 1, "0.2")]
    [InlineData("-1.25", 1, "-1.3")]
    [InlineData("2.5", 0, "3")]
    [InlineData("-2.5", 0, "-3")]
    [InlineData("3.14159", 2, "3.14")]
    [InlineData("3.14159", 4, "3.1416")]
    [InlineData("99.99", 1, "100")]
    [InlineData("0.04", 1, "0")]
    [InlineData("-0.04", 1, "0")]
    [InlineData("0.5", 0, "1")]
    [InlineData(".75", 1, "0.8")]
    public void ADecimalNumber_IsRoundedHalfAwayFromZero(string written, int decimals, string expected)
    {
        Assert.Equal(expected, RoundedAttribute("x", written, decimals));
    }

    [Theory]
    [InlineData("7", 0)]
    [InlineData("7", 2)]
    [InlineData("1.5", 1)]
    [InlineData("1.5", 3)]
    [InlineData("0.125", 3)]
    [InlineData("-12.25", 2)]
    public void ANumberWithNoMoreDecimalsThanAsked_IsLeftExactlyAsWritten(string written, int decimals)
    {
        Assert.Equal(written, RoundedAttribute("x", written, decimals));
    }

    [Theory]
    [InlineData("1E-05", 3, "0")]
    [InlineData("1.234E-05", 5, "1E-05")]
    [InlineData("1.5E+20", 2, "1.5E+20")]
    [InlineData("-1E-05", 2, "0")]
    public void ANumberInExponentForm_IsRoundedAsADouble(string written, int decimals, string expected)
    {
        Assert.Equal(expected, RoundedAttribute("x", written, decimals));
    }

    [Fact]
    public void ANumberBeyondTheRangeOfADouble_IsLeftAsWritten()
    {
        Assert.Equal("1E999", RoundedAttribute("x", "1E999", 2));
    }

    [Fact]
    public void ANumberTooLongForADecimal_IsLeftAsWritten()
    {
        // 31 digits: more than a decimal holds, and the SVG writer never emits so many.
        const string written = "1234567890123456789012345678901.5";

        Assert.Equal(written, RoundedAttribute("x", written, 0));
    }

    [Fact]
    public void ThePathData_KeepsItsCommands_AndRoundsEveryNumber()
    {
        string rounded = RoundedAttribute("d", "M1.25,2.75L-0.05,3.00C0,0.35 1,1 10,20Z", 1);

        Assert.Equal("M1.3,2.8L-0.1,3C0,0.4 1,1 10,20Z", rounded);
    }

    [Fact]
    public void ThePoints_AreRoundedPairByPair()
    {
        Assert.Equal("1.2,3.4 5.6,7.9", RoundedAttribute("points", "1.234,3.4 5.55,7.891", 1));
    }

    [Fact]
    public void ATransform_IsRoundedInsideItsFunctions()
    {
        Assert.Equal("rotate(-45,100.5,200.3)", RoundedAttribute("transform", "rotate(-45,100.456,200.25)", 1));
        Assert.Equal("translate(10.1,20)", RoundedAttribute("transform", "translate(10.12,20.00)", 1));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("cx")]
    [InlineData("cy")]
    [InlineData("r")]
    [InlineData("rx")]
    [InlineData("ry")]
    [InlineData("x1")]
    [InlineData("y1")]
    [InlineData("x2")]
    [InlineData("y2")]
    [InlineData("dx")]
    [InlineData("dy")]
    [InlineData("width")]
    [InlineData("height")]
    public void ThePositionsAndSizes_AreRounded(string attribute)
    {
        Assert.Equal("1.2", RoundedAttribute(attribute, "1.2345", 1));
    }

    [Theory]
    [InlineData("stroke-width")]
    [InlineData("stroke-dasharray")]
    [InlineData("opacity")]
    [InlineData("fill-opacity")]
    [InlineData("stroke-opacity")]
    [InlineData("stop-opacity")]
    [InlineData("offset")]
    [InlineData("font-size")]
    [InlineData("data-xmin")]
    [InlineData("data-plot-x")]
    [InlineData("aria-label")]
    [InlineData("id")]
    [InlineData("fill")]
    public void StylingTextAndDataAttributes_AreLeftAlone(string attribute)
    {
        Assert.Equal("1.2345", RoundedAttribute(attribute, "1.2345", 1));
    }

    [Fact]
    public void TheViewBox_IsRounded()
    {
        string svg = $"<svg {Ns} viewBox=\"0 0 100.55 50.44\"/>".WithSaveOptions(new SaveOptions { SvgDecimalPrecision = 1 });

        Assert.Equal("0 0 100.6 50.4", XDocument.Parse(svg).Root!.Attribute("viewBox")!.Value);
    }

    [Fact]
    public void TheTextOfAnElement_IsNeverRounded()
    {
        string svg = Doc("<text x=\"1.26\">tick 0.12345</text>").WithSaveOptions(new SaveOptions { SvgDecimalPrecision = 1 });

        var text = XDocument.Parse(svg).Descendants().First(e => e.Name.LocalName == "text");
        Assert.Equal("tick 0.12345", text.Value);
        Assert.Equal("1.3", text.Attribute("x")!.Value);
    }

    [Fact]
    public void AnInlineScript_IsNeverRounded()
    {
        const string script = "var a = 0.123456; if (a < 1 && a > 0) {}";
        string svg = Doc($"<script><![CDATA[{script}]]></script>").WithSaveOptions(new SaveOptions { SvgDecimalPrecision = 0 });

        Assert.Equal(script, XDocument.Parse(svg).Descendants().First(e => e.Name.LocalName == "script").Value);
    }

    // ── metadata ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Metadata_GoesAfterTheLastOfTheTitleAndTheDescription()
    {
        string svg = $"<svg {Ns}><title>T</title><desc>D</desc><g/></svg>".WithSaveOptions(new SaveOptions { Title = "X" });

        Assert.Equal(new[] { "title", "desc", "metadata", "g" }, XDocument.Parse(svg).Root!.Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public void Metadata_GoesFirst_WhenTheDocumentNamesNothing()
    {
        string svg = $"<svg {Ns}><g/></svg>".WithSaveOptions(new SaveOptions { Author = "A" });

        Assert.Equal(new[] { "metadata", "g" }, XDocument.Parse(svg).Root!.Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public void Metadata_TakesItsOwnLine_WhenTheDocumentNamesNothingAndKeepsItsLines()
    {
        string svg = $"<svg {Ns}>\n<g/>\n</svg>".WithSaveOptions(new SaveOptions { Author = "A" }).Replace("\r", "");

        Assert.StartsWith($"<svg {Ns}>\n<metadata", svg);
        Assert.Contains("</metadata>\n<g />", svg);
    }

    [Fact]
    public void TheMetadataElement_IsInTheSvgNamespace()
    {
        string svg = Doc("").WithSaveOptions(new SaveOptions { Title = "X" });

        var metadata = XDocument.Parse(svg).Root!.Elements().First(e => e.Name.LocalName == "metadata");
        Assert.Equal("http://www.w3.org/2000/svg", metadata.Name.NamespaceName);
    }

    // ── layout ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Indenting_PutsEachNestedElementOnItsOwnLine_TwoSpacesIn()
    {
        string svg = Doc("<g><rect x=\"1\"/></g>").WithSaveOptions(new SaveOptions { PrettifySvg = true });

        string[] lines = svg.Replace("\r", "").Split('\n');
        Assert.Equal("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 50\">", lines[0]);
        Assert.Equal("  <title id=\"chart-title\">T</title>", lines[1]);
        Assert.Equal("  <g>", lines[2]);
        Assert.Equal("    <rect x=\"1\" />", lines[3]);
        Assert.Equal("  </g>", lines[4]);
        Assert.Equal("</svg>", lines[5]);
    }

    [Fact]
    public void Indenting_DropsTheWhitespaceBetweenElements_ButKeepsTheTextInside()
    {
        string svg = $"<svg {Ns}>\r\n<g>\r\n  <text>a  b</text>\r\n</g>\r\n</svg>".WithSaveOptions(new SaveOptions { PrettifySvg = true });

        Assert.Contains("<text>a  b</text>", svg);
    }

    [Fact]
    public void WithoutIndenting_TheLinesOfTheDocumentStay()
    {
        string source = $"<svg {Ns}>\n<title>T</title>\n<g/>\n</svg>";

        string svg = source.WithSaveOptions(new SaveOptions { Title = "X" }).Replace("\r", "");

        Assert.Contains("<title>T</title>\n<metadata", svg);
        Assert.Contains("</metadata>\n<g />", svg);
    }

    [Fact]
    public void ADocumentThatIsNotXml_IsRefused()
    {
        Assert.ThrowsAny<System.Xml.XmlException>(() => "<svg><g></svg>".WithSaveOptions(new SaveOptions { PrettifySvg = true }));
    }
}
