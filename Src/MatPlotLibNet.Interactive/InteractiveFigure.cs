// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Handle returned by <see cref="InteractiveExtensions.Show"/> for pushing updates to a displayed chart.</summary>
public sealed class InteractiveFigure
{
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
    public async Task UpdateAsync()
    {
        await ChartServer.Instance.UpdateFigureAsync(ChartId, Figure);
    }

    /// <summary>Plays an animation using the legacy AnimationBuilder (backward compatible).</summary>
    public async Task AnimateAsync(AnimationBuilder animation, CancellationToken ct = default)
    {
        var adapter = new LegacyAnimationAdapter(animation);
        await using var controller = new AnimationController<int>(adapter, PublishFrame);
        await controller.PlayAsync(ct);
    }

    /// <summary>Plays a generic stateful animation, pushing each frame via SignalR.</summary>
    public async Task AnimateAsync<TState>(IAnimation<TState> animation, CancellationToken ct = default)
    {
        await using var controller = new AnimationController<TState>(animation, PublishFrame);
        await controller.PlayAsync(ct);
    }

    /// <summary>Creates an animation controller for manual playback control (pause/resume/stop).</summary>
    public AnimationController<TState> CreateController<TState>(IAnimation<TState> animation) =>
        new(animation, PublishFrame);

    private Task PublishFrame(Figure frame, CancellationToken ct) =>
        ChartServer.Instance.UpdateFigureAsync(ChartId, frame);
}
