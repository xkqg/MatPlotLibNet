// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>J.0.a — TDD tests for the <c>EscapeForXml</c> string extension
/// (refactored from static <c>SvgXmlHelper.EscapeXml</c>).</summary>
public class SvgXmlExtensionsTests
{
    [Fact]
    public void EscapeForXml_NoSpecialChars_ReturnsSameReference()
    {
        const string input = "hello world";
        Assert.Same(input, input.EscapeForXml());
    }

    [Fact]
    public void EscapeForXml_Ampersand_Escapes()
    {
        Assert.Equal("a&amp;b", "a&b".EscapeForXml());
    }

    [Fact]
    public void EscapeForXml_LessThan_Escapes()
    {
        Assert.Equal("a&lt;b", "a<b".EscapeForXml());
    }

    [Fact]
    public void EscapeForXml_GreaterThan_Escapes()
    {
        Assert.Equal("a&gt;b", "a>b".EscapeForXml());
    }

    [Fact]
    public void EscapeForXml_AllThree_EscapesAll()
    {
        Assert.Equal("&amp;&lt;&gt;", "&<>".EscapeForXml());
    }

    [Fact]
    public void EscapeForXml_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, string.Empty.EscapeForXml());
    }
}
