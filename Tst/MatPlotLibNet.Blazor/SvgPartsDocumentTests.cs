// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>The client keeps the last whole document of a chart and puts every part the server sends in place of the part of
/// the same name, so a component is still handed a whole SVG while the wire carries only what changed.</summary>
public sealed class SvgPartsDocumentTests
{
    private const string N = "\n";

    private static string Part(string name, string body) => $"<g data-mpl-part=\"{name}\">{N}{body}{N}</g>{N}";

    private static string Document(string root, string background, string axes0, string axes1, string overlay, string tail) =>
        root + N + "<title id=\"chart-title\">t</title>" + N + Part("background", background) + Part("axes-0", axes0)
        + Part("axes-1", axes1) + Part("overlay", overlay) + tail + "</svg>" + N;

    private const string Root = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\" data-xmin=\"0\">";
    private const string Script = "<script type=\"text/ecmascript\"><![CDATA[ (function(){})(); ]]></script>";

    [Fact]
    public void AnAxesPart_IsPutInPlaceOfThePartOfItsName()
    {
        var document = new ChartSubscriptionClient.SvgPartsDocument(Document(Root, "<rect/>", "<g><g>first</g></g>", "<g>second</g>", "<text>o</text>", Script + N));

        Assert.True(document.Apply(new ChartSubscriptionClient.SvgPartsMessage(null, null, null, [new ChartSubscriptionClient.SvgAxesPartMessage(1, Part("axes-1", "<g>changed</g>"))])));

        Assert.Equal(Document(Root, "<rect/>", "<g><g>first</g></g>", "<g>changed</g>", "<text>o</text>", Script + N), document.Svg);
    }

    [Fact]
    public void TheRootTagTheBackgroundAndTheOverlay_AreReplacedToo()
    {
        var document = new ChartSubscriptionClient.SvgPartsDocument(Document(Root, "<rect/>", "a", "b", "<text>o</text>", ""));
        string newRoot = Root.Replace("data-xmin=\"0\"", "data-xmin=\"2\"") + N;

        Assert.True(document.Apply(new ChartSubscriptionClient.SvgPartsMessage(newRoot, Part("background", "<rect x=\"1\"/>"), Part("overlay", "<text>p</text>"), [])));

        Assert.Equal(Document(Root.Replace("data-xmin=\"0\"", "data-xmin=\"2\""), "<rect x=\"1\"/>", "a", "b", "<text>p</text>", ""), document.Svg);
    }

    [Fact]
    public void AnOverlayFollowedByScripts_EndsWhereTheFirstScriptBegins()
    {
        var document = new ChartSubscriptionClient.SvgPartsDocument(Document(Root, "r", "a", "b", "<text>o</text>", Script + N + Script + N));

        Assert.True(document.Apply(new ChartSubscriptionClient.SvgPartsMessage(null, null, Part("overlay", "<text>new</text>"), null)));

        Assert.Equal(Document(Root, "r", "a", "b", "<text>new</text>", Script + N + Script + N), document.Svg);
    }

    [Fact]
    public void APartTheDocumentDoesNotHave_IsRefused_AndTheDocumentStaysAsItWas()
    {
        string before = Document(Root, "r", "a", "b", "o", "");
        var document = new ChartSubscriptionClient.SvgPartsDocument(before);

        Assert.False(document.Apply(new ChartSubscriptionClient.SvgPartsMessage(null, null, null, [new ChartSubscriptionClient.SvgAxesPartMessage(5, Part("axes-5", "x"))])));
        Assert.False(document.Apply(new ChartSubscriptionClient.SvgPartsMessage(null, null, null, [new ChartSubscriptionClient.SvgAxesPartMessage(0, "not a part")])));

        Assert.Equal(before, document.Svg);
    }

    [Fact]
    public void ThePlainDocument_IsTheWrappedOneWithoutItsPartGroups()
    {
        var document = new ChartSubscriptionClient.SvgPartsDocument(Document(Root, "<rect/>", "<g><g>a</g></g>", "<g>b</g>", "<text>o</text>", Script + N));

        Assert.Equal(Root + N + "<title id=\"chart-title\">t</title>" + N + "<rect/>" + N + "<g><g>a</g></g>" + N + "<g>b</g>" + N
            + "<text>o</text>" + N + Script + N + "</svg>" + N, document.Plain);
    }

    [Fact]
    public void ThePlainDocument_TakesTheLineBreaksTheServerWrote_WhicheverTheyAre()
    {
        string unix = "<svg>\n<g data-mpl-part=\"background\">\n<rect/>\n</g>\n<g data-mpl-part=\"overlay\">\n<text/>\n</g>\n</svg>\n";

        Assert.Equal("<svg>\n<rect/>\n<text/>\n</svg>\n", new ChartSubscriptionClient.SvgPartsDocument(unix).Plain);
    }

    [Fact]
    public void ADocumentWithoutParts_IsNotOneThatCanBePatched()
    {
        Assert.False(ChartSubscriptionClient.SvgPartsDocument.IsWrapped("<svg><rect/></svg>"));
        Assert.True(ChartSubscriptionClient.SvgPartsDocument.IsWrapped(Document(Root, "r", "a", "b", "o", "")));
    }
}
