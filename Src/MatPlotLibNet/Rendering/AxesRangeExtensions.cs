// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Extension methods on <see cref="Axes"/> that support the data-range pipeline consumed by
/// <see cref="CartesianAxisPlacement.ComputeDataRanges"/> and the cube of a 3-D axes (<c>ComputeCubeRange</c>). Kept as
/// extensions rather than static helpers on the renderer so the operations can be reused from other renderers
/// (<see cref="PolarAxesRenderer"/>, <see cref="ThreeDAxesRenderer"/>) without duplication.
/// </summary>
internal static class AxesRangeExtensions
{
    /// <summary>The least margin a 3-D scatter keeps on Z in every style (<c>Axes3D.scatter</c>:
    /// <c>if self._zmargin &lt; 0.05 and xs.size &gt; 0: self.set_zmargin(0.05)</c>).</summary>
    private const double Scatter3DZMargin = 0.05;

    /// <summary>What every axis of a 2-D axes is ranged from, worked out once for both (<see cref="RangeOf"/>): the axes, its theme, its
    /// own contributions and its patches', the contributions of the axes that share each scale, whether the patches range each axis,
    /// the theme's scatter margin, the autolimit rule and whether a series runs each axis down.</summary>
    /// <param name="Axes">The axes ranged.</param>
    /// <param name="Theme">The theme it is drawn under.</param>
    /// <param name="Own">Its own series' and overlays' contributions.</param>
    /// <param name="OwnPatches">Its patches' contributions; none when the patches range no axis.</param>
    /// <param name="Shared">Per axis, the contributions of the axes that share its scale.</param>
    /// <param name="PatchesOn">Per axis, whether the patches take part in its range.</param>
    /// <param name="ScatterFloor">The least margin a scatter asks of X and Y under the theme.</param>
    /// <param name="Rule">Where the theme's autolimit mode (or a tight request) ends each axis.</param>
    /// <param name="RunsDown">Per axis, whether a series runs it down (the Y axis of an image's rows; never X).</param>
    /// <param name="MarginRaise">Per axis, how much the series raise its margin (<see cref="AxesAsk.MarginRaise"/>).</param>
    private readonly record struct RangeInputs(Axes Axes, Theme Theme, DataRangeContribution[] Own, DataRangeContribution[] OwnPatches,
        AxisPair<DataRangeContribution[]> Shared, AxisPair<bool> PatchesOn, double ScatterFloor, AutolimitRule Rule, AxisPair<bool> RunsDown,
        AxisPair<double> MarginRaise);

    extension(Axes axes)
    {
        /// <summary>The combined X and Y ranges the axes draws: its own series and the axes it shares a scale with, ranged and
        /// padded and snapped by its axis' autoscale policy (<see cref="AxisAutoscale"/>): aggregate (set limits are exact), normalise, pad with the
        /// axis margin or the theme's, clamp to the sticky edges, end where the theme's autolimit mode says
        /// (<see cref="AxisAutoscale.Autolimited"/>), orient. It reads the axes and the theme
        /// and nothing about the plot area, so the aspect of a plot, which is the ratio of these two ranges, can be asked of the
        /// axes before any rectangle exists; the renderer delegates here, so the two cannot disagree.</summary>
        /// <param name="theme">The theme whose margins pad the range where the axes sets none.</param>
        /// <returns>The drawn range.</returns>
        internal DataRange ComputeDataRanges(Theme theme) => axes.RangeOf(theme).Range;

