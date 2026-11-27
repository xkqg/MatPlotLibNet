// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers.Hierarchical;

/// <summary>The data attributes a treemap tile carries (<c>data-treemap-node</c>, <c>-depth</c>, <c>-parent</c> and, for a tile, <c>-label</c>)
/// are written by one member, <c>TagTreemapNode</c>, in that order: the drill-down script reads them to walk the hierarchy.</summary>
public class TreemapNodeTagTests
{
    private static List<(string Key, string Value)> Written(Action<IRenderContext> tag)
    {
        var ctx = new RecordingRenderContext();
        tag(ctx);
        return [.. ctx.Of<SetNextElementDataCall>().Select(c => (c.Key, c.Value))];
    }

    [Fact]
    public void ATile_CarriesItsNodeDepthParentAndLabelInThatOrder()
    {
        var written = Written(ctx => TreemapSeriesRenderer.TagTreemapNode(ctx, "0.2", 3, "0", "alpha"));

        Assert.Equal(
            [("treemap-node", "0.2"), ("treemap-depth", "3"), ("treemap-parent", "0"), ("treemap-label", "alpha")],
            written);
    }

    [Fact]
    public void ALabelOfItsOwn_IsLeftOutWhereTheTextIsTheOneDrawnForTheTile()
    {
        var written = Written(ctx => TreemapSeriesRenderer.TagTreemapNode(ctx, "0.2", 3, "0"));

        Assert.Equal([("treemap-node", "0.2"), ("treemap-depth", "3"), ("treemap-parent", "0")], written);
    }

    [Fact]
    public void AnEmptyLabel_IsWrittenAsEmpty()
    {
        var written = Written(ctx => TreemapSeriesRenderer.TagTreemapNode(ctx, "0", 0, "", string.Empty));

        Assert.Equal(("treemap-label", ""), written[^1]);
    }

    [Fact]
    public void TheDepth_IsWrittenWithTheInvariantCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            Assert.Equal(("treemap-depth", "12"), Written(ctx => TreemapSeriesRenderer.TagTreemapNode(ctx, "0", 12, ""))[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
