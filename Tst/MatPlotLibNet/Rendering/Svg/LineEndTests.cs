// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Packaging;
using System.Text;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Every text the library writes ends its lines with a line feed alone, on every operating system, so the same
/// figure writes the same bytes on Windows and on Linux. Before, the SVG and the indented JSON ended their lines with the
/// operating system's line end: CRLF on Windows, with the scripts' own lines in LF inside the same file.</summary>
public class LineEndTests
{
    /// <summary>A figure that reaches every SVG writer: the root tag, the title and the description, two subplots, a legend,
    /// a hatch pattern, a gradient, a hyperlink, a clip, the scripts of the browser interaction.</summary>
    private static Figure EveryWriter() => Plt.Create()
        .WithTitle("Two lines")
        .WithDescription("A figure drawn to reach every SVG writer.")
        .WithBrowserInteraction()
        .WithRichTooltips()
        .AddSubPlot(1, 2, 1, ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "first")
            .Bar(["A", "B"], [3.0, 5.0], s => { s.Hatch = HatchPattern.ForwardDiagonal; s.Label = "bars"; })
            .WithLegend())
        .AddSubPlot(1, 2, 2, ax => ax.HideAllAxes().Sankey(
            [new SankeyNode("A", Color.FromHex("#1F77B4")), new SankeyNode("B", Color.FromHex("#FF7F0E"))],
            [new SankeyLink(0, 1, 10)],
            s => s.LinkColorMode = SankeyLinkColorMode.Gradient))
        .Build();

    private static void AssertLineFeedsOnly(string text)
    {
        Assert.Contains('\n', text);
        Assert.DoesNotContain('\r', text);
    }

    [Fact]
    public void TheSvgOfAFigure_EndsEveryLineWithALineFeed()
    {
        string svg = EveryWriter().ToSvg();

        Assert.Contains("<linearGradient", svg);
        Assert.Contains("<pattern", svg);
        Assert.Contains("<script", svg);
        AssertLineFeedsOnly(svg);
    }

    [Fact]
    public void TheSvgOfAFigureInItsParts_EndsEveryLineWithALineFeed()
    {
        var document = new SvgTransform().RenderDocument(EveryWriter());

        AssertLineFeedsOnly(document.ToWrappedSvg());
        AssertLineFeedsOnly(document.WrappedAxes(1));
    }

    [Fact]
    public void AHyperlinkedTileWithACaption_EndsEveryLineWithALineFeed()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => { s.Caption = "one\ntwo"; s.Url = "https://example.org/"; }))
            .ToSvg();

        Assert.Contains("<a ", svg);
        AssertLineFeedsOnly(svg);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "A title")]
    public void TheSvgSavedWithOptions_EndsEveryLineWithALineFeed(bool prettify, string? title)
    {
        using var stream = new MemoryStream();
        new SvgTransform().Transform(EveryWriter(), stream, new SaveOptions { PrettifySvg = prettify, Title = title });

        AssertLineFeedsOnly(Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>The SVG that steps through an animation's frames is written by an XML writer of its own; its script ends its
    /// lines with a line feed as every other line does.</summary>
    [Fact]
    public void TheSvgThatStepsThroughFrames_EndsEveryLineWithALineFeed()
    {
        using var stream = new MemoryStream();
        new StepThroughSvgTransform().Transform([EveryWriter(), EveryWriter()], TimeSpan.FromMilliseconds(100), true, stream);

        AssertLineFeedsOnly(Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void TheIndentedJsonOfAFigure_EndsEveryLineWithALineFeed() =>
        AssertLineFeedsOnly(EveryWriter().ToJson(indented: true));

    /// <summary>A caption breaks its lines at CRLF and at LF alike, whichever operating system the caption was written on:
    /// the reader's rule does not follow the operating system it runs on either.</summary>
    [Theory]
    [InlineData("threshold 6\r\n2148 msg")]
    [InlineData("threshold 6\n2148 msg")]
    public void ACaptionBrokenWithEitherLineEnd_IsDrawnAsTheSameTwoLines(string caption)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => s.Caption = caption))
            .ToSvg();

        Assert.Contains(">threshold 6</text>", svg);
        Assert.Contains(">2148 msg</text>", svg);
        Assert.DoesNotContain('\r', svg);
    }

    [Fact]
    public void AppendLineLf_EndsTheLineWithALineFeed_AndWithoutTextWritesTheLineFeedAlone() =>
        Assert.Equal("a\n\n", new StringBuilder().AppendLineLf("a").AppendLineLf().ToString());

    [Fact]
    public void TheLineBreaksAReaderSplitsAt_AreCrLfAndLf_InThatOrder() =>
        Assert.Equal(["\r\n", "\n"], LineEndExtensions.LineBreaks);

    /// <summary>What writes the operating system's line end: <c>Environment.NewLine</c>, <c>StringBuilder.AppendLine</c>, and an
    /// XML tree written by its own <c>ToString</c> or by an XML writer whose settings do not name the line end.</summary>
    private static readonly Regex OperatingSystemLineEnd = new(
        @"Environment\.NewLine|\.AppendLine\(|Xml\.Linq\.SaveOptions|SaveOptions\.DisableFormatting", RegexOptions.CultureInvariant);

    private static readonly Regex XmlWriterMade = new(@"XmlWriter\.Create\(", RegexOptions.CultureInvariant);

    /// <summary>The owner of the line end, which names what the others may not.</summary>
    private const string Owner = "Src/MatPlotLibNet/Extensions/LineEndExtensions.cs";

    /// <summary>The one XML writer (<c>XNode.ToSvgText</c>), whose settings name the line end.</summary>
    private const string XmlWriterOwner = "Src/MatPlotLibNet/Rendering/Svg/SvgDocumentExtensions.cs";

    /// <summary>No writer of the library or its packages ends a line with the operating system's line end
    /// (<c>Environment.NewLine</c>, <c>StringBuilder.AppendLine</c>, an XML writer left at its default): the one line end is
    /// <c>LineEndExtensions.LineEnd</c>. A comment may name them.</summary>
    [Fact]
    public void NoWriter_EndsALineWithTheOperatingSystemsLineEnd()
    {
        var found = new List<string>();
        foreach (var file in RepositoryTree.SourceFiles().Where(f => !string.Equals(f.Path, Owner, StringComparison.OrdinalIgnoreCase)))
        {
            bool xmlWriterOwner = string.Equals(file.Path, XmlWriterOwner, StringComparison.OrdinalIgnoreCase);
            for (int i = 0; i < file.CodeLines.Count; i++)
            {
                string line = file.CodeLines[i];
                if (OperatingSystemLineEnd.IsMatch(line) || (!xmlWriterOwner && XmlWriterMade.IsMatch(line)))
                {
                    found.Add($"{file.Path}:{i + 1}: {line.Trim()}");
                }
            }
        }

        Assert.True(found.Count == 0,
            $"{found.Count} line(s) end a line with the operating system's line end:\n" + string.Join("\n", found));
    }
}
