// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

public sealed class SvgDiffEngineTests
{
    private const string SvgTemplate = """
        <svg>
          <g class="axes">
            <g data-series-index="0" class="line">{0}</g>
            <g data-series-index="1" class="scatter">{1}</g>
          </g>
        </svg>
        """;

    [Fact]
    public void IdenticalSvg_EmptyPatches()
    {
        var svg = string.Format(SvgTemplate, "<polyline points='0,0 1,1'/>", "<circle r='3'/>");
        var patch = SvgDiffEngine.Compute(svg, svg);
        Assert.Empty(patch.Patches);
        Assert.False(patch.IsFullReplace);
    }

    [Fact]
    public void OneSeries_Changed_OnePatch()
    {
        var prev = string.Format(SvgTemplate, "<polyline points='0,0 1,1'/>", "<circle r='3'/>");
        var curr = string.Format(SvgTemplate, "<polyline points='0,0 1,1 2,2'/>", "<circle r='3'/>");
        var patch = SvgDiffEngine.Compute(prev, curr);

        Assert.Single(patch.Patches);
        Assert.Equal(0, patch.Patches[0].SeriesIndex);
        Assert.Contains("2,2", patch.Patches[0].NewContent);
        Assert.False(patch.IsFullReplace);
    }

    [Fact]
    public void BothSeries_Changed_TwoPatches()
    {
        var prev = string.Format(SvgTemplate, "<polyline points='0,0'/>", "<circle r='3'/>");
        var curr = string.Format(SvgTemplate, "<polyline points='1,1'/>", "<circle r='5'/>");
        var patch = SvgDiffEngine.Compute(prev, curr);

        Assert.Equal(2, patch.Patches.Count);
    }

    [Fact]
    public void SeriesCount_Changed_FullReplace()
    {
        var prev = string.Format(SvgTemplate, "<polyline/>", "<circle/>");
        var curr = """
            <svg>
              <g class="axes">
                <g data-series-index="0" class="line"><polyline/></g>
              </g>
            </svg>
            """;
        var patch = SvgDiffEngine.Compute(prev, curr);
        Assert.True(patch.IsFullReplace);
    }

    [Fact]
    public void CompressionRatio_CalculatesCorrectly()
    {
        var prev = string.Format(SvgTemplate, "<polyline points='0,0 1,1'/>", "<circle r='3'/>");
        var curr = string.Format(SvgTemplate, "<polyline points='0,0 1,1 2,2'/>", "<circle r='3'/>");
        var patch = SvgDiffEngine.Compute(prev, curr);

        double ratio = SvgDiffEngine.CompressionRatio(patch, curr.Length);
        Assert.True(ratio > 0.5); // patch should be much smaller than full SVG
    }

    [Fact]
    public void CompressionRatio_FullReplace_ReturnsZero()
    {
        var patch = new SvgDiffEngine.SvgPatch([], true);
        Assert.Equal(0.0, SvgDiffEngine.CompressionRatio(patch, 1000));
    }

    [Fact]
    public void CompressionRatio_EmptyPatch_ReturnsOne()
    {
        var patch = new SvgDiffEngine.SvgPatch([], false);
        Assert.Equal(1.0, SvgDiffEngine.CompressionRatio(patch, 1000));
    }

    [Fact]
    public void NoSeriesGroups_FullReplace()
    {
        var prev = "<svg><rect/></svg>";
        var curr = "<svg><circle/></svg>";
        var patch = SvgDiffEngine.Compute(prev, curr);
        // Both have 0 series groups → same count → empty patches
        Assert.Empty(patch.Patches);
        Assert.False(patch.IsFullReplace);
    }

    [Fact]
    public void NewSeriesAdded_FullReplace()
    {
        var prev = """<svg><g data-series-index="0" class="x">A</g></svg>""";
        var curr = """<svg><g data-series-index="0" class="x">A</g><g data-series-index="1" class="y">B</g></svg>""";
        var patch = SvgDiffEngine.Compute(prev, curr);
        Assert.True(patch.IsFullReplace);
    }
}
