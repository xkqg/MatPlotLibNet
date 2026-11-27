// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>What the chart control says of itself is where its figure comes from: the figure it is handed, or the frames of the
/// animation source it follows. Following itself (let go of the one before, listen to the one now) is the core's
/// <c>AnimationSourceFollowerTests</c>; a frame is posted to the UI thread, which a test has no dispatcher for.</summary>
public class MplChartControlTests
{
    [Fact]
    public void TheFigureAndTheAnimationSource_ReadBackAsGiven_AndTheSourceFollowedIsTheOneSetLast()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        AnimationSourceReadback.Verify(new MplChartControl { Figure = figure }, figure, static c => c.Figure, static c => c.AnimationSource,
            static (c, source) => c.AnimationSource = source);
    }
}
