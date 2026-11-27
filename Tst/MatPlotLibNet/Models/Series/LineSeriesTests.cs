// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="LineSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class LineSeriesTests
{
    /// <summary>A marker on every N-th point is a step of at least one point: a step of none (matplotlib refuses a slice step of
    /// zero) or below none is refused where it is given, and the line keeps the step it had.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MarkEvery_BelowOne_IsRefusedByName(int step)
    {
        var series = new LineSeries([1.0, 2, 3], [2.0, 4, 3]) { Marker = MarkerStyle.Circle, MarkEvery = 2 };

        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => series.MarkEvery = step);

        Assert.Equal(nameof(LineSeries.MarkEvery), refused.ParamName);
        Assert.Equal(2, series.MarkEvery);
    }

    /// <summary>A budget of points given and then taken back draws every point again, as a line never given one does.</summary>
    [Fact]
    public void ABudgetTakenBack_DrawsEveryPointAgain()
    {
        double[] x = [.. Enumerable.Range(0, 50).Select(i => (double)i)];
        double[] y = [.. x.Select(v => v % 7)];
        static string Drawn(LineSeries line) => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(line)).ToSvg();
        var takenBack = new LineSeries(x, y) { MaxDisplayPoints = 10 };
        takenBack.MaxDisplayPoints = null;

        Assert.Equal(Drawn(new LineSeries(x, y)), Drawn(takenBack));
    }
}
