// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A hatch is a tile that repeats. At a higher resolution the tile has to be drawn at the device's pixels, not
/// drawn small and stretched: the stretched tile turns a one-unit stroke into soft, doubled half-tones and a print
/// of the figure shows exactly that.</summary>
public class HatchResolutionTests
{
    private static readonly SKColor Ink = new(0, 0, 0);

    private static Figure HatchedBars(double dpi) =>
        Plt.Create().WithSize(200, 150).WithDpi(dpi)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [3.0, 5.0], s =>
            {
                s.Color = Colors.Gray;
                s.Hatch = HatchPattern.Horizontal;
                s.HatchColor = Colors.Black;
            }))
            .Build();

    /// <summary>The rows, down the middle of the tallest bar, at which the hatch ink is solid.</summary>
    private static List<int> InkRows(byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);

        // The tallest bar is the right one: find a column through its body by the grey that hatch leaves between strokes.
        int column = -1;
        int tallest = 0;
        for (int x = 0; x < bitmap.Width; x++)
        {
            int grey = 0;
            for (int y = 0; y < bitmap.Height; y++)
                if (bitmap.GetPixel(x, y) == new SKColor(128, 128, 128)) grey++;
            if (grey > tallest) { tallest = grey; column = x; }
        }

        Assert.True(column >= 0, "no bar body found");
        var rows = new List<int>();
        for (int y = 0; y < bitmap.Height; y++)
            if (bitmap.GetPixel(column, y) == Ink && IsInsideBar(bitmap, column, y))
                rows.Add(y);
        return rows;
    }

    /// <summary>Grey above and below on the same column: the pixel is a stroke of the hatch, not a letter or a spine.</summary>
    private static bool IsInsideBar(SKBitmap bitmap, int x, int y)
    {
        var grey = new SKColor(128, 128, 128);
        bool above = false, below = false;
        for (int d = 1; d <= 8 && !above; d++)
            if (y - d >= 0 && bitmap.GetPixel(x, y - d) == grey) above = true;
        for (int d = 1; d <= 8 && !below; d++)
            if (y + d < bitmap.Height && bitmap.GetPixel(x, y + d) == grey) below = true;
        return above && below;
    }

    [Fact]
    public void AtTwiceTheResolution_AStrokeIsTwoSolidPixelsEveryTwelve()
    {
        var runs = Runs(InkRows(HatchedBars(192).ToPng()));

        // A one-unit stroke every six units is two device pixels every twelve, fully inked.
        Assert.True(runs.Count >= 4, $"expected a hatch of solid strokes, found {runs.Count}");
        Assert.All(runs, run => Assert.Equal(2, run.Length));
        Assert.All(runs.Skip(1).Zip(runs, (next, prev) => next.Start - prev.Start), gap => Assert.Equal(12, gap));
    }

    private readonly record struct InkRun(int Start, int Length);

    /// <summary>The rows grouped into runs of consecutive rows.</summary>
    private static List<InkRun> Runs(List<int> rows)
    {
        var runs = new List<InkRun>();
        foreach (int row in rows)
        {
            if (runs.Count > 0 && runs[^1].Start + runs[^1].Length == row)
                runs[^1] = runs[^1] with { Length = runs[^1].Length + 1 };
            else
                runs.Add(new InkRun(row, 1));
        }

        return runs;
    }

    [Fact]
    public void AtTheDefaultResolution_TheTileIsDrawnAsItAlwaysWas()
    {
        // At scale 1 the half-tone of a one-unit stroke across two rows is the tile's own rendering: no solid ink row.
        var rows = InkRows(HatchedBars(96).ToPng());

        Assert.Empty(rows);
    }
}