        /// <summary>The range the axes draws (<see cref="ComputeDataRanges"/>) together with whether its patches can reach past the
        /// plot (<see cref="AxesRange.PatchesMayReachPastPlot"/>), from one pass over what asks for the range: the clip of the patches
        /// is decided by the same question the range of the patches is (<see cref="AxisRangeLook.PatchesRange"/>), so the two cannot disagree and
        /// no series is asked for its range a second time.</summary>
        /// <param name="theme">The theme whose margins pad the range where the axes sets none.</param>
        /// <returns>The range and the clip.</returns>
        internal AxesRange RangeOf(Theme theme)
        {
            // One snapshot of the own series (Histogram rebuilds bins each call), then each scale's contributions: own plus the
            // axes it is shared with. The same contributions range the scale and decide its sticky edges, so the axes that share a
            // scale cannot disagree on its margins. Each axis is then ranged by the one pipeline (RangeAlong).
            var own = axes.SnapshotContributions(theme);
            var start = new RangeInputs(axes, theme, own, [], default, default, 0, AutolimitRule.Data, default, default);
            var shared = axes.PrimaryAxes.Select(start, static (_, isX, state) => state.Axes.LinkedContributions(isX, state.Theme));
            // The patches range an axis where the theme lets them (all of them under the library's own themes; under matplotlib's an axis
            // something asks for: a value added to it here or by an axes sharing it, or a series or an overlay that asks without one,
            // AsksForTheAutoscaleAlong, as matplotlib autoscales over the data limits the patches are in); the own ones are worked out
            // once for both axes.
            var patchesOn = shared.Select(start, static (linked, isX, state) => state.Theme.Looks.Range.PatchesRange(
                state.Own.AsksForRange(isX) || linked.AsksForRange(isX) || state.Axes.AsksForTheAutoscaleAlong(isX)));
            // A series that asks for a tight range (a hexbin, an image under the matplotlib themes) ends both axes at the data and the
            // margin, whatever the theme's mode (matplotlib's autoscale_view skips view_limits when _tight; imshow sets the limits to the
            // image's extent); one that runs the rows of an image down runs the Y axis down (imshow's set_ylim(rows - 0.5, -0.5)).
            var ask = axes.AskUnder(theme);
            var inputs = start with
            {
                OwnPatches = patchesOn.Any ? axes.PatchContributions(theme) : [],
                Shared = shared,
                PatchesOn = patchesOn,
                ScatterFloor = axes.ScatterMarginIn(theme),
                Rule = ask.Tight ? AutolimitRule.Data : theme.Looks.Range.AutolimitRule,
                RunsDown = new AxisPair<bool>(false, ask.RowsDown),
                MarginRaise = ask.MarginRaise,
            };
            var range = axes.PrimaryAxes.Select(inputs, static (axis, isX, state) => state.Axes.RangeAlong(isX, axis, state));

            // Asked only of an axes that has patches (almost none does): HasExplicitLimit walks the Y slots. The patches are clipped by
            // the rule the series are (ClipsItsMarksUnder, a patch's artist), and also where they take no part in the range of an axis.
            // An axis the patches do not range (the theme's answer, PatchesRange, under the matplotlib themes only) can leave them past it.
            bool patchesMayReach = axes.Patches.Count > 0 && (axes.ClipsItsMarksUnder(theme, MatplotlibArtist.Patch) || !(patchesOn.X && patchesOn.Y));
            return new AxesRange(new DataRange(range.X.Lo, range.X.Hi, range.Y.Lo, range.Y.Hi), patchesMayReach);
        }

