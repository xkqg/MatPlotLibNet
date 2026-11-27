// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SpinesConfig"/> and <see cref="SpineConfig"/> behavior.</summary>
public class SpinesConfigTests
{
    [Fact]
    public void SpinesConfig_RecordEquality_Works()
    {
        var a = new SpinesConfig();
        var b = new SpinesConfig();
        Assert.Equal(a, b);
    }

    /// <summary>One spine of an axes is changed through its edge, and no other spine is touched.</summary>
    [Fact]
    public void AnEdgeChanged_ChangesTheSpineOnThatEdgeOnly()
    {
        var spines = new SpinesConfig();

        foreach (var edge in Enum.GetValues<SpineEdge>())
        {
            var changed = spines.WithEdge(edge, spine => spine with { LineWidth = 3 });

            foreach (var other in Enum.GetValues<SpineEdge>())
                Assert.Equal(other == edge ? 3 : spines.On(other).LineWidth, changed.On(other).LineWidth);
        }
    }

    /// <summary>Spines hidden are hidden edge by edge, whichever are named, and the others stay as they were.</summary>
    [Fact]
    public void SpinesHidden_AreTheOnesNamed_AndNoOther()
    {
        var spines = new SpinesConfig().Hiding(SpineEdge.Top, SpineEdge.Left);

        Assert.Equal([false, true, false, true], [spines.Top.Visible, spines.Bottom.Visible, spines.Left.Visible, spines.Right.Visible]);
        Assert.Equal(new SpinesConfig(), new SpinesConfig().Hiding());
    }

    /// <summary>Spines hidden on several edges are one copy of the spines, whatever the number of edges named: a row of stat tiles
    /// hides all four spines of every tile, and a copy for each edge made its build slower than it was.</summary>
    [Fact]
    public void SpinesHiddenOnEveryEdge_AreOneCopyOfTheSpines()
    {
        var spines = new SpinesConfig();
        _ = spines.Hiding(SpineEdge.Top);
        _ = spines.Hiding(SpineEdge.Top, SpineEdge.Bottom, SpineEdge.Left, SpineEdge.Right);
        _ = spines.Top with { Visible = false };

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = spines.Hiding(SpineEdge.Top);
        long oneEdge = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        _ = spines.Top with { Visible = false };
        long oneSpine = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        _ = spines.Hiding(SpineEdge.Top, SpineEdge.Bottom, SpineEdge.Left, SpineEdge.Right);
        long everyEdge = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(oneEdge + (3 * oneSpine), everyEdge);
    }
}
