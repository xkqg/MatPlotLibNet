// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The room a layout keeps past an edge is measured from the plot's edge to the far side of what is drawn there, so a spine
/// moved off its edge takes its tick labels' room with it: matplotlib's tight layout reads the axes' tight bbox, which holds the tick
/// labels wherever their spine stands. On all four edges: the primary's labels left and below, the mirrored ones right and above.
/// matplotlib 3.11.2, read with python (600 x 400 at 100 dpi, <c>ax.plot([0, 10], [0, 10])</c>,
/// <c>ax.spines[side].set_position(('axes', -0.1))</c>, or 1.1 on the right and the top, <c>fig.tight_layout()</c> repeated until
/// it stands): the margin widens by a tenth of the side the plot is drawn at, to 1e-6 on every edge and in both styles (default: left
/// 42.722 to 92.020, the plot 492.980 wide; bottom 38.611 to 70.101, the plot 314.899 high; right 42.722 to 89.500; top 38.611 to
/// 67.955). One <c>tight_layout()</c> alone reads the spine at the plot before the layout (left 89.222, a tenth of 465) and a second
/// call moves it again (92.300), so the layout that stands is the one a layout settles at. A spine moved in keeps the room of what
/// still stands past the edge: at <c>('axes', 0.05)</c> the bottom margin is the pad and the label band less a twentieth of the plot's
/// height (15 + 23.611 - 18.231 = 20.380).
/// <para>Each row holds a figure with a moved spine to the figure whose spine stands on its edge and whose tick labels stand as far
/// out by a wider tick pad (the spine's displacement at the plot the moved figure is drawn at): the labels then stand in the same place,
/// so the room kept for them is the same, whatever floor a theme keeps under it. The resting margins are 0, so the room is what is
/// drawn.</para></summary>
public class MovedSpineRoomTests
{
    private const double Share = 0.1;