        /// <summary>The range one axis of a 2-D axes draws (X along X, the primary Y otherwise), by the one pipeline both run: aggregate,
        /// normalise, pad, clamp to the sticky edges, end where the autolimit rule says, orient.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="inputs">What both axes are ranged from (<see cref="RangeInputs"/>).</param>
        /// <returns>The range, in drawn orientation.</returns>
        private Range1D RangeAlong(bool isX, Axis axis, in RangeInputs inputs)
        {
            // === 1. Aggregate — seed from the axis' limits, fold in own + shared (+ patches where they range it); a set limit is exact ===
            bool patchesOn = inputs.PatchesOn.Along(isX);
            var contribs = Gather(inputs.Own, inputs.Shared.Along(isX), patchesOn ? inputs.OwnPatches : [],
                patchesOn ? axes.LinkedPatchContributions(isX, inputs.Theme) : []);
            var auto = new AxisAutoscale(axis, isX, inputs.Theme);
            var range = auto.WithExplicitLimits(contribs.FoldRange(isX, auto.Seed));
            bool runsDown = inputs.RunsDown.Along(isX) != axis.Inverted;

            // === 2. Open — an empty axis nothing asks for keeps its first view under the matplotlib themes; any other range with no
            // width is opened (the library's rule or the scale's locator, by theme) and taken onto its scale before any math ===
            if (auto.FirstViewWhenEmpty(range) is { } first && !axes.AsksForTheAutoscaleAlong(isX))
                return Turned(first, runsDown);
            range = auto.Opened(range, contribs.SmallestPositive(isX));

            // === 3. Drive padding + sticky + the theme's autolimit mode from the contributions gathered once above ===
            // The unpadded snapshot is captured BEFORE padding so sticky clamp can distinguish
            // "padding pushed past the sticky" (snap back) from "another series has data past the
            // sticky" (preserve, an overlay's sticky must not clip a wider underlying series).
            var unpadded = range;
            // The margin the axis was given, else the theme's (raised to a scatter's floor), raised by what the series ask (a rug's).
            range = auto.Padded(range, axis.Margin ?? Math.Max(inputs.Theme.AxisMarginAlong(isX), inputs.ScatterFloor) + inputs.MarginRaise.Along(isX));
            for (int i = 0; i < contribs.Length; i++)
                range = auto.ClampSticky(range, contribs[i].StickyMinAlong(isX), contribs[i].StickyMaxAlong(isX), unpadded);

            // An axis whose ticks name categories is placed by a fixed set of ticks, which a round-numbers end does not move, unless its
            // series numbers its rows first and names them after (a Gantt chart, TickSource.HoldsTheRange); asked only in that mode, as it
            // reads the series.
            range = auto.Autolimited(range, inputs.Rule, inputs.Theme.Looks.Ticks.Ladder, unpadded, contribs.HasAnySticky(isX),
                namesCategories: inputs.Rule.RoundsToTicks && axes.PrimaryTickSource(isX, inputs.Theme).HoldsTheRange);

            // === 4. Orient — an inverted axis is drawn high to low, the way a limit set high to low already is, and so is the Y axis
            // of an image's rows under the matplotlib themes, once, as inverted is a state ===
            // An inverted Y axis turns an image's rows back up, as matplotlib's invert_yaxis() toggles the limits imshow set.
            return Turned(range, runsDown);

            static Range1D Turned(Range1D range, bool runsDown) => runsDown && range.Lo < range.Hi ? new Range1D(range.Hi, range.Lo) : range;

            // An axes without patches (almost every one) gathers two lists, as it always did.
            static DataRangeContribution[] Gather(DataRangeContribution[] own, DataRangeContribution[] shared,
                DataRangeContribution[] ownPatches, DataRangeContribution[] linkedPatches)
            {
                if (ownPatches.Length == 0 && linkedPatches.Length == 0)
                    return [.. own, .. shared];
                var all = new DataRangeContribution[own.Length + shared.Length + ownPatches.Length + linkedPatches.Length];
                own.CopyTo(all, 0);
                shared.CopyTo(all, own.Length);
                ownPatches.CopyTo(all, own.Length + shared.Length);
                linkedPatches.CopyTo(all, own.Length + shared.Length + ownPatches.Length);
                return all;
            }
        }

