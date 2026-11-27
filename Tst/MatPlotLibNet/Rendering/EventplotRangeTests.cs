// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What an eventplot puts into the automatic range. matplotlib 3.11.2, <c>Axes.eventplot</c>: <c>minline =
/// (lineoffsets - linelengths).min()</c>, <c>maxline = (lineoffsets + linelengths).max()</c>, then <c>update_datalim(corners)</c>
/// and no sticky edge, so the rows reach a whole line length past their offsets and the margin pads the events. Measured,
/// <c>ax.eventplot([[1, 2], [3], [2.5], [4]])</c>: the default style X 0.85 .. 4.15, Y -1.25 .. 4.25; the classic style X 1 .. 4,
/// Y -1 .. 4.</summary>
public sealed class EventplotRangeTests
{
    private static DataRange Range(Theme theme)
    {
        var axes = new Axes();
        axes.AddSeries(new EventplotSeries([[1.0, 2], [3.0], [2.5], [4.0]]));
        return axes.ComputeDataRanges(theme);
    }

    /// <summary>The default style reaches a line length past the rows and pads the events; the classic style rounds the same extent.</summary>
    [Theory]
    [InlineData("v2", 0.85, 4.15, -1.25, 4.25)]
    [InlineData("classic", 1, 4, -1, 4)]
    public void TheRangeReachesALineLengthPastTheRows_AndIsPaddedOrRoundedByTheStyle(string theme, double xMin, double xMax, double yMin, double yMax)
    {
        var range = Range(theme.AsTheme());

        Assert.Equal(xMin, range.XMin, 9);
        Assert.Equal(xMax, range.XMax, 9);
        Assert.Equal(yMin, range.YMin, 9);
        Assert.Equal(yMax, range.YMax, 9);
    }
}
