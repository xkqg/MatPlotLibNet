// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 — Verifies <see cref="DendrogramSeries"/> default properties, mutation,
/// and visitor dispatch. Companion render and serialization behaviour live in
/// <c>DendrogramRenderTests</c> and <c>DendrogramSerializationTests</c>.</summary>
public class DendrogramSeriesTests
{
    private static TreeNode SampleTree => DendrogramTreeFixtures.TwoLeaf();

    [Fact]
    public void Accept_DispatchesToDendrogramVisitor()
    {
        var series = new DendrogramSeries(SampleTree);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(DendrogramSeries), visitor.LastVisited);
    }
}