        /// <summary>The cube a 3-D axes draws over, by matplotlib's two-stage expansion (<c>Axes3D._autoscale_axis</c> and
        /// <c>_set_lim3d(view_margin=1/48)</c>). Each series reports its
        /// extent once; a grid series reports its grid (over the drawable values only) here instead, so the 2-D axes it can also
        /// be placed on do not move. Then each axis runs its autoscale policy (<see cref="AxisAutoscale"/>): a degenerate range normalised, a
        /// set limit exact, the stage-1 margin (the axis' own or the theme's on X and Y; on Z none under the perspective
        /// projection, 5 % with a scatter), the theme's autolimit mode (<see cref="AxisAutoscale.Autolimited"/>: under the classic
        /// theme the rounding out to the axis' outermost ticks, the 2-D axes' rule), the view margin of a
        /// forty-eighth on all three, and the sticky edges a series of your own registers (no built-in 3-D series does, as
        /// matplotlib's 3-D axes knows none). No other rounding: matplotlib's
        /// default style scales a 3-D axis tight. Verified against matplotlib for its bar3d scene: raw X <c>[-0.3, 2.3]</c> gives
        /// <c>[-0.4896, 2.4896]</c>.</summary>
        /// <param name="theme">The theme whose margins pad X and Y where the axis sets none.</param>
        /// <returns>The range of each axis of the cube.</returns>
        internal Box3D ComputeCubeRange(Theme theme)
        {
            var x = Range1D.Empty;
            var y = Range1D.Empty;
            var z = Range1D.Empty;

            // The series are walked by index here and below: a foreach over the list boxes its enumerator, 40 bytes a draw.
            for (int i = 0; i < axes.Series.Count; i++)
            {
                if (axes.Series[i] is I3DGridSeries grid)
                {
                    x = x.IncludingSpan(grid.X.DrawableRange());
                    y = y.IncludingSpan(grid.Y.DrawableRange());
                    z = z.IncludingSpan(grid.Z.DrawableRange());
                }
            }

            // What each series reports: a bar widens X and Y by its width and takes its floor at 0 in on Z.
            var context = new AxesContextAdapter(axes);
            var contribs = new DataRangeContribution[axes.Series.Count];
            for (int i = 0; i < contribs.Length; i++)
            {
                var c = axes.Series[i].ComputeDataRange(context);
                contribs[i] = c;
                x = x.Merge(c.XMin, c.XMax);
                y = y.Merge(c.YMin, c.YMax);
                z = z.Merge(c.ZMin, c.ZMax);
            }

            // matplotlib's Axes3D pads X and Y by axes.xmargin and axes.ymargin (the theme's margins, raised for a scatter by the
            // theme's scatter margin as in 2-D: Axes3D.scatter calls Axes.scatter) and, in clear(), gives Z axes.zmargin only
            // under an orthographic projection (focal length infinite) and none under a perspective one, which the projection
            // always is (a camera distance included). A scatter raises Z's to 5 % in every style
            // (Axes3D.scatter: if self._zmargin < 0.05: self.set_zmargin(0.05)).
            bool holdsScatter = axes.HoldsScatter();
            double scatterFloor = holdsScatter ? theme.Looks.Range.ScatterMarginOr(0) : 0;
            double zMarginDefault = holdsScatter ? Scatter3DZMargin : 0.0;
            var rule = theme.Looks.Range.AutolimitRule.InCube;

            return new Box3D(
                CubeAxis(axes.XAxis, isX: true, x, Math.Max(theme.AxisXMargin, scatterFloor), static c => c.StickyXMin, static c => c.StickyXMax),
                CubeAxis(axes.YAxis, isX: false, y, Math.Max(theme.AxisYMargin, scatterFloor), static c => c.StickyYMin, static c => c.StickyYMax),
                CubeAxis(axes.ZAxis, isX: false, z, zMarginDefault, static c => c.StickyZMin, static c => c.StickyZMax));

            // One axis of the cube, by the one pipeline the three run: a degenerate range normalised and a set limit exact, padded by the
            // axis' own margin or the theme's, ended where the theme's autolimit mode says (matplotlib's classic style rounds it out to its
            // outermost ticks; a theme that names none ends it where the data and the margin end, as matplotlib's default does), padded by
            // the view margin of a forty-eighth, held to the sticky edges a series of your own registers.
            Range1D CubeAxis(Axis axis, bool isX, Range1D raw, double themeMargin,
                Func<DataRangeContribution, double?> stickyMin, Func<DataRangeContribution, double?> stickyMax)
            {
                const double viewMargin = 1.0 / 48.0;
                var auto = new AxisAutoscale(axis, isX, theme);
                var unpadded = auto.UnpaddedInCube(raw, isZ: ReferenceEquals(axis, axes.ZAxis));
                var range = auto.Padded(unpadded, axis.Margin ?? themeMargin);
                range = auto.Autolimited(range, rule, theme.Looks.Ticks.Ladder, unpadded, hasSticky: false);
                range = auto.Padded(range, viewMargin);
                foreach (var c in contribs)
                    range = auto.ClampSticky(range, stickyMin(c), stickyMax(c), unpadded);
                return range;
            }
        }

