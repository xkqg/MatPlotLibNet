// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet.Tests.Performance;

/// <summary>Verifies <see cref="ViewportCuller"/> behavior.</summary>
public class ViewportCullerTests
{
    /// <summary>Verifies that points strictly within range are all included.</summary>
    [Fact]
    public void Cull_PointsWithinRange_AllIncluded()
    {
        double[] x = [1, 2, 3, 4, 5];
        double[] y = [10, 20, 30, 40, 50];
        var (outX, outY) = ViewportCuller.Cull(x, y, 0, 6);
        Assert.Equal(5, outX.Length);
    }

    /// <summary>Verifies that points completely outside range are excluded (but one each side kept for line clipping).</summary>
    [Fact]
    public void Cull_PointsOutsideRange_Excluded()
    {
        double[] x = [100, 200, 300];
        double[] y = [1, 2, 3];
        var (outX, outY) = ViewportCuller.Cull(x, y, 0, 10);
        Assert.Empty(outX);
    }

    /// <summary>Verifies that one point just before range is kept for line clipping.</summary>
    [Fact]
    public void Cull_OnePointBeforeRange_IsKeptForLineClipping()
    {
        // Points: x = [-1, 0.5, 2, 3, 4] — range is [1, 5]
        // -1 is before range, 0.5 is before range, 2..4 are inside
        // The last point before the range start (x=0.5) should be kept
        double[] x = [-1, 0.5, 2, 3, 4];
        double[] y = [0, 1, 2, 3, 4];
        var (outX, outY) = ViewportCuller.Cull(x, y, 1, 5);
        // outX should include 0.5 (just before range) and 2,3,4
        Assert.Contains(0.5, outX);
    }

    /// <summary>Verifies that one point just after range is kept for line clipping.</summary>
    [Fact]
    public void Cull_OnePointAfterRange_IsKeptForLineClipping()
    {
        // Points: x = [1, 2, 3, 5.5, 10] — range is [0, 4]
        // 5.5 and 10 are after range, first one after (5.5) should be kept
        double[] x = [1, 2, 3, 5.5, 10];
        double[] y = [1, 2, 3, 4, 5];
        var (outX, outY) = ViewportCuller.Cull(x, y, 0, 4);
        Assert.Contains(5.5, outX);
        Assert.DoesNotContain(10.0, outX);
    }

    /// <summary>Verifies empty input returns empty output.</summary>
    [Fact]
    public void Cull_EmptyInput_ReturnsEmpty()
    {
        var (outX, outY) = ViewportCuller.Cull([], [], 0, 10);
        Assert.Empty(outX);
        Assert.Empty(outY);
    }

    /// <summary>Verifies that output X and Y lengths are always equal.</summary>
    [Fact]
    public void Cull_OutputXAndYSameLengths()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        double[] y = [0, 1, 4, 9, 16, 25, 36, 49, 64, 81];
        var (outX, outY) = ViewportCuller.Cull(x, y, 2, 7);
        Assert.Equal(outX.Length, outY.Length);
    }

    /// <summary>Verifies that all output X values maintain their original order.</summary>
    [Fact]
    public void Cull_OutputMaintainsOriginalOrder()
    {
        double[] x = [0, 1, 2, 3, 4, 5];
        double[] y = [0, 1, 2, 3, 4, 5];
        var (outX, _) = ViewportCuller.Cull(x, y, 1, 4);
        for (int i = 1; i < outX.Length; i++)
            Assert.True(outX[i] > outX[i - 1]);
    }
}
