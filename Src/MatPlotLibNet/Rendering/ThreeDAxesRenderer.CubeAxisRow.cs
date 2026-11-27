// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

public sealed partial class ThreeDAxesRenderer
{
    /// <summary>One axis of a 3-D cube as one draw sees it: its major tick values, worked out once, the edge they run along, that
    /// edge on the screen and the way out of the cube from it, and the ONE rule for where a tick of the axis stands
    /// (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Holds"/>). The grid on the walls, the tick marks and labels, and the axis title with the room it keeps from
    /// the labels all read it, so none of them stands where the others show nothing, and all of them leave the cube the same
    /// way.</summary>
    internal readonly record struct CubeAxisRow
    {
        /// <summary>The room between the tick-label band and the axis title, in pixels.</summary>
        internal const double TitleGapPx = 8;

        /// <summary>The extra distance a 3-D tick label stands out from its mark, in pixels, so the labels clear the bars and
        /// surfaces drawn inside the cube; matplotlib keeps its X and Y labels in the margin below the cube in the same way.</summary>
        internal const double ThreeDExtraPad = 14;

        /// <summary>Which axis of the cube this is.</summary>
        internal CubeAxis Axis { get; init; }

        /// <summary>The values the axis' locator hands back. The uniform label format is built from all of them, as matplotlib
        /// formats every value its locator gives before it keeps the ones in view.</summary>
        internal double[] Ticks { get; init; }

        private readonly double[]? _gridTicks;

        /// <summary>The values the grid lines on the walls stand at: the major ticks, and under a theme whose grid line is a tick's
        /// (<see cref="AxisTickLook.MinorsByLocator"/>, the matplotlib themes) the minor ones too when the axis has its minor ticks
        /// on, as <c>axis3d.Axis.draw_grid</c> draws a line for every tick in view, major or minor. The row's own <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Ticks"/>
        /// when it names no more; the values past the edge are left out where the lines are drawn (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.HoldsGridLine"/>).</summary>
        internal double[] GridTicks { get => _gridTicks ?? Ticks; init => _gridTicks = value; }

        /// <summary>Whether a wall grid line stands on an end of the edge too: where the cube draws no edge of its own for it to coincide with,
        /// which is under the themes that bound the box by one axis line per axis (<see cref="CubeLook.AxisLineWidth"/>, the matplotlib
        /// ones), as <c>axis3d.Axis.draw_grid</c> draws a line for every tick in view, the two at the ends included.</summary>
        internal bool GridLinesOnEnds { get; init; }

        /// <summary>The range the axis' edge runs over.</summary>
        internal Range1D Edge { get; init; }

        /// <summary>The cube edge the ticks stand on, in data units.</summary>
        internal AxisEdge3D Line { get; init; }

        /// <summary>The start of <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Line"/> on the screen.</summary>
        internal Point From { get; init; }

        /// <summary>The end of <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Line"/> on the screen.</summary>
        internal Point To { get; init; }

        /// <summary>The unit perpendicular of the edge on the screen that points away from the cube's centre: the way the marks,
        /// the labels and the title all leave the cube.</summary>
        internal Point Outward { get; init; }

        /// <summary>How the labels are written: the axis' formatter, or else the one uniform format of <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Ticks"/>.</summary>
        internal TickDrawContext Labels { get; init; }

        /// <summary>The edge as vertex data (<see cref="Rendering.ThreeDAxesRenderer.SceneVertexWriter.Wire"/>), written once for the row, for every label and
        /// the title to carry; <see langword="null"/> when the scene writes no vertex data.</summary>
        internal string? EdgeWire { get; init; }

        /// <summary>The row of one axis of the cube.</summary>
        /// <param name="cubeAxis">Which axis.</param>
        /// <param name="axis">The axis' settings: its locator, spacing and formatter.</param>
        /// <param name="cube">The cube, the range of each axis (<see cref="AxesRangeExtensions"/>' <c>ComputeCubeRange</c>).</param>
        /// <param name="proj">The projection the scene is drawn through; its face selection picks the edge.</param>
        /// <param name="centroid">The cube's centre on the screen.</param>
        /// <param name="theme">The theme whose tick font the labels start from and whose locator places the ticks of an axis that names
        /// none.</param>
        /// <param name="vertices">The scene's vertex data writer.</param>
        /// <param name="axesWidth">The width of the 3-D axes, the length every axis of its cube lays its ticks along.</param>
        /// <returns>The row.</returns>
        internal static CubeAxisRow Of(CubeAxis cubeAxis, Axis axis, Box3D cube, Projection3D proj, Point centroid,
            Theme theme, SceneVertexWriter vertices, double axesWidth)
        {
            var edge = cube.RangeOf(cubeAxis);
            var ticks = ComputeAxisTicks(axis, edge, theme, axesWidth);
            var line = proj.Faces.AxisEdge(cubeAxis, cube);
            var from = proj.Project(line.From.X, line.From.Y, line.From.Z);
            var to = proj.Project(line.To.X, line.To.Y, line.To.Z);
            return new CubeAxisRow
            {
                Axis = cubeAxis,
                Ticks = ticks,
                GridTicks = GridTicksOf(axis, ticks, theme),
                GridLinesOnEnds = theme.Looks.Cube.AxisLineWidth is not null,
                Edge = edge,
                Line = line,
                From = from,
                To = to,
                Outward = OutwardPerp(from, to, centroid),
                Labels = new TickDrawContext { BaseFont = theme.TickFont(), Formatter = axis.TickFormatter, UniformFormat = ticks.UniformFormat(), Theme = theme },
                EdgeWire = vertices.Writes ? vertices.Wire(line.From, line.To) : null,
            };
        }

        /// <summary>The values the wall lines of an axis stand at: <paramref name="ticks"/> alone, or with the axis' minor values after
        /// them where the theme's grid line is a tick's and the axis has its minor ticks on (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.GridTicks"/>).</summary>
        private static double[] GridTicksOf(Axis axis, double[] ticks, Theme theme)
        {
            // A 3-D axis is drawn on a linear scale whatever scale it names.
            if (!theme.Looks.Ticks.GridStandsOnMinors(axis.MinorTicks, ScaleRule.Linear)) return ticks;

            var grid = new List<double>(ticks);
            foreach (double minor in theme.Looks.Ticks.MinorValuesBetween(axis.MinorTicks, ticks)) grid.Add(minor);
            return [.. grid];
        }

        /// <summary>Whether a tick at <paramref name="value"/> stands on the axis, so a mark, major or minor, a label or a grid
        /// line is drawn there: on the edge, its ends included, give or take a ten-billionth of its length, whichever way the
        /// edge runs (the rule of the 2-D axes, <see cref="Range1D.HoldsClose"/>), in the margins below or above the data too, as
        /// matplotlib's 3-D axis draws every tick in view (axis3d.py), below a bar's floor too. A value past the edge, which a
        /// locator of the caller's own may hand back, stands nowhere.</summary>
        /// <param name="value">The tick value.</param>
        /// <returns><see langword="true"/> when the tick stands.</returns>
        internal bool Holds(double value) => Edge.HoldsClose(value);

        /// <summary>Whether a grid line on a wall stands at <paramref name="value"/>: where a tick stands (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Holds"/>),
        /// but not on an end of the edge, where the cube draws its own edge, unless the cube draws none (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.GridLinesOnEnds"/>).</summary>
        /// <param name="value">The tick value.</param>
        /// <returns><see langword="true"/> when the grid line stands.</returns>
        internal bool HoldsGridLine(double value)
        {
            if (GridLinesOnEnds) return Holds(value);

            var (lo, hi) = Edge.Interval;
            return value > lo && value < hi && Holds(value);
        }

        /// <summary>The label of the tick at <paramref name="value"/> (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Labels"/>).</summary>
        /// <param name="value">The tick value.</param>
        /// <returns>The label text.</returns>
        internal string LabelText(double value) => Labels.LabelText(value);

        /// <summary>The point of the tick at <paramref name="value"/>: the edge's start with its own component replaced.</summary>
        /// <param name="value">The tick value.</param>
        /// <returns>The point, in data units.</returns>
        internal Vec3 TickPoint(double value) => Axis switch
        {
            CubeAxis.X => new(value, Line.From.Y, Line.From.Z),
            CubeAxis.Y => new(Line.From.X, value, Line.From.Z),
            _ => new(Line.From.X, Line.From.Y, value),
        };

        /// <summary>How far a tick label stands out from the edge: the mark, its pad and <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.ThreeDExtraPad"/>.</summary>
        /// <param name="major">The axis' major tick settings.</param>
        /// <returns>The distance in pixels.</returns>
        internal static double LabelOffset(TickConfig major) => major.Length + major.Pad + ThreeDExtraPad;

        /// <summary>
        /// How far out from its edge the axis title stands so it clears the tick labels the row draws. Both sit on the same
        /// outward perpendicular, the labels at <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.LabelOffset"/> from their ticks and the title at this pad from its
        /// point on the edge, so only the difference of the two separates them where a tick sits at that point. Text boxes are
        /// upright on the screen; projected on the perpendicular, a box of w by h reaches |perp.X|·w + |perp.Y|·h, and keeping the
        /// two projections apart proves the boxes disjoint wherever along the edge a label sits. Measured per draw, because it
        /// depends on the camera, the tick values and the fonts; only the labels where a tick stands (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Holds"/>) count.
        /// </summary>
        /// <param name="ctx">The context that measures the texts.</param>
        /// <param name="axis">The axis: its major tick settings.</param>
        /// <param name="title">The title the axis draws (<c>AxesTitleExtensions.TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="titleFont">The font the title is drawn in.</param>
        /// <returns>The pad in pixels.</returns>
        internal double TitlePad(IRenderContext ctx, Axis axis, string? title, Font titleFont)
        {
            var major = axis.MajorTicks;
            double labelOffset = LabelOffset(major);
            if (title is null || !major.Visible) return labelOffset + TitleGapPx;

            var labelFont = Labels.LabelFont(major);
            double labelW = 0, labelH = 0;
            foreach (double t in Ticks)
            {
                if (!Holds(t)) continue;
                var size = ctx.ExtentOf(TextLabel.Of(LabelText(t)), labelFont);
                if (size.Width > labelW) labelW = size.Width;
                if (size.Height > labelH) labelH = size.Height;
            }

            var titleSize = ctx.ExtentOf(TextLabel.Of(title), titleFont);
            double perpX = Math.Abs(Outward.X), perpY = Math.Abs(Outward.Y);
            double labelReach = perpX * labelW + perpY * labelH;
            double titleReach = perpX * titleSize.Width + perpY * titleSize.Height;

            return labelOffset + (labelReach + titleReach) / 2 + TitleGapPx;
        }

        /// <summary>The major tick values of one 3-D axis: those its request names (<see cref="TickRequest"/>,
        /// <see cref="TickAxisKind.Cube"/>): a locator the axis was given, told how many labels fit along the width of the 3-D axes (a 3-D axis
        /// is matplotlib's <c>XAxis</c> and counts its tick space so), then a fixed spacing, then the theme's ladder laid with the 3-D raw step,
        /// else the library's cube placement, each over the edge's interval low to high, so an edge whose limits are set high to low has the
        /// ticks of the same interval, each drawn where its value is (matplotlib's locators take the view's ends in order). Which of them stand
        /// on the axis is the row's to decide (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Holds"/>).</summary>
        /// <param name="axis">The axis.</param>
        /// <param name="range">The range its edge runs over, either way round.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <param name="axesWidth">The width of the 3-D axes.</param>
        /// <returns>The tick values.</returns>
        internal static double[] ComputeAxisTicks(Axis axis, Range1D range, Theme theme, double axesWidth) =>
            new TickRequest(range, axesWidth, axis.TickLabelSizeUnder(theme), AlongX: true, theme, TickAxisKind.Cube)
            {
                Given = axis.TickLocator,
                Spacing = axis.MajorTicks.Spacing,
            }.Majors();

        /// <summary>The unit perpendicular of the screen edge from <paramref name="edgeA"/> to <paramref name="edgeB"/>, turned so
        /// it points away from <paramref name="cubeCentroid"/>. One rule for the tick marks, the tick labels and the axis title:
        /// they must leave the cube the same way, or the title lands on another side than the labels it clears.</summary>
        /// <param name="edgeA">The start of the edge on the screen.</param>
        /// <param name="edgeB">The end of the edge on the screen.</param>
        /// <param name="cubeCentroid">The cube's centre on the screen.</param>
        /// <returns>The unit perpendicular.</returns>
        internal static Point OutwardPerp(Point edgeA, Point edgeB, Point cubeCentroid)
        {
            var perp = edgeA.HeadingTo(edgeB).RightNormal;
            var edgeMid = new Point((edgeA.X + edgeB.X) / 2, (edgeA.Y + edgeB.Y) / 2);
            double towardCubeX = cubeCentroid.X - edgeMid.X;
            double towardCubeY = cubeCentroid.Y - edgeMid.Y;
            return perp.X * towardCubeX + perp.Y * towardCubeY > 0 ? new Point(-perp.X, -perp.Y) : perp;
        }
    }
}