        /// <summary>What the series the axes holds ask of it (<see cref="IAsksOfAxes"/>: each kind says it itself), from one walk over them.
        /// <see cref="AxesAsk.Tight"/>: a series asks for a tight range, which ends both axes where the data and the margin end, rounded out to
        /// no tick under any theme (a hexbin and a map under every theme, a contour under the matplotlib themes, an image standing as
        /// <c>imshow</c> stands it); the request is the axes', so every series on it is ranged tight. <see cref="AxesAsk.RowsDown"/>: a series
        /// runs the Y axis down, the first row on top (<c>imshow</c>'s <c>set_ylim(rows - 0.5, -0.5)</c>), unless the Y axis has a limit of its
        /// own, which <c>imshow</c> leaves as it is. <see cref="AxesAsk.Aspect"/>: the first aspect a series asks (a map's 1).
        /// <see cref="AxesAsk.MarginRaise"/>: what the series raise the margins by together (a rug's twice its height across it). Asked of the
        /// series the axes holds, as its scatter margin is (<see cref="ScatterMarginIn"/>), so a series' contribution keeps its size; walked by
        /// index: a foreach over the list boxes its enumerator.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>What the series ask.</returns>
        internal AxesAsk AskUnder(Theme theme)
        {
            bool tight = false, rowsDown = false;
            double? aspect = null;
            AxisPair<double> raise = default;
            for (int i = 0; i < axes.Series.Count; i++)
            {
                if (axes.Series[i] is not IAsksOfAxes asking) continue;
                var ask = asking.AskUnder(theme);
                tight |= ask.Tight;
                rowsDown |= ask.RowsDown;
                aspect ??= ask.Aspect;
                raise = raise.Select(ask.MarginRaise, static (sum, isX, more) => sum + more.Along(isX));
            }

            return new AxesAsk(tight, rowsDown && !axes.YAxis.Min.HasValue && !axes.YAxis.Max.HasValue, aspect, raise);
        }

        /// <summary>The aspect the plot of the axes is drawn at: the one it was given (<see cref="Axes.Aspect"/>), else the one its series
        /// ask (<see cref="AskUnder"/>: a map's 1), else none. The one rule the layout fits the plot by and the SVG writer locks a zoom by.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The aspect, <see langword="null"/> when the plot fills its cell; one that is not positive and finite takes no effect.</returns>
        internal double? AspectUnder(Theme theme) => axes.Aspect ?? axes.AskUnder(theme).Aspect;

        /// <summary>The least margin the theme pads X and Y of this axes by because it holds a scatter with points
        /// (<see cref="AxisRangeLook.ScatterMargin"/>, matplotlib's classic <c>Axes.scatter</c>), or 0 when the theme names none or the
        /// axes holds no such scatter. The one rule a 2-D and a 3-D axes read; the series are walked only under a theme that
        /// names a margin.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The margin, as a fraction of the range.</returns>
        internal double ScatterMarginIn(Theme theme) => theme.Looks.Range.ScatterMargin is { } floor && axes.HoldsScatter() ? floor : 0;

        /// <summary>Whether the axes holds a scatter that has points (<c>x.size &gt; 0</c> in matplotlib's <c>Axes.scatter</c> and
        /// <c>Axes3D.scatter</c>, which raise the axes' margins): a <see cref="ScatterSeries"/>, a <see cref="BubbleSeries"/> (a scatter
        /// with an area for each point), a <see cref="Scatter3DSeries"/>, a <see cref="PolarScatterSeries"/> (matplotlib's polar
        /// axes takes its scatter by the same <c>Axes.scatter</c>), a <see cref="StripplotSeries"/> or <see cref="SwarmplotSeries"/>
        /// (seaborn draws each group with <c>ax.scatter</c>), or a <see cref="ResidualSeries"/> (seaborn's <c>residplot</c> draws its
        /// residuals with <c>ax.scatter</c>).
        /// Walked by index: a foreach over the list boxes its enumerator.</summary>
        /// <returns><see langword="true"/> when one does.</returns>
        private bool HoldsScatter()
        {
            for (int i = 0; i < axes.Series.Count; i++)
            {
                if (axes.Series[i] is ScatterSeries { XData.Length: > 0 } or BubbleSeries { XData.Length: > 0 } or Scatter3DSeries { X.Length: > 0 }
                    or PolarScatterSeries { R.Length: > 0 } or StripplotSeries { HasObservations: true } or SwarmplotSeries { HasObservations: true }
                    or ResidualSeries { XData.Length: > 0 }) return true;
            }
            return false;
        }

        /// <summary>
        /// Snapshots what asks for the automatic range of the axes into an array: every series'
        /// <see cref="DataRangeContribution"/>, evaluating <c>ComputeDataRange</c> exactly once per series, then one
        /// contribution for each overlay that ranges the axes (<see cref="AddOverlayContributions"/>). The patches add
        /// theirs beside it (<see cref="PatchContributions"/>), where the theme lets them. Critical for
        /// performance: histogram and KDE series rebuild bins on every call, and the legacy code in
        /// <see cref="CartesianAxisPlacement.ComputeDataRanges"/> called them three times per render
        /// (once for aggregation, once for sticky clamp, once for sticky-flag collection). A series whose share depends on the
        /// theme (<see cref="IThemedRange"/>) is asked under it.
        /// </summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        public DataRangeContribution[] SnapshotContributions(Theme theme)
        {
            var context = new AxesContextAdapter(axes);
            var result = new List<DataRangeContribution>(axes.Series.Count + axes.ReferenceLines.Count);
            for (int i = 0; i < axes.Series.Count; i++)
                result.Add(axes.Series[i].DataRangeUnder(context, theme));
            axes.AddOverlayContributions(result, theme);
            return [.. result];
        }

