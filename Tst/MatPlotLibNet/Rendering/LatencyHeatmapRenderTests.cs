// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// The chart a control room calls a latency heatmap: a real clock along the bottom, latency buckets up the side
/// on a log scale, and a count in every cell — a histogram over time, one column per minute. Everything it needs
/// already shipped, and nothing tested the three parts together: no test anywhere paired a mesh with a date axis
/// or with a log scale, and the cookbook had no recipe for it. This file is that pairing, so the recipe cannot
/// quietly stop working.
/// </summary>
public class LatencyHeatmapRenderTests
{
    private static readonly DateTime Start = new(2026, 9, 12, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>Six one-minute columns of counts over five latency buckets, drawn the way the recipe draws it.</summary>
    private static string Render()
    {
        double[] minutes = [.. Enumerable.Range(0, 7).Select(i => Start.AddMinutes(i).ToOADate())];
        double[] buckets = [1, 5, 25, 100, 500, 2500];           // milliseconds, deliberately not evenly stepped
        var counts = new double[5, 6];
        var rng = new Random(7);
        for (int bucket = 0; bucket < 5; bucket++)
        {
            for (int minute = 0; minute < 6; minute++)
            {
                counts[bucket, minute] = Math.Round(rng.NextDouble() * 100);
            }
        }

        return Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Pcolormesh(minutes, buckets, counts)
                .SetXDateAxis()
                .SetYScale(AxisScale.Log)
                .WithTitle("Request latency"))
            .Build().ToSvg();
    }

    private static (double X, double Y, double W, double H)[] Rects(string svg) =>
        [.. Regex.Matches(svg, @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)""")
            .Select(m => (
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)))];

    private static string[] Labels(string svg) =>
        [.. Regex.Matches(svg, @"<text[^>]*>([^<]+)</text>").Select(m => m.Groups[1].Value)];

    [Fact]
    public void EveryBucketAndEveryMinute_GetsItsOwnCell()
    {
        var cells = Rects(Render()).Where(r => r.W < 600).ToArray();

        Assert.Equal(30, cells.Length);   // 5 buckets × 6 minutes
    }

    [Fact]
    public void TheBottomAxis_ReadsAsAClock()
    {
        // The x edges are OLE Automation numbers. Without a date axis the reader sees 46277.58.
        Assert.Contains(Labels(Render()), label => label.Contains(':', StringComparison.Ordinal));
    }

    [Fact]
    public void TheSideAxis_ReadsAsDecades()
    {
        Assert.Contains(Labels(Render()), label => label.StartsWith("10", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRowHeights_FollowTheBucketEdges_NotTheRowCount()
    {
        // 1→5, 5→25, 100→500 and 500→2500 are each a factor of five; 25→100 is a factor of four, so on a log
        // axis that one row is shorter than the other four. Equal rows would mean the edges were ignored.
        var rows = Rects(Render())
            .Where(r => r.H < 400)
            .GroupBy(r => Math.Round(r.Y, 1))
            .OrderBy(group => group.Key)
            .Select(group => Math.Round(group.First().H, 2))
            .ToArray();

        Assert.Equal(5, rows.Length);
        Assert.Equal(2, rows.Distinct().Count());
        // Top to bottom the buckets are 500-2500, 100-500, 25-100, 5-25, 1-5, so the third row is the short one.
        Assert.Equal(rows.Min(), rows[2]);
    }

    [Fact]
    public void TheCells_FillThePlotAreaOnALogAxisToo()
    {
        string svg = Render();
        var all = Rects(svg);
        var frame = all.Where(r => r.W < 799).OrderByDescending(r => r.W * r.H).First();
        var cells = all.Where(r => r.W < frame.W - 0.5).ToArray();

        Assert.Equal(frame.X, cells.Min(c => c.X), 1);
        Assert.Equal(frame.X + frame.W, cells.Max(c => c.X + c.W), 1);
        Assert.Equal(frame.Y, cells.Min(c => c.Y), 1);
        Assert.Equal(frame.Y + frame.H, cells.Max(c => c.Y + c.H), 1);
    }

    [Fact]
    public void TheDataTable_CarriesTheClockAndTheBucketsAndTheCounts()
    {
        // What a reader who cannot see the picture gets, and what the MCP server hands a model.
        double[] minutes = [.. Enumerable.Range(0, 3).Select(i => Start.AddMinutes(i).ToOADate())];
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Pcolormesh(minutes, [1, 5, 25], new double[,] { { 4, 5 }, { 6, 7 } })
                .SetXDateAxis()
                .SetYScale(AxisScale.Log))
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(4, table.RowCount);
        Assert.Contains("2026-09-12 14:00", table.ToMarkdown(), StringComparison.Ordinal);
    }
}
