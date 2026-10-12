// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>
/// WPF was the only one of the four native heads without a streaming control: Avalonia, MAUI and Uno each
/// ship one beside the ordinary chart, so a reader binds a <see cref="StreamingFigure"/> once and the chart
/// follows the data. Without it a WPF application had to fake a new figure — <c>Figure = null</c> and back —
/// because the <c>Figure</c> dependency property only raises its callback on a reference change. Reported as
/// discussion #20.
/// <para>These tests hold the three decisions the control makes: it subscribes to the figure it is given, it
/// lets go of the one it is given up, and it lets go when the control itself is unloaded. That last one is the
/// leak a council found in all three siblings — the figure outlives the control, so a control that never
/// detaches keeps being invalidated after it is gone.</para>
/// </summary>
public class MplStreamingChartControlTests
{
    /// <summary>Runs the body on an STA thread, because a WPF <see cref="FrameworkElement"/> cannot be
    /// constructed anywhere else: its keyboard-navigation service demands the single-threaded apartment.
    /// The dispatcher it creates there never pumps, which is exactly right — these tests are about what the
    /// control subscribes to, not about what it paints.</summary>
    private static void OnStaThread(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>How many handlers the figure is holding — the only way to see a leak from outside.</summary>
    private static int SubscriberCount(StreamingFigure sf)
    {
        var field = typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (field.GetValue(sf) as Delegate)?.GetInvocationList().Length ?? 0;
    }

    private static StreamingFigure NewStreamingFigure()
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        return new StreamingFigure(figure);
    }

    [Fact]
    public void StreamingFigureProperty_Set_SubscribesToRenderRequested()
    {
        OnStaThread(() =>
        {            using var sf = NewStreamingFigure();

            var control = new MplStreamingChartControl { StreamingFigure = sf };

            Assert.Same(sf, control.StreamingFigure);
            Assert.Equal(1, SubscriberCount(sf));    });
    }

    [Fact]
    public void StreamingFigureChange_UnsubscribesTheOldFigure()
    {
        OnStaThread(() =>
        {            using var first = NewStreamingFigure();
            using var second = NewStreamingFigure();
            var control = new MplStreamingChartControl { StreamingFigure = first };

            control.StreamingFigure = second;

            Assert.Equal(0, SubscriberCount(first));
            Assert.Equal(1, SubscriberCount(second));    });
    }

    [Fact]
    public void StreamingFigureClearedToNull_LetsGoOfTheFigure()
    {
        OnStaThread(() =>
        {            using var sf = NewStreamingFigure();
            var control = new MplStreamingChartControl { StreamingFigure = sf };

            control.StreamingFigure = null;

            Assert.Equal(0, SubscriberCount(sf));    });
    }

    [Fact]
    public void Unloaded_LetsGoEvenWhenTheFigureNeverChanges()
    {
        OnStaThread(() =>
        {            // The leak that matters: the figure outlives the control, so a control torn down with its figure
            // still attached would keep receiving render requests — and keep the dead control alive.
            using var sf = NewStreamingFigure();
            var control = new MplStreamingChartControl { StreamingFigure = sf };

            control.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            Assert.Equal(0, SubscriberCount(sf));    });
    }

    [Fact]
    public void AppendingData_ReachesTheControlWithoutHandingItANewFigure()
    {
        OnStaThread(() =>
        {            // The whole point of the control. The ordinary MplChartControl redraws only when the Figure
            // *reference* changes; here the same figure keeps its identity and the data alone moves the chart.
            using var sf = NewStreamingFigure();
            var series = (StreamingLineSeries)sf.Figure.SubPlots[0].Series[0];
            var control = new MplStreamingChartControl { StreamingFigure = sf };

            int invalidations = 0;
            var field = typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested),
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var handler = (Action)field.GetValue(sf)!;

            sf.RenderRequested += () => Interlocked.Increment(ref invalidations);
            series.AppendPoint(5.0, 42.0);
            Thread.Sleep(400);

            Assert.NotNull(handler);
            Assert.True(Volatile.Read(ref invalidations) >= 1,
                "appending a point must reach the subscribers of the figure the control is bound to");    });
    }
}