        /// <summary>What the patches of the axes add to its range: one contribution for each, over the true extent of its outline
        /// (<see cref="PathSegmentExtensions.ExtremePoints"/>: its vertices and where a curve turns back, not a curve's control
        /// points), as matplotlib's <c>add_patch</c> updates the data limits. A coordinate its axis cannot place (0 or less on a
        /// log axis) adds nothing on that axis, the rule a series' values follow, and a patch that cannot be drawn adds nothing. A
        /// patch carries no sticky edge, so the range is padded beyond it like any data. Under a theme that ranges patches as
        /// <c>add_patch</c> does (<see cref="AxisRangeLook.PatchesLikeAddPatch"/>), a rectangle with neither width nor height adds
        /// nothing, as matplotlib's <c>_update_patch_limits</c> returns for it. Whether the patches take part in the range of an axis
        /// at all is the caller's to decide.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The contributions; none for an axes without patches (almost every axes: no list is made).</returns>
        private DataRangeContribution[] PatchContributions(Theme theme)
        {
            if (axes.Patches.Count == 0)
                return [];
            var result = new DataRangeContribution[axes.Patches.Count];
            double xLowest = axes.XAxis.Scale.Rule.LowestPlaced;
            double yLowest = axes.YAxis.Scale.Rule.LowestPlaced;
            int count = 0;
            for (int i = 0; i < axes.Patches.Count; i++)
            {
                var patch = axes.Patches[i];
                if (!theme.Looks.Range.Counts(patch))
                    continue;
                var x = Range1D.Empty;
                var y = Range1D.Empty;
                foreach (var point in patch.Outline().ExtremePoints())
                {
                    x = x.IncludingFrom(point.X, xLowest);
                    y = y.IncludingFrom(point.Y, yLowest);
                }

                result[count++] = DataRangeContribution.Spanning(x.OrNone(), y.OrNone());
            }

            return count == result.Length ? result : result[..count];
        }

        /// <summary>What the patches of every axes this one shares the scale <paramref name="isX"/> names with add to the range of that
        /// scale, nearest first, each axes once, as its series are (<see cref="LinkedContributions"/>).</summary>
        /// <param name="isX">Which link to follow from an axes: the one it shares its X scale with, else its Y scale.</param>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The contributions; none for an axes that shares no scale.</returns>
        private DataRangeContribution[] LinkedPatchContributions(bool isX, Theme theme)
        {
            if (axes.SharesScaleAlong(isX) is null)
                return [];
            var result = new List<DataRangeContribution>();
            var visited = new HashSet<Axes> { axes };
            for (var cur = axes.SharesScaleAlong(isX); cur is not null && visited.Add(cur); cur = cur.SharesScaleAlong(isX))
                result.AddRange(cur.PatchContributions(theme));
            return [.. result];
        }

