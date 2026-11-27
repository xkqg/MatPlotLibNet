// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A figure drawn again draws an axes again only when something it draws changed; an axes nothing changed in keeps
/// the markup of the draw before. Every fact here holds the picture drawn that way to the one drawn from scratch: after every
/// property a user can set, every value changed in place in the data, a change to an axes another shares a scale with, a
/// layout that moves an axes, an interaction and a change to the figure itself.
/// <para>In the global-state collection: what the process measures text and writes glyphs with is part of every axes' key, and
/// a test elsewhere that loads the Skia package swaps both mid-run, which rightly draws every axes again; the facts here that
/// say an axes was handed out again need both to hold still between their draws.</para></summary>
[Collection("ChartServicesGlobalState")]
public sealed class SvgAxesReuseTests
{
    /// <summary>The picture <paramref name="figure"/> draws from scratch, leaving what the last draw kept as it was.</summary>
    internal static string FromScratch(Figure figure)
    {
        var kept = figure.DrawnSvgAxes;
        figure.DrawnSvgAxes = null;
        try
        {
            return figure.ToSvg();
        }
        finally
        {
            figure.DrawnSvgAxes = kept;
        }
    }

    /// <summary>The axes a figure has drawn once it has settled.</summary>
    private static MatPlotLibNet.Rendering.Frames.KeptAxesDraw<MatPlotLibNet.Rendering.Svg.SvgRenderContext>[] Settled(Figure figure)
    {
        Settle(figure);
        return figure.DrawnSvgAxes!;
    }

    /// <summary>What the figure draws after <paramref name="evt"/> has been applied to it.</summary>
    private static string AfterApplying(Figure figure, FigureInteractionEvent evt)
    {
        evt.ApplyTo(figure);
        return figure.ToSvg();
    }

    /// <summary>Draws <paramref name="figure"/> twice: an axes keeps a copy of its data from the second draw under the same
    /// key on, so only a settled figure can hand anything out on the draw after a change.</summary>
    internal static void Settle(Figure figure)
    {
        _ = figure.ToSvg();
        _ = figure.ToSvg();
    }

    /// <summary>A second subplot beside the fixture's, with a line of its own, so a change to one axes leaves another.</summary>
    private static Figure WithASecondAxes(Figure figure)
    {
        var other = figure.AddSubPlot(1, 2, 2);
        other.AddSeries(new LineSeries([1.0, 2, 3], [3.0, 1, 2]));
        return figure;
    }

    /// <summary>Every drawn property of a series, each set on a settled figure at rest, is drawn as a fresh draw draws it; a property
    /// drawn otherwise is named as the failing item.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ASeriesPropertySet_IsDrawnAsAFreshDrawDrawsIt(string type)
    {
        Assert.All(ChangeStampContractTests.ChangedAfterMade(type), property =>
        {
            var figure = WithASecondAxes(DrawnSeriesFixtures.Build(type, everyPropertySet: false));
            Settle(figure);
            var series = DrawnSeriesFixtures.TheSeries(figure);
            var info = series.GetType().GetProperty(property)!;

            info.SetValue(series, DrawnSeriesFixtures.NotTheDefault(info, info.GetValue(series), 0));

            Assert.Equal(FromScratch(figure), figure.ToSvg());
        });
    }

    /// <summary>Every drawn property of a part, each set on a settled figure at rest, is drawn as a fresh draw draws it.</summary>
    [Theory]
    [MemberData(nameof(ChangeStampContractTests.PartsChangedInPlace), MemberType = typeof(ChangeStampContractTests))]
    public void APartSet_IsDrawnAsAFreshDrawDrawsIt(string part)
    {
        Assert.All(DrawnAxesFixtures.DrawnPaths(DrawnAxesFixtures.Part(part)), path =>
        {
            var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
            Settle(figure);

            DrawnAxesFixtures.Set(figure, part, path, 0);

            // A setting the renderer refuses refuses the same way whether anything was kept or not.
            Assert.Equal(OutcomeOf(() => FromScratch(figure)), OutcomeOf(figure.ToSvg));
        });
    }

