// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A scatter leaves room for its markers under the classic theme. matplotlib 3.11.2, <c>Axes.scatter</c>: "ensure there
/// are margins to allow for the finite size of the symbols. In v2.x, margins are present by default, so we disable this
/// scatter-specific override": <c>if mpl.rcParams['_internal.classic_mode']: if self._xmargin &lt; 0.05 and x.size &gt; 0:
/// self.set_xmargin(0.05)</c>, and the same for Y. <c>Axes3D.scatter</c> calls it, so a 3-D scatter's X and Y are raised too.
/// The margin is the axes', so every series of an axes that holds a scatter is padded by it.</summary>
public sealed class ScatterMarginTests
{
    private static Axes With(params ISeries[] series)
    {
        var axes = new Axes();
        foreach (var s in series) axes.AddSeries(s);
        return axes;
    }

    private static Axes ThreeD(params ISeries[] series)
    {
        var axes = With(series);
        axes.CoordinateSystem = CoordinateSystem.ThreeD;
        return axes;
    }

    /// <summary><c>ax.scatter([1, 2, 3], [1, 5.1, 3])</c> under the classic style draws X from 0.5 to 3.5 and Y from 0 to 6: the 5 %
    /// margin first (0.9 .. 3.1, 0.795 .. 5.305), then the round numbers. Without the margin the ends would be 1 .. 3 and 1 .. 5.5.
    /// A line beside it is padded by the same margin (the margin is the axes').</summary>
    [Fact]
    public void Classic_AScatterPadsItsAxesByFivePercent_BeforeTheRoundNumbers()
    {
        var alone = With(new ScatterSeries([1, 2, 3], [1, 5.1, 3])).ComputeDataRanges(Theme.MatplotlibClassic);
        var withLine = With(new LineSeries([1, 2, 3], [1, 5.1, 3]), new ScatterSeries([1, 2, 3], [1, 5.1, 3]))
            .ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(0.5, 3.5).AssertCloseTo(new Range1D(alone.XMin, alone.XMax), 1e-9);
        new Range1D(0, 6).AssertCloseTo(new Range1D(alone.YMin, alone.YMax), 1e-9);
        new Range1D(0.5, 3.5).AssertCloseTo(new Range1D(withLine.XMin, withLine.XMax), 1e-9);
        new Range1D(0, 6).AssertCloseTo(new Range1D(withLine.YMin, withLine.YMax), 1e-9);
    }

    /// <summary>seaborn 0.13.2 draws a strip plot and a swarm plot with <c>ax.scatter</c>, so the classic margin of 5 % pads them too:
    /// <c>sns.stripplot(data=[[1, 5.1, 3]], jitter=False)</c> and <c>sns.swarmplot(data=[[1, 5.1, 3]])</c> under the classic style read
    /// <c>get_ylim()</c> = (0.0, 6.0), as <c>ax.scatter</c> of the same numbers does; under the default style (0.795, 5.305).
    /// A group with no dot raises nothing.</summary>
    [Fact]
    public void Classic_AStripAndASwarmPadTheirAxesByFivePercent_BeforeTheRoundNumbers()
    {
        double[][] data = [[1, 5.1, 3]];
        var strip = With(new StripplotSeries(data)).ComputeDataRanges(Theme.MatplotlibClassic);
        var swarm = With(new SwarmplotSeries(data)).ComputeDataRanges(Theme.MatplotlibClassic);
        var v2 = With(new SwarmplotSeries(data)).ComputeDataRanges(Theme.MatplotlibV2);

        new Range1D(0, 6).AssertCloseTo(new Range1D(strip.YMin, strip.YMax), 1e-9);
        new Range1D(0, 6).AssertCloseTo(new Range1D(swarm.YMin, swarm.YMax), 1e-9);
        new Range1D(0.795, 5.305).AssertCloseTo(new Range1D(v2.YMin, v2.YMax), 1e-9);
        Assert.Equal(0, With(new StripplotSeries([[]]), new SwarmplotSeries([])).ScatterMarginIn(Theme.MatplotlibClassic));
        Assert.Equal(0.05, With(new StripplotSeries([[], [2.0]])).ScatterMarginIn(Theme.MatplotlibClassic));
    }

    /// <summary>A scatter with no points raises nothing (<c>x.size &gt; 0</c>): beside a line over 1 .. 3 the classic X stays 1 .. 3. A
    /// margin set on the axis wins, and a line alone keeps the classic margin of 0.</summary>
    [Fact]
    public void Classic_AnEmptyScatter_ASetMarginAndALineAlone_RaiseNothing()
    {
        var empty = With(new ScatterSeries([], []), new LineSeries([1, 2, 3], [1, 5.1, 3])).ComputeDataRanges(Theme.MatplotlibClassic);
        var set = With(new ScatterSeries([1, 2, 3], [1, 5.1, 3]));
        set.XAxis.Margin = 0;
        var line = With(new LineSeries([1, 2, 3], [1, 5.1, 3])).ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(1, 3).AssertCloseTo(new Range1D(empty.XMin, empty.XMax), 1e-9);
        new Range1D(1, 3).AssertCloseTo(new Range1D(set.ComputeDataRanges(Theme.MatplotlibClassic).XMin, set.ComputeDataRanges(Theme.MatplotlibClassic).XMax), 1e-9);
        new Range1D(1, 3).AssertCloseTo(new Range1D(line.XMin, line.XMax), 1e-9);
    }

