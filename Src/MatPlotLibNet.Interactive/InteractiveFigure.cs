// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Handle returned by <see cref="InteractiveExtensions.ShowAsync"/> for pushing updates to a displayed chart.</summary>
/// <remarks>The process' server keeps the figure so its page can be loaded again for as long as the handle lives; dispose the
/// handle when the chart is done with, and the server lets the figure go (its page is then no longer served).</remarks>
public sealed class InteractiveFigure : IDisposable
{
    private volatile bool _disposed;

    /// <summary>Gets the chart identifier used for SignalR subscription.</summary>
    public string ChartId { get; }

    /// <summary>Gets the figure being displayed.</summary>
    public Figure Figure { get; }

    internal InteractiveFigure(string chartId, Figure figure)
    {
        ChartId = chartId;
        Figure = figure;
    }

    /// <summary>Pushes the current state of the figure to the browser via SignalR.</summary>
    /// <exception cref="ObjectDisposedException">The handle was disposed.</exception>
    public async Task UpdateAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await ChartServer.Instance.UpdateFigureAsync(ChartId, Figure).ConfigureAwait(false);
    }

    /// <summary>Plays the frames of an <see cref="AnimationBuilder"/>, pushing each via SignalR: the builder is an animation of
    /// frame indexes, played as any other (<see cref="AnimateAsync{TState}(IAnimation{TState}, CancellationToken)"/>).</summary>
    /// <exception cref="ObjectDisposedException">The handle was disposed.</exception>
    public Task AnimateAsync(AnimationBuilder animation, CancellationToken ct = default) =>
        AnimateAsync<int>(animation, ct);

    /// <summary>Plays a generic stateful animation, pushing each frame via SignalR.</summary>
    /// <exception cref="ObjectDisposedException">The handle was disposed.</exception>
    public async Task AnimateAsync<TState>(IAnimation<TState> animation, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var controller = new AnimationController<TState>(animation, PublishFrame);
        await using (controller.ConfigureAwait(false))
        {
            await controller.PlayAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Creates an animation controller for manual playback control (pause/resume/stop).</summary>
    /// <exception cref="ObjectDisposedException">The handle was disposed.</exception>
    public AnimationController<TState> CreateController<TState>(IAnimation<TState> animation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(animation, PublishFrame);
    }

    /// <summary>Lets the process' server go of the figure: its page is no longer served, and the handle pushes nothing more. A
    /// second call does nothing.</summary>
    public void Dispose()
    {
        _disposed = true;
        ChartServer.Instance.ReleaseFigure(ChartId);
    }

    /// <summary>Pushes one frame; a controller made before the handle was disposed pushes nothing after, so the server is not
    /// handed the figure again.</summary>
    private Task PublishFrame(Figure frame, CancellationToken ct) =>
        _disposed ? Task.CompletedTask : ChartServer.Instance.UpdateFigureAsync(ChartId, frame, ct);
}
