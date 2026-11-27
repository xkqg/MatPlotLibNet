// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>A document's parts written once into the finished string: the result is exactly what appending every part to
/// one builder in the same order would have made, whatever the parts are and however many pieces a builder holds.</summary>
public sealed class SvgDocumentPartsTests
{
    [Fact]
    public void NoParts_MakeTheEmptyString()
    {
        Assert.Equal("", new SvgFigureDocument.SvgDocumentParts().ToString());
    }

    [Fact]
    public void Builders_Texts_AndLines_AreJoinedInTheOrderTheyWereGiven()
    {
        var first = new StringBuilder("<svg>");
        var parts = new SvgFigureDocument.SvgDocumentParts();
        parts.Add(first);
        parts.AppendLineLf("<g/>");
        parts.Add(new StringBuilder());
        parts.AppendLineLf("</svg>");

        Assert.Equal("<svg><g/>\n</svg>\n", parts.ToString());
    }

    [Fact]
    public void ABuilderOfManyChunks_IsCopiedWhole()
    {
        // A builder grown one append at a time holds its text in many chunks; every one of them must arrive, in order.
        var large = new StringBuilder();
        for (int i = 0; i < 20_000; i++)
        {
            large.Append(i).Append(',');
        }
        var expected = new StringBuilder().Append("a").Append(large).Append("b").Append('\n').ToString();

        var parts = new SvgFigureDocument.SvgDocumentParts();
        parts.Add(new StringBuilder("a"));
        parts.Add(large);
        parts.AppendLineLf("b");

        Assert.Equal(expected, parts.ToString());
    }

    [Fact]
    public void AContext_AddsTheMarkupItWrote()
    {
        var context = new SvgRenderContext();
        context.DrawText("label", new MatPlotLibNet.Rendering.Point(1, 2), new MatPlotLibNet.Styling.Font(), MatPlotLibNet.Rendering.TextAlignment.Left);
        var written = new StringBuilder();
        context.WriteTo(written);

        var parts = new SvgFigureDocument.SvgDocumentParts();
        parts.Add(context);

        Assert.Equal(written.ToString(), parts.ToString());
    }
}
