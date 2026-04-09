// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that batch transform methods produce results equivalent to per-point <c>DataToPixel</c>.</summary>
public class DataTransformBatchTests
{
    private static DataTransform MakeTransform() =>
        new DataTransform(0, 100, 0, 200, new Rect(10, 20, 400, 300));

    [Fact]
    public void TransformX_MatchesPerPointDataToPixelX()
    {
        var t = MakeTransform();
        double[] xData = [0.0, 25.0, 50.0, 75.0, 100.0];
        double[] batch = t.TransformX(xData);
        for (int i = 0; i < xData.Length; i++)
            Assert.Equal(t.DataToPixel(xData[i], 0).X, batch[i], 1e-10);
    }

    [Fact]
    public void TransformY_MatchesPerPointDataToPixelY()
    {
        var t = MakeTransform();
        double[] yData = [0.0, 50.0, 100.0, 150.0, 200.0];
        double[] batch = t.TransformY(yData);
        for (int i = 0; i < yData.Length; i++)
            Assert.Equal(t.DataToPixel(0, yData[i]).Y, batch[i], 1e-10);
    }

    [Fact]
    public void TransformBatch_MatchesPerPointDataToPixel()
    {
        var t = MakeTransform();
        double[] xData = [0.0, 25.0, 50.0, 75.0, 100.0];
        double[] yData = [0.0, 50.0, 100.0, 150.0, 200.0];
        var batch = t.TransformBatch(xData, yData);
        for (int i = 0; i < xData.Length; i++)
        {
            var expected = t.DataToPixel(xData[i], yData[i]);
            Assert.Equal(expected.X, batch[i].X, 1e-10);
            Assert.Equal(expected.Y, batch[i].Y, 1e-10);
        }
    }

    [Fact]
    public void TransformX_ZeroDataRange_ReturnsCenteredX()
    {
        var t = new DataTransform(5, 5, 0, 100, new Rect(10, 20, 400, 300));
        double[] xData = [5.0, 5.0, 5.0];
        double[] batch = t.TransformX(xData);
        double expected = t.DataToPixel(5.0, 50.0).X; // per-point handles zero range
        foreach (var px in batch)
            Assert.Equal(expected, px, 1e-10);
    }

    [Fact]
    public void TransformY_ZeroDataRange_ReturnsCenteredY()
    {
        var t = new DataTransform(0, 100, 3, 3, new Rect(10, 20, 400, 300));
        double[] yData = [3.0, 3.0, 3.0];
        double[] batch = t.TransformY(yData);
        double expected = t.DataToPixel(50.0, 3.0).Y;
        foreach (var py in batch)
            Assert.Equal(expected, py, 1e-10);
    }

    [Fact]
    public void TransformBatch_LargeDataset_MatchesPerPoint()
    {
        // 1003 points: exercises both SIMD (4-wide) path and scalar remainder (3 elements)
        var rng = new Random(42);
        int n = 1003;
        double[] xData = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] yData = Enumerable.Range(0, n).Select(_ => rng.NextDouble() * 200).ToArray();
        var t = new DataTransform(0, n - 1, 0, 200, new Rect(10, 20, 800, 600));

        var batch = t.TransformBatch(xData, yData);

        Assert.Equal(n, batch.Length);
        for (int i = 0; i < n; i++)
        {
            var expected = t.DataToPixel(xData[i], yData[i]);
            Assert.Equal(expected.X, batch[i].X, 1e-10);
            Assert.Equal(expected.Y, batch[i].Y, 1e-10);
        }
    }

    [Fact]
    public void TransformBatch_ZeroBothRanges_AllCentered()
    {
        var t = new DataTransform(5, 5, 3, 3, new Rect(10, 20, 400, 300));
        double[] xData = [5.0, 5.0, 5.0, 5.0, 5.0];
        double[] yData = [3.0, 3.0, 3.0, 3.0, 3.0];
        var batch = t.TransformBatch(xData, yData);
        var expected = t.DataToPixel(5.0, 3.0);
        foreach (var p in batch)
        {
            Assert.Equal(expected.X, p.X, 1e-10);
            Assert.Equal(expected.Y, p.Y, 1e-10);
        }
    }

    [Fact]
    public void TransformBatch_SinglePoint_MatchesPerPoint()
    {
        var t = MakeTransform();
        double[] xData = [42.0];
        double[] yData = [137.0];
        var batch = t.TransformBatch(xData, yData);
        var expected = t.DataToPixel(42.0, 137.0);
        Assert.Equal(expected.X, batch[0].X, 1e-10);
        Assert.Equal(expected.Y, batch[0].Y, 1e-10);
    }

    [Fact]
    public void TransformBatch_Empty_ReturnsEmpty()
    {
        var t = MakeTransform();
        var batch = t.TransformBatch(ReadOnlySpan<double>.Empty, ReadOnlySpan<double>.Empty);
        Assert.Empty(batch);
    }
}
