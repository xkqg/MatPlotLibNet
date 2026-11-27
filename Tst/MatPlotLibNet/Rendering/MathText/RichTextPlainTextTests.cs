// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The plain text of a rich text is every span's text one after the other: what a surface that cannot set a span draws.</summary>
public sealed class RichTextPlainTextTests
{
    [Fact]
    public void TheSpansTexts_AreJoinedInOrder()
    {
        var rich = new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7), new TextSpan(" rate")]);

        Assert.Equal("x2 rate", rich.PlainText);
    }

    [Fact]
    public void ARichTextOfOneSpan_IsThatSpansText()
    {
        var text = string.Concat("rate", " of change");

        Assert.Same(text, new RichText([new TextSpan(text)]).PlainText);
    }

    [Fact]
    public void ARichTextWithNoSpan_IsEmpty()
    {
        Assert.Equal(string.Empty, MathTextParser.Parse(string.Empty).PlainText);
    }

    [Theory]
    [InlineData("price")]
    [InlineData("$\\alpha$ rate")]
    [InlineData("a $x_1^2$ b")]
    public void AParsedText_ReadsAsTheTextOfItsSpans(string text)
    {
        var rich = MathTextParser.Parse(text);

        Assert.Equal(string.Concat(rich.Spans.Select(span => span.Text)), rich.PlainText);
    }
}
