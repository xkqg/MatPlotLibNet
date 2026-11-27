// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Whether something asks for the axes to be scaled, under the matplotlib themes: a series, and an axes along one axis through
/// the series and the overlays it holds or shares a scale with. An empty axis nothing asks for keeps the first view a matplotlib axes was made with
/// (<see cref="AxisAutoscale.FirstViewWhenEmpty"/>); the range of the primary axes (<see cref="AxesRangeExtensions"/>) and of an axis past
/// it (<see cref="CartesianAxisPlacement"/>) read it.</summary>
internal static class AutoscaleAskExtensions
{
    extension(Axes axes)
    {
        /// <summary>Whether a series or an overlay asks for the X axis (<paramref name="isX"/>) or the Y axis of the axes to be scaled
        /// (<see cref="HoldsWhatAsksAlong"/>): one of the axes, or of an axes that shares that scale with it, nearest first, each axes
        /// once (matplotlib flags every axes of a shared scale when one asks). Asked only of an empty axis under the matplotlib themes,
        /// whose first view it keeps when none asks (<see cref="AxisAutoscale.FirstViewWhenEmpty"/>); walked by index: a foreach over the list
        /// boxes its enumerator.</summary>
        /// <param name="isX">Whether the X axis is asked about, else the Y axis.</param>
        /// <returns><see langword="true"/> when one asks.</returns>
        internal bool AsksForTheAutoscaleAlong(bool isX)
        {
            if (axes.HoldsWhatAsksAlong(isX)) return true;
            if (axes.SharesScaleAlong(isX) is null) return false;
            var visited = new HashSet<Axes> { axes };
            for (var cur = axes.SharesScaleAlong(isX); cur is not null && visited.Add(cur); cur = cur.SharesScaleAlong(isX))
                if (cur.HoldsWhatAsksAlong(isX)) return true;
            return false;
        }

        /// <summary>Whether a series of this axes asks for its axes to be scaled (<see cref="AsksForTheAutoscale"/>), or an overlay asks
        /// for the X axis (<paramref name="isX"/>) or the Y axis: a trend line asks for both (<c>axline</c>, <c>axes/_axes.py</c> 992), a
        /// span for the axis its edges stand on (<c>axvspan</c> for X, 1110; <c>axhspan</c> for Y, 1047), whether or not its edges are values
        /// it adds (<see cref="AxesRangeExtensions"/>). A reference line asks only when its value lies past the view (<c>axvline</c>'s
        /// <c>scalex</c>), which a value it adds already does.</summary>
        private bool HoldsWhatAsksAlong(bool isX)
        {
            for (int i = 0; i < axes.Series.Count; i++)
                if (axes.Series[i].AsksForTheAutoscale()) return true;
            if (axes.Trendlines.Count > 0) return true;
            for (int i = 0; i < axes.Spans.Count; i++)
                if (axes.Spans[i].Orientation.RunsAlongX != isX) return true;
            return false;
        }
    }

    extension(ISeries series)
    {
        /// <summary>Whether <paramref name="series"/> asks for the axes it is drawn on to be scaled, under the matplotlib themes: one that
        /// holds data does, as matplotlib's plotting functions ask for the autoscale of both axes; one that holds none does when its
        /// function asks whatever it is handed (<see cref="ChartSeries.AsksWhenEmpty"/>); one that draws without axes
        /// (<see cref="ChartSeries.DrawsWithoutAxes"/>) never does, as its marks stand where it puts them.</summary>
        /// <returns><see langword="true"/> when it asks.</returns>
        internal bool AsksForTheAutoscale() =>
            series is not ChartSeries chart || (!chart.DrawsWithoutAxes && (!chart.HoldsNoData || chart.AsksWhenEmpty));
    }
}
