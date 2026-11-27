// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An SVG document in its parts: the root element, the title, the background, one part per subplot, what the figure
/// draws above them and the scripts. Written plain it is the document <c>ToSvg</c> writes, byte for byte; written for a
/// receiver that patches, every drawn part stands in a group of its own, named, so one part can be replaced alone.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class SvgFigureDocumentTests
{
    private static readonly SvgTransform Svg = new();

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void WrittenPlain_ItIsTheDocumentToSvgWrites(string type)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true);
        figure.EnableRichTooltips = true;

        Assert.Equal(figure.ToSvg(), Svg.RenderDocument(figure).ToSvg());
    }

    [Fact]
    public void WrittenPlain_AFigureWithEveryPartAndServerInteraction_IsTheDocumentToSvgWrites()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        figure.ServerInteraction = true;
        figure.ChartId = "wall";
        figure.Description = "every part";

        Assert.Equal(figure.ToSvg(), Svg.RenderDocument(figure).ToSvg());
    }

    [Fact]
    public void WrittenPlain_AFigureWithNoSubplot_IsTheDocumentToSvgWrites()
    {
        var figure = new Figure { Title = "empty" };
        figure.AddText("only text", 0.5, 0.5);

        Assert.Equal(figure.ToSvg(), Svg.RenderDocument(figure).ToSvg());
    }

    [Fact]
    public void WrittenWrapped_EveryDrawnPartStandsInANamedGroup_InTheOrderTheyAreDrawn()
    {
        var figure = TwoPanels();
        var document = Svg.RenderDocument(figure);

        string wrapped = document.ToWrappedSvg();

        int background = wrapped.IndexOf("<g data-mpl-part=\"background\">", StringComparison.Ordinal);
        int first = wrapped.IndexOf("<g data-mpl-part=\"axes-0\">", StringComparison.Ordinal);
        int second = wrapped.IndexOf("<g data-mpl-part=\"axes-1\">", StringComparison.Ordinal);
        int overlay = wrapped.IndexOf("<g data-mpl-part=\"overlay\">", StringComparison.Ordinal);
        Assert.True(background > 0 && background < first && first < second && second < overlay, wrapped[..Math.Min(400, wrapped.Length)]);
        string scripts = string.Concat(document.Scripts.Select(script => script + "\n"));
        Assert.Equal(
            document.RootTag + document.HeadBody + document.WrappedBackground() + document.WrappedAxes(0) + document.WrappedAxes(1)
                + document.WrappedOverlay() + scripts + "</svg>\n",
            wrapped);
    }

    [Fact]
    public void APartWrittenAlone_IsThePartTheWrappedDocumentHolds()
    {
        var document = Svg.RenderDocument(TwoPanels());
        string wrapped = document.ToWrappedSvg();

        Assert.Contains(document.WrappedBackground(), wrapped);
        Assert.Contains(document.WrappedAxes(0), wrapped);
        Assert.Contains(document.WrappedAxes(1), wrapped);
        Assert.Contains(document.WrappedOverlay(), wrapped);
        Assert.StartsWith(document.RootTag, wrapped);
        Assert.StartsWith("<svg ", document.RootTag);
        Assert.EndsWith(">", document.RootTag.TrimEnd());
    }

    [Fact]
    public void ASubplotNothingChangedIn_IsTheSameContextInTheNextDocument()
    {
        var figure = TwoPanels();
        _ = Svg.RenderDocument(figure);
        var settled = Svg.RenderDocument(figure);

        figure.SubPlots[1].Title = "changed";
        var next = Svg.RenderDocument(figure);

        Assert.Same(settled.Axes[0], next.Axes[0]);
        Assert.NotSame(settled.Axes[1], next.Axes[1]);
    }

    [Fact]
    public void TheHeadAndTheScripts_AreTheirOwnParts()
    {
        var figure = TwoPanels();
        figure.Description = "two panels";
        figure.EnableZoomPan = true;

        var document = Svg.RenderDocument(figure);

        Assert.Contains("<title id=\"chart-title\">", document.HeadBody);
        Assert.Contains("<desc id=\"chart-desc\">two panels</desc>", document.HeadBody);
        Assert.NotEmpty(document.Scripts);
        Assert.All(document.Scripts, script => Assert.Contains("<script", script));
    }

    private static Figure TwoPanels() => Plt.Create()
        .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
        .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2, 3], [2.0, 1, 3]))
        .Build();
}
