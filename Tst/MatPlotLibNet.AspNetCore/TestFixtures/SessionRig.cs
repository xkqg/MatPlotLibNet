// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A session on a one-line figure, with a recording publisher, a recording caller publisher and a way to wait until the
/// reader has dealt with everything written so far: a data-cursor event with a handler that signals. It goes down the notification
/// path, so it neither changes the figure nor causes a publish, and events are taken in order, so everything before it is done once it
/// is reached.</summary>
internal sealed class SessionRig
{
    private TaskCompletionSource _reached = new();

    /// <summary>Starts the session.</summary>
    /// <param name="handler">The handler of every event, or <see langword="null"/> for none.</param>
    /// <param name="configure">What else the test adds to the options, or <see langword="null"/> for nothing.</param>
    public SessionRig(IChartEventHandler? handler = null, Action<ChartSessionOptions>? configure = null)
    {
        var options = new ChartSessionOptions().OnDataCursor(async _ =>
        {
            await Task.Yield();
            _reached.TrySetResult();
        });
        if (handler is not null)
        {
            options.Handle(handler);
        }

        configure?.Invoke(options);
        Figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        Session = new ChartSession("c1", Figure, Publisher, Callers, options);
    }

    /// <summary>The figure the session serves.</summary>
    public Figure Figure { get; }

    /// <summary>The figure's only subplot.</summary>
    public Axes Axes => Figure.SubPlots[0];

    /// <summary>What the session published.</summary>
    public RecordingChartPublisher Publisher { get; } = new();

    /// <summary>The tooltips the session sent to callers.</summary>
    public RecordingCallerPublisher Callers { get; } = new();

    /// <summary>The session.</summary>
    public ChartSession Session { get; }

    /// <summary>Hands the session an event.</summary>
    /// <param name="evt">The event.</param>
    public void Send(FigureInteractionEvent evt) => Assert.True(Session.Events.Writer.TryWrite(evt));

    /// <summary>Returns once the reader has dealt with everything sent so far.</summary>
    /// <returns>A task that ends when the reader has reached the marker event.</returns>
    public async Task DrainedAsync()
    {
        _reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Send(new DataCursorEvent("c1", 0, new PinnedAnnotation("marker", 0, 0, 0, 0, 0)));
        await _reached.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