    /// <summary>The default style raises no margin for a scatter, its margins already being 5 %; a theme of the library's own keeps
    /// its margin as well.</summary>
    [Fact]
    public void V2AndTheLibrarysThemes_RaiseNoMargin()
    {
        var v2 = With(new ScatterSeries([1, 2, 3], [1, 5.1, 3])).ComputeDataRanges(Theme.MatplotlibV2);
        var noMargin = new Theme("no-margin", Theme.Default) { AxisXMargin = 0, AxisYMargin = 0 };
        var own = With(new ScatterSeries([1, 2, 3], [1, 5.1, 3])).ComputeDataRanges(noMargin);

        new Range1D(0.9, 3.1).AssertCloseTo(new Range1D(v2.XMin, v2.XMax), 1e-9);
        new Range1D(1, 3).AssertCloseTo(new Range1D(own.XMin, own.XMax), 1e-9);
    }

    /// <summary><c>ax.scatter([0, 1], [0, 2], [0, 1])</c> on a 3-D axes under the classic style draws X from -0.2292 to 1.2292 and Y
    /// from -0.5625 to 2.5625: the 5 % margin (-0.05 .. 1.05, -0.1 .. 2.1), the round numbers (-0.2 .. 1.2, -0.5 .. 2.5), then the
    /// view margin of a forty-eighth.</summary>
    [Fact]
    public void Classic_AThreeDScatterPadsXAndYByFivePercent()
    {
        var cube = ThreeD(new Scatter3DSeries(new double[] { 0, 1 }, new double[] { 0, 2 }, new double[] { 0, 1 }))
            .ComputeCubeRange(Theme.MatplotlibClassic);

        new Range1D(-0.2 - 1.4 / 48, 1.2 + 1.4 / 48).AssertCloseTo(new Range1D(cube.X.Lo, cube.X.Hi), 1e-9);
        new Range1D(-0.5 - 3.0 / 48, 2.5 + 3.0 / 48).AssertCloseTo(new Range1D(cube.Y.Lo, cube.Y.Hi), 1e-9);
    }

    /// <summary>A 3-D scatter with no points raises no margin either (<c>Axes3D.scatter</c>: <c>xs.size &gt; 0</c>): beside a line over
    /// 0 .. 1 the classic X keeps the view margin alone, and so does Z.</summary>
    [Fact]
    public void Classic_AnEmptyThreeDScatter_RaisesNothing()
    {
        var cube = ThreeD(new Scatter3DSeries(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>()),
                new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }))
            .ComputeCubeRange(Theme.MatplotlibClassic);

        new Range1D(-1.0 / 48, 1 + 1.0 / 48).AssertCloseTo(new Range1D(cube.X.Lo, cube.X.Hi), 1e-9);
        new Range1D(-1.0 / 48, 1 + 1.0 / 48).AssertCloseTo(new Range1D(cube.Z.Lo, cube.Z.Hi), 1e-9);
    }

    /// <summary>The theme carries the margin: 5 % under classic, none under the other themes; a derived theme and the JSON keep it.</summary>
    [Fact]
    public void TheThemeCarriesTheScatterMargin()
    {
        Assert.Equal(0.05, Theme.MatplotlibClassic.Looks.Range.ScatterMargin);
        Assert.Null(Theme.MatplotlibV2.Looks.Range.ScatterMargin);
        Assert.Null(Theme.Default.Looks.Range.ScatterMargin);
        Assert.Equal(0.05, Theme.MatplotlibClassic.Cloned().Looks.Range.ScatterMargin);
        Assert.Equal(0.05, Theme.MatplotlibClassic.ThroughDto().Looks.Range.ScatterMargin);
        Assert.Null(ThemeDto.From(Theme.MatplotlibV2).ScatterMargin);
    }

    /// <summary>Every kind of series matplotlib draws with <c>scatter</c> raises the classic margin while it holds a point and raises
    /// nothing while it holds none: bubbles, a polar scatter and the points of a residual plot, as a scatter does.</summary>
    [Theory]
    [InlineData("bubble")]
    [InlineData("polar")]
    [InlineData("residual")]
    public void Classic_AScatterOfAnyKind_RaisesTheMarginOnlyWithAPoint(string kind)
    {
        ISeries Of(double[] values) => kind switch
        {
            "bubble" => new BubbleSeries(values, values, values),
            "polar" => new PolarScatterSeries(values, values),
            _ => new ResidualSeries(values, values),
        };

        Assert.Equal(0.05, With(Of([1.0, 2.0])).ScatterMarginIn(Theme.MatplotlibClassic));
        Assert.Equal(0, With(Of([])).ScatterMarginIn(Theme.MatplotlibClassic));
    }
}
