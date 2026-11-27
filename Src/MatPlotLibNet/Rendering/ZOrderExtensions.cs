// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where each thing a Cartesian axes draws in its data layer stands in the z-order under a theme: the one owner of that
/// rule. Under the matplotlib themes (<see cref="AxesFrameLook.AxisZOrder"/> set) it is matplotlib's: the grid and the tick marks at the
/// axis' zorder (<c>axes.axisbelow</c>), a span and a patch at 1, a reference line and a drawing tool at 2 (an <c>axhline</c> is a
/// <c>Line2D</c>), a series at the zorder it was given or else at its kind's (<see cref="ChartSeries.ArtistZOrder"/>). Under the
/// library's own themes the grid, the spans, the patches given none, the reference lines and the tools stand under every series in
/// that order, and a series stands at its <see cref="ISeries.ZOrder"/>.</summary>
internal static class ZOrderExtensions
{
    extension(Theme theme)
    {
        /// <summary>Whether this theme draws in matplotlib's z-order.</summary>
        private bool OrdersLikeMatplotlib => theme.Looks.Frame.AxisZOrder is not null;

        /// <summary>Where the grid stands.</summary>
        internal double GridZ => theme.Looks.Frame.AxisZOrderOr(double.NegativeInfinity);

        /// <summary>Where the tick marks of the primary axes stand among the data: at the axis' zorder under the matplotlib themes, as
        /// matplotlib's <c>Axis.draw</c> draws its tick marks with the axis; <see langword="null"/> under the library's own themes,
        /// which draw each mark with its label, under every data layer.</summary>
        internal double? TickMarksZ => theme.Looks.Frame.AxisZOrder;

        /// <summary>Where the spans stand.</summary>
        internal double SpansZ => theme.OrdersLikeMatplotlib ? MatplotlibArtist.Patch.ZOrder : double.NegativeInfinity;

        /// <summary>Where the reference lines and the drawing tools stand.</summary>
        internal double OverlayLinesZ => theme.OrdersLikeMatplotlib ? MatplotlibArtist.Line.ZOrder : double.NegativeInfinity;

        /// <summary>Where <paramref name="patch"/> stands: the z-order it was given, else matplotlib's 1, or under every series under
        /// the library's own themes.</summary>
        /// <param name="patch">The patch.</param>
        internal double ZOf(Patch patch) =>
            patch.GivenZOrder ?? (theme.OrdersLikeMatplotlib ? MatplotlibArtist.Patch.ZOrder : double.NegativeInfinity);

        /// <summary>Where <paramref name="series"/> stands: its <see cref="ISeries.ZOrder"/>, unless the theme draws in matplotlib's
        /// z-order and it is a series of the library given none, which then stands at its kind's.</summary>
        /// <param name="series">The series.</param>
        internal double ZOf(ISeries series) =>
            theme.OrdersLikeMatplotlib && series is ChartSeries chart ? chart.GivenZOrder ?? chart.ArtistZOrder : series.ZOrder;
    }
}
