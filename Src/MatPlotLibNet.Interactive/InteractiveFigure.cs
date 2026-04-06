// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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

    /// <summary>Plays an animation by pushing each frame to the browser with the configured interval.</summary>
    /// <param name="animation">The animation to play.</param>
    /// <param name="ct">Cancellation token to stop the animation.</param>
    public async Task AnimateAsync(AnimationBuilder animation, CancellationToken ct = default)
    {
        do
        {
            for (int i = 0; i < animation.FrameCount && !ct.IsCancellationRequested; i++)
            {
                var frame = animation.GenerateFrame(i);
                await ChartServer.Instance.UpdateFigureAsync(ChartId, frame);
                await Task.Delay(animation.Interval, ct);
            }
        }
        while (animation.Loop && !ct.IsCancellationRequested);
    }
}
