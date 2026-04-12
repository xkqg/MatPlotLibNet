// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet.Tests.Rendering.Downsampling;

/// <summary>Verifies <see cref="MonotonicViewportSlicer"/> slices and downsamples correctly
/// for all viewport/data boundary combinations.</summary>
public class MonotonicViewportSlicerTests
{
    // A simple uniform-rate stub: x[i] = i * 1.0, y[i] = i * 2.0, length = n
    private sealed class UniformStub : IMonotonicXY
    {
        private readonly int _n;
        public UniformStub(int n) => _n = n;
        public int Length => _n;
        public IndexRange IndexRangeFor(double xMin, double xMax)
        {
            int s = (int)Math.Max(0, Math.Floor(xMin));
            int e = (int)Math.Min(_n, Math.Ceiling(xMax) + 1);
            return s >= e ? new(0, 0) : new(s, e);
        }
        public double XAt(int i) => i;
        public double YAt(int i) => i * 2.0;
    }

    // ── viewport fully inside data ────────────────────────────────────────────

    [Fact]
    public void Slice_ViewportInside_ReturnsSubset()
    {
        var stub = new UniformStub(100);
        var result = MonotonicViewportSlicer.Slice(stub, 20, 30, null);
        Assert.True(result.X.Length > 0);
        Assert.All(result.X, x => Assert.True(x >= 20 && x <= 31));
    }

    [Fact]
    public void Slice_ViewportInside_XYParallel()
    {
        var stub = new UniformStub(50);
        var result = MonotonicViewportSlicer.Slice(stub, 10, 20, null);
        Assert.Equal(result.X.Length, result.Y.Length);
        for (int i = 0; i < result.X.Length; i++)
            Assert.Equal(result.X[i] * 2.0, result.Y[i], 10);
    }

    // ── viewport entirely outside (left) ─────────────────────────────────────

    [Fact]
    public void Slice_OutsideLeft_ReturnsEmpty()
    {
        var stub = new UniformStub(100); // x = 0..99
        var result = MonotonicViewportSlicer.Slice(stub, -50, -10, null);
        Assert.Empty(result.X);
        Assert.Empty(result.Y);
    }

    // ── viewport entirely outside (right) ────────────────────────────────────

    [Fact]
    public void Slice_OutsideRight_ReturnsEmpty()
    {
        var stub = new UniformStub(100); // x = 0..99
        var result = MonotonicViewportSlicer.Slice(stub, 200, 300, null);
        Assert.Empty(result.X);
        Assert.Empty(result.Y);
    }

    // ── viewport spans entire data ────────────────────────────────────────────

    [Fact]
    public void Slice_SpanningViewport_ReturnsAll()
    {
        var stub = new UniformStub(10);
        var result = MonotonicViewportSlicer.Slice(stub, -100, 100, null);
        Assert.Equal(10, result.X.Length);
    }

    // ── empty source ─────────────────────────────────────────────────────────

    [Fact]
    public void Slice_EmptySource_ReturnsEmpty()
    {
        var stub = new UniformStub(0);
        var result = MonotonicViewportSlicer.Slice(stub, 0, 100, null);
        Assert.Empty(result.X);
        Assert.Empty(result.Y);
    }

    // ── single point inside viewport ─────────────────────────────────────────

    [Fact]
    public void Slice_SinglePoint_ReturnsOnePoint()
    {
        var stub = new UniformStub(1); // x[0] = 0
        var result = MonotonicViewportSlicer.Slice(stub, -1, 1, null);
        Assert.Single(result.X);
        Assert.Equal(0.0, result.X[0]);
        Assert.Equal(0.0, result.Y[0]);
    }

    // ── ±Infinity bounds ─────────────────────────────────────────────────────

    [Fact]
    public void Slice_InfinityBounds_ReturnsAll()
    {
        var stub = new UniformStub(5);
        var result = MonotonicViewportSlicer.Slice(stub, double.NegativeInfinity, double.PositiveInfinity, null);
        Assert.Equal(5, result.X.Length);
    }

    // ── downsampling applied when maxPoints < slice length ───────────────────

    [Fact]
    public void Slice_WithMaxPoints_DownsamplesToTarget()
    {
        var stub = new UniformStub(1000);
        var result = MonotonicViewportSlicer.Slice(stub, 0, 999, maxPoints: 100);
        Assert.True(result.X.Length <= 100);
    }

    [Fact]
    public void Slice_MaxPointsLargerThanSlice_ReturnsFullSlice()
    {
        var stub = new UniformStub(10);
        var result = MonotonicViewportSlicer.Slice(stub, 0, 9, maxPoints: 1000);
        Assert.Equal(10, result.X.Length);
    }
}