        /// <summary>Adds to <paramref name="result"/> what the overlays of the axes add to its range. Under every theme, the lines drawn
        /// across the plot at a data value, each as that value on its own axis: a reference line (<see cref="Axes.AxHLine"/> on the Y axis,
        /// <see cref="Axes.AxVLine"/> on the X axis), a horizontal level and the levels of a Fibonacci retracement, as matplotlib's
        /// <c>axhline</c> and <c>axvline</c> update the data limits along their own axis. Under the matplotlib themes also a span, its two
        /// edges on its axis (<c>axvspan</c> and <c>axhspan</c> add their rectangle to the data limits, <c>axes/_axes.py</c> 1099-1106 and
        /// 1036-1043), unless an edge is no value (<see cref="SpanRegion.EdgesStandAtValues"/>), and a trend line, its two points on both
        /// axes (<c>axline</c>, 975-990), a point with a coordinate that is not finite left out whole, as <c>update_datalim</c> leaves it.
        /// The library's own themes clip a span to the range and draw a trend line over it. A value the axis cannot place (a missing value,
        /// an infinity, 0 or less on a log axis) adds nothing, the rule a series' values follow. An overlay carries no sticky edge. The
        /// lists are walked by index: a foreach over a list read as an interface boxes its enumerator.</summary>
        /// <param name="result">The contributions so far.</param>
        /// <param name="theme">The theme the axes is drawn under.</param>
        private void AddOverlayContributions(List<DataRangeContribution> result, Theme theme)
        {
            // A line that runs along X stands at a value on the Y axis, one that runs along Y at a value on the X axis.
            for (int i = 0; i < axes.ReferenceLines.Count; i++)
                result.Add(axes.OnAxisBetween(!axes.ReferenceLines[i].Orientation.RunsAlongX, axes.ReferenceLines[i].Value, axes.ReferenceLines[i].Value));
            for (int i = 0; i < axes.HorizontalLevels.Count; i++)
                result.Add(axes.OnAxisBetween(isX: false, axes.HorizontalLevels[i].Value, axes.HorizontalLevels[i].Value));
            for (int i = 0; i < axes.FibonacciRetracements.Count; i++)
                for (int j = 0; j < axes.FibonacciRetracements[i].Levels.Count; j++)
                    result.Add(axes.OnAxisBetween(isX: false, axes.FibonacciRetracements[i].Levels[j].Price, axes.FibonacciRetracements[i].Levels[j].Price));

            if (!theme.Looks.Range.AutolimitRule.NamedByTheme)
                return;
            for (int i = 0; i < axes.Spans.Count; i++)
            {
                var span = axes.Spans[i];
                if (span.EdgesStandAtValues)
                    result.Add(axes.OnAxisBetween(!span.Orientation.RunsAlongX, span.Min, span.Max));
            }

            var xRule = axes.XAxis.Scale.Rule;
            var yRule = axes.YAxis.Scale.Rule;
            for (int i = 0; i < axes.Trendlines.Count; i++)
            {
                var trend = axes.Trendlines[i];
                Range1D x = Range1D.Empty, y = Range1D.Empty;
                if (new Point(trend.X1, trend.Y1).IsDrawable()) (x, y) = (x.Including(trend.X1, xRule), y.Including(trend.Y1, yRule));
                if (new Point(trend.X2, trend.Y2).IsDrawable()) (x, y) = (x.Including(trend.X2, xRule), y.Including(trend.Y2, yRule));
                result.Add(DataRangeContribution.Spanning(x.OrNone(), y.OrNone()));
            }
        }

        /// <summary>The values <paramref name="from"/> to <paramref name="to"/> on the X axis when <paramref name="isX"/>, on the Y axis
        /// otherwise, each where that axis can place it.</summary>
        private DataRangeContribution OnAxisBetween(bool isX, double from, double to)
        {
            var rule = axes.AxisAlong(isX).Scale.Rule;
            return Range1D.Empty.Including(from, rule).Including(to, rule).OrNone() is { } r ? DataRangeContribution.Along(isX, r.Lo, r.Hi) : default;
        }

        /// <summary>
        /// The contributions of every axes linked to this one through the scale <paramref name="isX"/> names
        /// (<see cref="Axes.ShareXWith"/> or <see cref="Axes.ShareYWith"/>), nearest first, each axes once. They are what an
        /// axes that shares a scale adds to its own to range, pad and clamp that scale: the range AND the sticky edges, so
        /// two axes that share a scale have the same one. Cycle-safe via a visited set — a reciprocal
        /// <c>ax1.ShareXWith = ax2; ax2.ShareXWith = ax1</c> pair will not infinite-loop.
        /// </summary>
        /// <param name="isX">Which link to follow from an axes: the one it shares its X scale with, else its Y scale.</param>
        /// <param name="theme">The theme the axes is drawn under.</param>
        public DataRangeContribution[] LinkedContributions(bool isX, Theme theme)
        {
            // Almost every axes shares no scale; it then needs no list and no visited set, and asks nothing of a hash.
            if (axes.SharesScaleAlong(isX) is null)
                return [];
            var result = new List<DataRangeContribution>();
            var visited = new HashSet<Axes> { axes };
            for (var cur = axes.SharesScaleAlong(isX); cur is not null && visited.Add(cur); cur = cur.SharesScaleAlong(isX))
                result.AddRange(cur.SnapshotContributions(theme));
            return [.. result];
        }
    }

