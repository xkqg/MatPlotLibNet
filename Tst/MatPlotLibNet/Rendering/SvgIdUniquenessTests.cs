// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An id in an SVG names one element. Every subplot is drawn by a render context of its own that counted its clip,
/// hatch and gradient definitions from zero, so the second subplot of a figure defined <c>clip-0</c> again and its clipped
/// group resolved to whichever definition the browser met first. The first subplot keeps the ids it always had, so the SVG of
/// a one-plot figure is the same bytes; each later subplot prefixes its own, and <see cref="SaveOptions.SvgIdPrefix"/> gives
/// the figures that share an HTML page a prefix each.</summary>
public class SvgIdUniquenessTests
{
    private static Figure TwoHatchedPlots() => Plt.Create().WithSize(800, 380)
        .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]).Bar(["A", "B"], [1.0, 2.0], s => s.Hatch = HatchPattern.ForwardDiagonal).SetYLim(0, 5))
        .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2, 3], [3.0, 1, 2]).Bar(["A", "B"], [3.0, 4.0], s => s.Hatch = HatchPattern.Cross).SetYLim(0, 5))
        .Build();

    private static Figure OneHatchedPlot() => Plt.Create().WithSize(400, 380)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]).Bar(["A", "B"], [1.0, 2.0], s => s.Hatch = HatchPattern.ForwardDiagonal).SetYLim(0, 5))
        .Build();

    private static void AssertEveryIdOnce_AndEveryReferenceResolves(string svg)
    {
        var ids = svg.Ids();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        var unresolved = svg.UrlReferences().Where(reference => !ids.Contains(reference)).ToArray();
        Assert.True(unresolved.Length == 0, $"References with no definition: {string.Join(", ", unresolved)}.");
    }

    [Fact]
    public void ATwoPlotFigure_DefinesEveryIdOnce()
    {
        string svg = TwoHatchedPlots().ToSvg();

        AssertEveryIdOnce_AndEveryReferenceResolves(svg);
        Assert.Contains(svg.Ids(), id => id.StartsWith("s1-clip-", StringComparison.Ordinal));
        Assert.Contains(svg.Ids(), id => id.StartsWith("s1-hatch-", StringComparison.Ordinal));
    }

    [Fact]
    public void TheClippedGroupOfEachPlot_ResolvesToTheClipDefinedForThatPlot()
    {
        string svg = TwoHatchedPlots().ToSvg();

        // A reference resolves to the definition that stands nearest before it: the plot defines its clip, then clips its group.
        foreach (string reference in svg.UrlReferences().Where(id => id.Contains("clip-", StringComparison.Ordinal)))
        {
            int use = svg.IndexOf($"url(#{reference})", StringComparison.Ordinal);
            int definition = svg.LastIndexOf($"id=\"{reference}\"", use, StringComparison.Ordinal);
            Assert.True(definition >= 0, $"{reference} is used before it is defined.");
            Assert.Equal(1, svg.Ids().Count(id => id == reference));
        }
    }

    [Fact]
    public void TheFirstPlot_KeepsTheIdsItAlwaysHad()
    {
        string svg = TwoHatchedPlots().ToSvg();

        Assert.Contains("clip-0", svg.Ids());
        Assert.Contains("hatch-0", svg.Ids());
    }

    [Fact]
    public void AOnePlotFigure_WritesTheIdsOfAlways_WithNoPrefix()
    {
        string svg = OneHatchedPlot().ToSvg();

        Assert.Equal(["chart-title", "clip-0", "clip-1", "hatch-0"], svg.Ids().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        Assert.DoesNotContain("s0-", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void APrefixInTheSaveOptions_PrefixesEveryIdAndEveryReferenceToOne()
    {
        string svg = TwoHatchedPlots().ToSvg().WithSaveOptions(new SaveOptions { SvgIdPrefix = "fig1-" });

        AssertEveryIdOnce_AndEveryReferenceResolves(svg);
        Assert.All(svg.Ids(), id => Assert.StartsWith("fig1-", id, StringComparison.Ordinal));
        Assert.Contains("aria-labelledby=\"fig1-chart-title\"", svg, StringComparison.Ordinal);
        Assert.NotNull(XDocument.Parse(svg));
    }

    [Fact]
    public void TwoFiguresWithTheirOwnPrefix_ShareAPageWithoutSharingAnId()
    {
        string first = TwoHatchedPlots().ToSvg().WithSaveOptions(new SaveOptions { SvgIdPrefix = "a-" });
        string second = TwoHatchedPlots().ToSvg().WithSaveOptions(new SaveOptions { SvgIdPrefix = "b-" });

        AssertEveryIdOnce_AndEveryReferenceResolves(first + second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoPrefix_LeavesTheDocumentAsItWas(string? prefix)
    {
        string svg = TwoHatchedPlots().ToSvg();

        Assert.Equal(svg, svg.WithSaveOptions(new SaveOptions { SvgIdPrefix = prefix }));
    }

    [Fact]
    public void APrefixThatIsNotAnXmlName_IsRefusedByName()
    {
        string svg = OneHatchedPlot().ToSvg();

        var ex = Assert.Throws<ArgumentException>(() => svg.WithSaveOptions(new SaveOptions { SvgIdPrefix = "1 bad" }));

        Assert.Contains(nameof(SaveOptions.SvgIdPrefix), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePrefixAndTheOtherSettings_ComposeInOneRun()
    {
        string svg = OneHatchedPlot().ToSvg().WithSaveOptions(new SaveOptions { SvgIdPrefix = "p-", SvgDecimalPrecision = 1 });

        Assert.All(svg.Ids(), id => Assert.StartsWith("p-", id, StringComparison.Ordinal));
    }
}
