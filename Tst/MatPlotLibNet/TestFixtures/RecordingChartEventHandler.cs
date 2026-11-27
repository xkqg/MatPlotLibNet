// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests;

/// <summary>The chart event handler a test hands a host or a session: it keeps every event it is told, and can be made to behave the
/// way a handler written by an application might, so a test sees what the host does about it.</summary>
/// <param name="into">The list the events are kept in, when the test holds one of its own; left out, the handler keeps its own.</param>
internal sealed class RecordingChartEventHandler(List<FigureInteractionEvent>? into = null) : IChartEventHandler
{
    /// <summary>The events the handler was told, in the order it was told them.</summary>
    public List<FigureInteractionEvent> Events { get; } = into ?? [];

    /// <summary>The context each event came with (the viewer it came from), in the order of <see cref="Events"/>: the nth context belongs to the nth event.</summary>
    public List<ChartEventContext> Contexts { get; } = [];

    /// <summary>Each event the handler was told, with the context it came with.</summary>
    public IReadOnlyList<HandledEvent> Handled
    {
        get
        {
            lock (Events)
            {
                return [.. Events.Zip(Contexts, (evt, context) => new HandledEvent(evt, context))];
            }
        }
    }

    /// <summary>The type name of each event the handler was told, in order.</summary>
    public IReadOnlyList<string> Names
    {
        get
        {
            lock (Events)
            {
                return [.. Events.Select(e => e.GetType().Name)];
            }
        }
    }

    /// <summary>The failure the handler's task ends with, after it has kept the event; <see langword="null"/> for none.</summary>
    public Exception? Throw { get; init; }

    /// <summary>The failure the handler throws before it returns a task at all; <see langword="null"/> for none. It keeps nothing.</summary>
    public Exception? ThrowAtOnce { get; init; }

    /// <summary>The events the failures above are meant for; <see langword="null"/> for every event. Another event is kept and ends well.</summary>
    public Func<FigureInteractionEvent, bool>? ThrowWhen { get; init; }

    /// <summary>Whether the handler does real work: it awaits once before it keeps the event, so a host that does not wait for the task sees nothing.</summary>
    public bool Yields { get; init; }

    /// <summary>What the handler does while it is handling an event, after it has kept it: a test reads the state of the chart there.</summary>
    public Action? WhileHandling { get; init; }

    /// <summary>The task the handler's own task follows, so it finishes when this one does; <see langword="null"/> for a handler that finishes at once.</summary>
    public Task? Finishes { get; init; }

    /// <inheritdoc />
    public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
    {
        bool fails = ThrowWhen?.Invoke(evt) ?? true;
        if (fails && ThrowAtOnce is not null)
        {
            throw ThrowAtOnce;
        }

        return Yields ? KeepAfterYieldAsync(evt, context, fails) : Keep(evt, context, fails);
    }

    /// <summary>Returns once the handler has been told at least <paramref name="count"/> events.</summary>
    /// <param name="count">How many events are waited for.</param>
    /// <param name="within">The longest the wait lasts; left out, five seconds.</param>
    /// <returns>A task that ends when the count is reached.</returns>
    /// <exception cref="TimeoutException">Fewer events came within <paramref name="within"/>.</exception>
    public Task WaitForAsync(int count, TimeSpan? within = null) =>
        Eventually.HoldsAsync(() =>
        {
            lock (Events)
            {
                return Events.Count >= count;
            }
        }, within);

    private async ValueTask KeepAfterYieldAsync(FigureInteractionEvent evt, ChartEventContext context, bool fails)
    {
        await Task.Yield();
        await Keep(evt, context, fails);
    }

    private ValueTask Keep(FigureInteractionEvent evt, ChartEventContext context, bool fails)
    {
        lock (Events)
        {
            Events.Add(evt);
            Contexts.Add(context);
        }

        WhileHandling?.Invoke();
        if (Finishes is not null)
        {
            return new ValueTask(Finishes);
        }

        return Throw is null || !fails ? default : ValueTask.FromException(Throw);
    }
}

/// <summary>An event a <see cref="RecordingChartEventHandler"/> was told, with the context it came with.</summary>
/// <param name="Event">The event.</param>
/// <param name="Context">The viewer it came from.</param>
internal readonly record struct HandledEvent(FigureInteractionEvent Event, ChartEventContext Context);