    /// <summary>A line on one axes, the labels drawn on <paramref name="edge"/> (mirrored on the right and the top), that spine moved
    /// <paramref name="outward"/> of the plot's side out past its edge (in for a negative share), its axis' tick pad widened by
    /// <paramref name="widerPad"/>.</summary>
    private static Figure Line(string edge, Theme theme, double outward = 0, double widerPad = 0, bool tight = true, string? title = null)
    {
        var builder = Plt.Create().WithSize(600, 400).WithTheme(theme);
        if (tight) builder = builder.WithSubPlotSpacing(s => s with { MarginLeft = 0, MarginRight = 0, MarginTop = 0, MarginBottom = 0 }).TightLayout();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                if (edge == "Right") ax.WithYTicksMirrored();
                if (edge == "Top") ax.WithXTicksMirrored();
                if (title is not null) ax.WithTitle(title);
            })
            .Build();
        var axes = figure.SubPlots[0];
        if (widerPad != 0)
        {
            var axis = edge is "Left" or "Right" ? axes.YAxis : axes.XAxis;
            axis.MajorTicks = axis.MajorTicks with { Pad = axis.MajorTicksUnder(theme).Pad + widerPad };
        }
        if (outward == 0) return figure;
        axes.Spines = edge switch
        {
            "Left" => axes.Spines with { Left = axes.Spines.Left with { Position = SpinePosition.Axes, PositionValue = -outward } },
            "Bottom" => axes.Spines with { Bottom = axes.Spines.Bottom with { Position = SpinePosition.Axes, PositionValue = -outward } },
            "Right" => axes.Spines with { Right = axes.Spines.Right with { Position = SpinePosition.Axes, PositionValue = 1 + outward } },
            _ => axes.Spines with { Top = axes.Spines.Top with { Position = SpinePosition.Axes, PositionValue = 1 + outward } },
        };
        return figure;
    }

    private static double Margin(string edge, Rect plot) => edge switch
    {
        "Left" => plot.X,
        "Bottom" => 400 - plot.Bottom,
        "Right" => 600 - plot.Right,
        _ => plot.Y,
    };

    private static double Side(string edge, Rect plot) => edge is "Left" or "Right" ? plot.Width : plot.Height;

    /// <summary>The margin on <paramref name="edge"/> of the figure whose spine moved <paramref name="outward"/>, beside the margin of
    /// the figure whose labels stand there by a wider pad.</summary>
    private static void AssertRoomAsDrawn(string edge, Theme theme, double outward, bool tight = true)
    {
        var moved = Line(edge, theme, outward, tight: tight).FirstPlotArea();
        var padded = Line(edge, theme, widerPad: outward * Side(edge, moved), tight: tight).FirstPlotArea();

        Assert.NotEqual(Margin(edge, Line(edge, theme, tight: tight).FirstPlotArea()), Margin(edge, moved), 1);
        Assert.Equal(Margin(edge, padded), Margin(edge, moved), ConstrainedLayoutEngine.SettledWithin);
    }

    [Theory]
    [InlineData("Left", "Default")]
    [InlineData("Bottom", "Default")]
    [InlineData("Right", "Default")]
    [InlineData("Top", "Default")]
    [InlineData("Left", "MatplotlibV2")]
    [InlineData("Bottom", "MatplotlibV2")]
    [InlineData("Right", "MatplotlibV2")]
    [InlineData("Top", "MatplotlibV2")]
    [InlineData("Left", "MatplotlibClassic")]
    [InlineData("Bottom", "MatplotlibClassic")]
    [InlineData("Right", "MatplotlibClassic")]
    [InlineData("Top", "MatplotlibClassic")]
    public void ASpineMovedOutward_KeepsTheRoomItsLabelsTakeWhereTheyAreDrawn(string edge, string theme) =>
        AssertRoomAsDrawn(edge, theme.AsTheme(), Share);

    /// <summary>Moved in, a spine takes its labels in with it, and only what still stands past the edge is kept room for. Not below
    /// the plot: the floor the tight layout keeps under drawn X tick labels holds there, labels moved in or not.</summary>
    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Top")]
    public void ASpineMovedIn_KeepsOnlyWhatStillStandsPastTheEdge(string edge) =>
        AssertRoomAsDrawn(edge, Theme.MatplotlibV2, -0.02);

    /// <summary>The default layout keeps room for what is drawn past the plot by the same rule, where the theme's margin does not hold it.</summary>
    [Theory]
    [InlineData("Left")]
    [InlineData("Bottom")]
    public void TheDefaultLayout_KeepsTheRoomOfAMovedSpinesLabels(string edge) =>
        AssertRoomAsDrawn(edge, Theme.Default, 0.3, tight: false);

    /// <summary>The axes title rises over what the moved top spine took up with it, as the layout keeps room for it there: the
    /// title's baseline stands where it stands over labels that a wider pad put in the same place.</summary>
    [Fact]
    public void TheTitle_RisesOverTheLabelsOfAMovedTopSpine()
    {
        static DrawnTitle Drawn(Figure figure)
        {
            var drawn = new RecordingRenderContext();
            new ChartRenderer().Render(figure, drawn);
            double baseline = drawn.Of<DrawTextCall>()
                .Where(a => a.Text == "Title").Select(a => (double)(a.Position).Y).Single();
            return new DrawnTitle(baseline, new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext()).PlotAreas[0]);
        }

        var moved = Drawn(Line("Top", Theme.Default, Share, title: "Title"));
        var padded = Drawn(Line("Top", Theme.Default, widerPad: Share * moved.Plot.Height, title: "Title"));

        Assert.Equal(padded.Plot.Y - padded.Baseline, moved.Plot.Y - moved.Baseline, ConstrainedLayoutEngine.SettledWithin);
        Assert.Equal(padded.Plot.Y, moved.Plot.Y, ConstrainedLayoutEngine.SettledWithin);
    }

    /// <summary>Where the axes title's baseline stands in a drawn figure, and the plot it stands over, laid out with the context it is
    /// drawn with.</summary>
    private readonly record struct DrawnTitle(double Baseline, Rect Plot);
}
