// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="SmallMultiplesBuilder"/> — one mini panel per series, wrapped into a grid.
/// <para>Beyond five or six lines a legend becomes the picture; small multiples put the name IN the panel
/// and give every series the same axes, so twenty processes read as twenty comparable shapes. The builder
/// owns the wrap, the shared limits and the in-panel label, so a caller never writes row/column
/// arithmetic of its own.</para></summary>
public class SmallMultiplesBuilderTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 16, 0, 0, DateTimeKind.Utc);
    private static readonly double[] X = [Now.AddSeconds(-2).ToOADate(), Now.AddSeconds(-1).ToOADate(), Now.ToOADate()];
    private static readonly double[] Y = [1, 2, 3];

    private static SmallMultiplesBuilder Five()
    {
        var b = Plt.SmallMultiples();
        for (int i = 0; i < 5; i++)
        {
            b.AddPanel($"P{i}", X, Y);
        }
        return b;
    }

    [Fact]
    public void EachPanel_IsItsOwnSubplot()
    {
        var figure = Five().Build().Build();

        Assert.Equal(5, figure.SubPlots.Count);
    }

    /// <summary>Five panels at four per row wrap BALANCED — 3+2, never 4+1 — the ops tile row's own rule.</summary>
    [Fact]
    public void TheWrap_IsBalanced()
    {
        var figure = Five().WithMaxCols(4).WithPanelSize(200, 100).Build().Build();

        Assert.Equal(200 * 3, figure.Width);
        Assert.Equal(100 * 2, figure.Height);
    }

    [Fact]
    public void ThePanelsShareTheYLimits()
    {
        var figure = Five().WithSharedYLimits(0, 100).Build().Build();

        Assert.All(figure.SubPlots, ax =>
        {
            Assert.Equal(0, ax.YAxis.Min);
            Assert.Equal(100, ax.YAxis.Max);
        });
    }

    [Fact]
    public void TheWindow_PinsEveryPanelsXAxis()
    {
        var figure = Five().WithWindow(Now, TimeSpan.FromMinutes(3)).Build().Build();

        Assert.All(figure.SubPlots, ax =>
        {
            Assert.Equal(Now.AddMinutes(-3).ToOADate(), ax.XAxis.Min);
            Assert.Equal(Now.ToOADate(), ax.XAxis.Max);
        });
    }

    /// <summary>The name sits INSIDE the panel as an axes-fraction annotation — not a title above it, which
    /// spends a row of height per panel, and not a legend, which is what small multiples exist to avoid.</summary>
    [Fact]
    public void TheLabel_IsInsideThePanel_NotATitleNorALegend()
    {
        var figure = Five().Build().Build();

        Assert.All(figure.SubPlots, ax =>
        {
            var label = Assert.Single(ax.Annotations);
            Assert.Equal(AnnotationCoordinates.AxesFraction, label.Coordinates);
            Assert.True(string.IsNullOrEmpty(ax.Title));
            Assert.False(ax.Legend.Visible);
        });
        Assert.Equal("P3", figure.SubPlots[3].Annotations[0].Text);
    }

    [Fact]
    public void ConfigurePanel_RunsOnEveryPanel()
    {
        var figure = Five().ConfigurePanel(ax => ax.AxHLine(100)).Build().Build();

        Assert.All(figure.SubPlots, ax => Assert.Single(ax.ReferenceLines));
    }

    [Fact]
    public void NoPanels_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Plt.SmallMultiples().Build());
    }

    [Fact]
    public void ItRendersToSvg()
    {
        string svg = Five().WithSharedYLimits(0, 100).WithWindow(Now, TimeSpan.FromMinutes(3)).Build().ToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("P4", svg);
    }
}