    /// <summary>The picture a draw makes, or the kind of exception it throws.</summary>
    private static string OutcomeOf(Func<string> draw)
    {
        try
        {
            return draw();
        }
        catch (Exception ex)
        {
            return $"threw {ex.GetType().FullName}";
        }
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void AValueChangedInPlace_IsDrawnAsAFreshDrawDrawsIt(string type)
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(type, everyPropertySet: true));
        var series = DrawnSeriesFixtures.TheSeries(figure);

        foreach (var field in DataSnapshot.Plan(series.GetType()))
        {
            _ = figure.ToSvg();
            if (!InPlaceChange.Apply(field.GetValue(series))) continue;

            Assert.Equal(FromScratch(figure), figure.ToSvg());
        }
    }

    [Fact]
    public void AnAxesNothingChangedIn_KeepsItsMarkup_AndOneThatChanged_IsDrawnAgain()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: true));
        var kept = Settled(figure);

        figure.SubPlots[1].Title = "changed";
        string svg = figure.ToSvg();

        var now = figure.DrawnSvgAxes!;
        Assert.Same(kept[0], now[0]);
        Assert.NotSame(kept[1], now[1]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AFigureNothingChangedIn_IsDrawnFromWhatItKept_AndTheSame()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(BarSeries), everyPropertySet: true));
        Settle(figure);
        string first = figure.ToSvg();
        var kept = figure.DrawnSvgAxes!;

        string second = figure.ToSvg();

        Assert.Equal(first, second);
        Assert.Same(kept[0], figure.DrawnSvgAxes![0]);
        Assert.Same(kept[1], figure.DrawnSvgAxes![1]);
    }

    public static TheoryData<string, bool> ThemesAndLabels { get; } = new()
    {
        { "default", false }, { "default", true },
        { "matplotlib-v2", false }, { "matplotlib-v2", true },
        { "matplotlib-classic", false }, { "matplotlib-classic", true },
    };

    [Theory]
    [MemberData(nameof(ThemesAndLabels))]
    public void AFigureNothingChangedIn_HandsOutEveryAxes_UnderEveryTheme(string theme, bool mathLabels)
    {
        var x = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var y = x.Select(v => v * v / 50).ToArray();
        var look = theme.AsTheme();
        var builder = Plt.Create().WithTheme(look).WithSize(800, 600)
            .WithTitle(mathLabels ? @"$\alpha_i^2 + \beta_i$" : "plain");
        for (int i = 0; i < 4; i++)
            builder = builder.AddSubPlot(2, 2, i + 1, ax => ax
                .Plot(x, y, line => line.Label = mathLabels ? @"$\sqrt{x^2 + y^2}$" : "line")
                .WithTitle(mathLabels ? @"Latency ($\mu$s)" : "title")
                .SetXLabel(mathLabels ? @"$\frac{\Delta y}{\Delta x}$" : "x")
                .SetYLabel("y")
                .WithLegend());
        var figure = builder.Build();
        var kept = Settled(figure);

        string svg = figure.ToSvg();

        for (int i = 0; i < kept.Length; i++) Assert.Same(kept[i], figure.DrawnSvgAxes![i]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AnAxes_KeepsACopyOfItsData_FromItsSecondDrawUnderTheSameKey_AndIsHandedOutFromTheThird()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: true));

        _ = figure.ToSvg();
        var first = figure.DrawnSvgAxes!;
        Assert.False(first[0].KeptData);
        // Without a copy of the data it can never be handed out, so the markup is not held on to: only the key is.
        Assert.Null(first[0].Layer);

        _ = figure.ToSvg();
        var second = figure.DrawnSvgAxes!;
        Assert.NotSame(first[0], second[0]);
        Assert.True(second[0].KeptData);

        _ = figure.ToSvg();
        Assert.Same(second[0], figure.DrawnSvgAxes![0]);
    }

    [Fact]
    public void AnAxesThatChangesOnEveryDraw_NeverKeepsACopyOfItsData()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: true));
        for (int i = 0; i < 4; i++)
        {
            figure.SubPlots[0].Title = $"draw {i}";
            _ = figure.ToSvg();
            Assert.False(figure.DrawnSvgAxes![0].KeptData);
        }
    }

    [Fact]
    public void AnAxesThatSharesAScale_IsDrawnAgain_WhenTheAxesItSharesWithChanges()
    {
        var figure = new Figure();
        double[] y = [1, 2, 3];
        var left = figure.AddSubPlot(1, 2, 1);
        left.AddSeries(new LineSeries([1.0, 2, 3], y));
        var right = figure.AddSubPlot(1, 2, 2, sharex: left, sharey: left);
        right.AddSeries(new LineSeries([1.0, 2, 3], [2.0, 2, 2]));
        var kept = Settled(figure);

        y[2] = 30;   // the left data reaches higher: the shared Y range of the right axes grows with it
        string svg = figure.ToSvg();

        Assert.NotSame(kept[1], figure.DrawnSvgAxes![1]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void ALayoutThatMovesAnAxes_DrawsItAgain()
    {
        var figure = Plt.Create().TightLayout()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2, 3], [3.0, 2, 1]))
            .Build();
        var kept = Settled(figure);

        figure.SubPlots[0].YAxis.Label = "a label long enough to take room from the axes beside it";
        string svg = figure.ToSvg();

        Assert.NotSame(kept[0], figure.DrawnSvgAxes![0]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AZoomOnOneAxes_DrawsThatAxesAgain_AndKeepsTheOther()
    {
        // Two axes one above the other, the lower one zoomed along X: its tick labels change, their height does not, so the
        // layout leaves the upper axes where it was. (Side by side, the gap between two axes follows the tick labels of the
        // right one, so a zoom there can move the left one, which is then drawn again: AZoomThatMovesTheAxesBesideIt_...)
        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2, 3], [2.0, 1, 3]))
            .Build();
        var kept = Settled(figure);

        string svg = AfterApplying(figure, new ZoomEvent("c", 1, 1.5, 2.5, 0.8, 3.2));

        Assert.Same(kept[0], figure.DrawnSvgAxes![0]);
        Assert.NotSame(kept[1], figure.DrawnSvgAxes![1]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AZoomThatMovesTheAxesBesideIt_DrawsThatOneAgainToo()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: false));
        var kept = Settled(figure);

        // Y tick labels of six digits need more than the gap the theme leaves between the two plots (0.2 of the average axes
        // under the matplotlib themes), so the gap widens and the plot beside it narrows.
        string svg = AfterApplying(figure, new ZoomEvent("c", 1, 1.5, 2.5, 100000, 300000));

        Assert.NotSame(kept[0], figure.DrawnSvgAxes![0]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void TurningA3DAxes_DrawsItAgain_AndKeepsThePanelsBesideIt()
    {
        double[] g = [-1.0, 0, 1];
        var figure = Plt.Create().With3DRotation()
            .AddSubPlot(1, 2, 1, ax => ax.Surface(g, g, new double[,] { { 1, 2, 1 }, { 2, 3, 2 }, { 1, 2, 1 } }))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
            .Build();
        var kept = Settled(figure);

        string svg = AfterApplying(figure, new Rotate3DEvent("c", 0, 12, 4));

        Assert.NotSame(kept[0], figure.DrawnSvgAxes![0]);
        Assert.Same(kept[1], figure.DrawnSvgAxes![1]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AChangeToTheFigureItself_DrawsEveryAxesAgain()
    {
        var figure = WithASecondAxes(DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: false));
        var kept = Settled(figure);

        figure.Theme = Theme.Dark;
        string svg = figure.ToSvg();

        Assert.NotSame(kept[0], figure.DrawnSvgAxes![0]);
        Assert.NotSame(kept[1], figure.DrawnSvgAxes![1]);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void ASubplotAdded_DrawsTheNewOne_AndTheFigureAsAFreshDrawDoes()
    {
        var figure = DrawnSeriesFixtures.Build(nameof(LineSeries), everyPropertySet: false);
        Settle(figure);

        WithASecondAxes(figure);
        string svg = figure.ToSvg();

        Assert.Equal(2, figure.DrawnSvgAxes!.Length);
        Assert.Equal(FromScratch(figure), svg);
    }

    [Fact]
    public void AStreamAppendingAPoint_IsDrawn()
    {
        var figure = new Figure();
        var stream = figure.AddSubPlot(1, 2, 1).AddSeries(new MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries(16));
        stream.AppendPoint(1, 1);
        stream.AppendPoint(2, 3);
        figure.AddSubPlot(1, 2, 2).AddSeries(new LineSeries([1.0, 2], [2.0, 1]));
        Settle(figure);

        stream.AppendPoint(3, 2);

        Assert.Equal(FromScratch(figure), figure.ToSvg());
    }
}

