// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>What the streaming control says of itself is where its figure comes from: it follows the figure it is bound to, lets go
/// of the one it is taken off of, and when it leaves the visual tree lets go of the last one too (the figure outlives the control and
/// would otherwise keep asking a dead one to draw) and of what its painter recorded.</summary>
public class MplStreamingChartControlTests
{
    [Fact]
    public void TheStreamingControl_ReadsBackWhatItWasGiven_AndLetsGoOfAFigureItIsTakenOffOf_AndOfTheLastWhenItLeaves()
    {
        using var first = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        using var second = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        var control = new MplStreamingChartControl();
        Assert.Null(control.Shown);

        control.StreamingFigure = first;
        Assert.Same(first, control.StreamingFigure);
        control.StreamingFigure = second;

        Assert.Same(second, control.StreamingFigure);
        Assert.Equal(0, Listeners(first));
        Assert.Equal(1, Listeners(second));

        control.Release();
        Assert.Equal(0, Listeners(second));
    }

    private static int Listeners(StreamingFigure figure) =>
        ((typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested), BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(figure) as Delegate)?.GetInvocationList().Length) ?? 0;
}