    extension(ISeries series)
    {
        /// <summary>What <paramref name="series"/> contributes to the range of the axes it is drawn on under
        /// <paramref name="theme"/>: nothing under the matplotlib themes when it holds no data (<see cref="ChartSeries.HoldsNoData"/>:
        /// matplotlib's data limits hold nothing for it, where the library's own themes range it from 0 to 1), what it says under the theme
        /// when it depends on it (<see cref="IThemedRange"/>), else its <see cref="ISeries.ComputeDataRange"/>.</summary>
        /// <param name="context">The axes the series is drawn on.</param>
        /// <param name="theme">The theme of the figure.</param>
        /// <returns>The contribution.</returns>
        internal DataRangeContribution DataRangeUnder(IAxesContext context, Theme theme) =>
            theme.Looks.Range.AutolimitRule.NamedByTheme && series is ChartSeries { HoldsNoData: true } ? default
            : series is IThemedRange themed ? themed.RangeUnder(context, theme) : series.ComputeDataRange(context);
    }

    extension(DataRangeContribution contribution)
    {
        /// <summary>The smallest low end above 0 of <paramref name="smallest"/> and this contribution's on the X axis
        /// (<paramref name="isX"/>) or the Y axis: one step of <c>SmallestPositive</c>, the rule a fold that keeps no list of its
        /// contributions (an axis past the primary) takes too.</summary>
        /// <param name="smallest">The smallest so far, or <see langword="null"/>.</param>
        /// <param name="isX">Whether the X axis is asked about, else the Y axis.</param>
        /// <returns>The smaller of the two, or the one there is.</returns>
        internal double? SmallestPositiveWith(double? smallest, bool isX) =>
            contribution.MinAlong(isX) is { } low && low > 0 && !(low >= smallest) ? low : smallest;
    }

    extension(IReadOnlyList<DataRangeContribution> contribs)
    {
        /// <summary>Folds every contribution's range on the X axis (<paramref name="isX"/>) or the Y axis into <paramref name="seed"/>.</summary>
        /// <param name="isX">Whether the X axis is folded, else the Y axis.</param>
        /// <param name="seed">The range to fold into.</param>
        /// <returns>The folded range.</returns>
        internal Range1D FoldRange(bool isX, Range1D seed)
        {
            // Walked by index: a foreach over the interface boxes its enumerator, on every draw.
            var running = seed;
            for (int i = 0; i < contribs.Count; i++)
                running = running.Merge(contribs[i].MinAlong(isX), contribs[i].MaxAlong(isX));
            return running;
        }

        /// <summary>True when any contribution holds an extent on the X axis (<paramref name="isX"/>) or the Y axis: the axis is
        /// asked to be ranged.</summary>
        /// <param name="isX">Whether the X axis is asked about, else the Y axis.</param>
        public bool AsksForRange(bool isX)
        {
            // Walked by index: a foreach over the interface boxes its enumerator, on every draw.
            for (int i = 0; i < contribs.Count; i++)
            {
                var c = contribs[i];
                if (c.MinAlong(isX).HasValue || c.MaxAlong(isX).HasValue) return true;
            }

            return false;
        }

        /// <summary>The smallest low end above 0 any contribution reaches on the X axis (<paramref name="isX"/>) or the Y axis:
        /// matplotlib's <c>minpos</c>, where a logit axis puts an end of its range that lies at or past 0 or 1.</summary>
        /// <param name="isX">Whether the X axis is asked about, else the Y axis.</param>
        /// <returns>The value, or <see langword="null"/> when no contribution reaches above 0.</returns>
        internal double? SmallestPositive(bool isX)
        {
            double? smallest = null;
            for (int i = 0; i < contribs.Count; i++)
                smallest = contribs[i].SmallestPositiveWith(smallest, isX);

            return smallest;
        }

        /// <summary>True when any contribution registers a sticky edge on the X axis (<paramref name="isX"/>) or the Y axis.</summary>
        /// <param name="isX">Whether the X axis is asked about, else the Y axis.</param>
        internal bool HasAnySticky(bool isX)
        {
            for (int i = 0; i < contribs.Count; i++)
                if (contribs[i].StickyMinAlong(isX).HasValue || contribs[i].StickyMaxAlong(isX).HasValue) return true;
            return false;
        }
    }
}
