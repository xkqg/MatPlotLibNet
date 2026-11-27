// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The names the SVG writer puts on a chart for its interaction scripts, written once: the chart's id, the view of each
/// axes (its plot area, its range, the scale and breaks of each axis, the axes stacked past the primary), a series' group and a
/// legend entry, and the SVG namespace. The writers append them and every script reads them from here, interpolated into its text,
/// so a name cannot change on one side of the page and be missed by the other.</summary>
/// <remarks>The markup a kind of series writes for its own script (a 3-D scene's vertices, a treemap's nodes, a Sankey diagram's
/// links, the rows of a linked selection, an animation's frames) is named by that kind's writer, beside the one script that reads
/// it; the names here are the ones the figure-level writers share with more than one script.</remarks>
internal static class SvgAttributes
{
    /// <summary>The SVG namespace, the value of the root's <c>xmlns</c>.</summary>
    internal const string Namespace = "http://www.w3.org/2000/svg";

    /// <summary>The id of a server-interactive chart, on its root.</summary>
    internal const string ChartId = "data-chart-id";

    /// <summary>The index of the axes a view belongs to.</summary>
    internal const string AxesIndex = "data-axes-index";

    /// <summary>The slot of the series a group draws.</summary>
    internal const string SeriesIndex = "data-series-index";

    /// <summary>The slot of the series a legend entry stands for.</summary>
    internal const string LegendIndex = "data-legend-index";

    /// <summary>Set on a server-interactive chart while a drag or a band is in progress, so a page that mounts a redrawn chart waits
    /// for it to go. The Blazor, React, Vue and Angular clients read it too, by the same name.</summary>
    internal const string Busy = "data-mpl-busy";

    /// <summary>What the name of an attribute of the primary axes' view starts with.</summary>
    internal const string View = "data-";

    /// <summary>What the name of an attribute of a Y axis stacked past the primary starts with, before its side and its slot.</summary>
    internal const string SecondaryView = View + "sec-";

    /// <summary>The side of a stacked Y axis in its attribute names: on the right.</summary>
    internal const string RightSide = "right";

    /// <summary>The side of a stacked Y axis in its attribute names: on the left.</summary>
    internal const string LeftSide = "left";

    /// <summary>The left of the plot area, in pixels.</summary>
    internal const string PlotX = View + "plot-x";

    /// <summary>The top of the plot area, in pixels.</summary>
    internal const string PlotY = View + "plot-y";

    /// <summary>The width of the plot area, in pixels.</summary>
    internal const string PlotW = View + "plot-w";

    /// <summary>The height of the plot area, in pixels.</summary>
    internal const string PlotH = View + "plot-h";

    /// <summary>The end of a view's name for the low end of its X range.</summary>
    internal const string XMinStem = "xmin";

    /// <summary>The end of a view's name for the high end of its X range.</summary>
    internal const string XMaxStem = "xmax";

    /// <summary>The end of a view's name for the low end of its Y range.</summary>
    internal const string YMinStem = "ymin";

    /// <summary>The end of a view's name for the high end of its Y range.</summary>
    internal const string YMaxStem = "ymax";

    /// <summary>The end of a view's name, after its axis' letter, for the scale the axis is drawn in.</summary>
    internal const string ScaleStem = "scale";

    /// <summary>The end of a view's name, after its axis' letter, for the width of a symlog axis' linear stretch.</summary>
    internal const string LinThreshStem = "linthresh";

    /// <summary>The end of a view's name, after its axis' letter, for the breaks cut out of the axis.</summary>
    internal const string BreaksStem = "breaks";

    /// <summary>The end of a view's name, after its axis' letter, for the room each break between two parts of the axis leaves.</summary>
    internal const string RoomStem = "room";

    /// <summary>The end of the name that counts the Y axes stacked on one side.</summary>
    internal const string CountStem = "count";

    /// <summary>The low end of the drawn X range.</summary>
    internal const string XMin = View + XMinStem;

    /// <summary>The high end of the drawn X range.</summary>
    internal const string XMax = View + XMaxStem;

    /// <summary>The low end of the drawn Y range.</summary>
    internal const string YMin = View + YMinStem;

    /// <summary>The high end of the drawn Y range.</summary>
    internal const string YMax = View + YMaxStem;

    /// <summary>The low end of the X range Home goes back to, on the root.</summary>
    internal const string ResetXMin = View + "reset-" + XMinStem;

    /// <summary>The high end of the X range Home goes back to, on the root.</summary>
    internal const string ResetXMax = View + "reset-" + XMaxStem;

    /// <summary>The low end of the Y range Home goes back to, on the root.</summary>
    internal const string ResetYMin = View + "reset-" + YMinStem;

    /// <summary>The high end of the Y range Home goes back to, on the root.</summary>
    internal const string ResetYMax = View + "reset-" + YMaxStem;

    /// <summary>The scale of the X axis, when it is not linear.</summary>
    internal const string XScale = View + "x" + ScaleStem;

    /// <summary>The scale of the Y axis, when it is not linear.</summary>
    internal const string YScale = View + "y" + ScaleStem;

    /// <summary>The linear stretch of a symlog X axis.</summary>
    internal const string XLinThresh = View + "x" + LinThreshStem;

    /// <summary>The linear stretch of a symlog Y axis.</summary>
    internal const string YLinThresh = View + "y" + LinThreshStem;

    /// <summary>The breaks of the X axis.</summary>
    internal const string XBreaks = View + "x" + BreaksStem;

    /// <summary>The breaks of the Y axis.</summary>
    internal const string YBreaks = View + "y" + BreaksStem;

    /// <summary>The room each break between two parts of the X axis leaves (<see cref="AxisBreakMap.Room"/>).</summary>
    internal const string XRoom = View + "x" + RoomStem;

    /// <summary>The room each break between two parts of the Y axis leaves.</summary>
    internal const string YRoom = View + "y" + RoomStem;

    /// <summary>How many Y axes are stacked on the right.</summary>
    internal const string SecondaryRightCount = SecondaryView + RightSide + "-" + CountStem;

    /// <summary>How many Y axes are stacked on the left.</summary>
    internal const string SecondaryLeftCount = SecondaryView + LeftSide + "-" + CountStem;
}
