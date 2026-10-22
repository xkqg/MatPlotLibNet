// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What an axes draws past its four spines besides the title and the end labels, measured by the renderer that
/// draws it (<see cref="AxesRenderer.MeasureEdgeBands"/>).</summary>
/// <param name="Top">From the top spine to the top of what is drawn above it below the axes title: the mirrored X tick
/// labels, or the secondary X axis that owns the edge (its tick labels, then its title). 0 when nothing is drawn
/// there.</param>
/// <param name="Right">From the right spine to the far side of the mirrored Y tick labels; 0 when none are drawn there.
/// The secondary Y axis is measured on its own (<see cref="ConstrainedLayoutEngine.SecondaryAxisBand"/>) and
/// <see cref="RightBand.Axis"/> takes the wider.</param>
/// <param name="Left">From the left spine to the far side of the category names the Y axis carries (bars that lie down):
/// the tick mark, the pad and the widest name. 0 when the Y axis names no categories (a numeric axis, whose labels the
/// layout estimates) or draws no labels.</param>
/// <param name="Bottom">From the bottom spine to the far side of the X tick labels, numbers, dates or the category names
/// the X axis carries (upright bars, a count plot, a candlestick's dates): the tick mark, the pad and the tallest label. 0
/// when the X axis draws no tick labels.</param>
/// <param name="BottomTitle">From the bottom spine to the far side of the X axis title drawn below the tick labels
/// (<see cref="AxisTickExtensions.BottomAxisTitleExtent"/>, the rule the title is placed by); 0 when the axes has no X
/// axis title or draws it elsewhere (a 3-D box draws it along its edge, a polar axes not at all).</param>
internal readonly record struct EdgeBands(double Top, double Right, double Left, double Bottom, double BottomTitle = 0);
