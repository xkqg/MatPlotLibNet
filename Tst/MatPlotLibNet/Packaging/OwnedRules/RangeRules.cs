// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a data range: what a series adds to the range of its axes (pinned, spanning, with no data), how a flat or
/// an empty range is opened, and whether a value lies on a range.</summary>
internal static class RangeRules
{
    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a range the data fill to every edge is pinned on all four sides by DataRangeContribution.Pinned (OfCells for the cells of a matrix): no series sets the four sticky edges itself",
            @"\bStickyXMin:\s*[^,]+,\s*StickyXMax:\s*[^,]+,\s*StickyYMin:\s*[^,]+,\s*StickyYMax:",
            "Src/MatPlotLibNet/Models/ValueObjects/DataRangeContribution.cs") { Under = "Src/", Finds = [
            "            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);",
            "        return new(0, cols, 0, rows, StickyXMin: 0, StickyXMax: cols, StickyYMin: 0, StickyYMax: rows);",
            "        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi, StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: y?.Lo, StickyYMax: y?.Hi);"] },
        new("the range of an axis that scales itself is opened where it has no width in AxisAutoscale (Opened, UnpaddedInCube): the library's rule under its own themes, the scale's locator under the matplotlib themes; no range code normalises an axis' range itself (a 3-D projection guards the cube it is handed, a sparkline scales its own line)",
            @"\.Normalized\(\)",
            "Src/MatPlotLibNet/Rendering/AxisAutoscale.cs", "Src/MatPlotLibNet/Rendering/Projection3D.cs",
            "Src/MatPlotLibNet/Rendering/SeriesRenderers/XY/SparklineSeriesRenderer.cs") { Under = "Src/", Finds = [
            "range = auto.OnScale(range.Normalized(), contribs.SmallestPositive(isX));",
            "var unpadded = auto.WithExplicitLimits(raw.Normalized());",
            "var y = Range1D.Empty.Merge(lo, hi).Normalized();"], Ignores = [
            "double t = norm.Normalize(value, min, max);",
            "var unpadded = auto.UnpaddedInCube(raw, isZ: false);"] },
        new("what a series that holds no data adds to its range under the library's own themes, 0 to 1 on both axes, is ChartSeries.NoDataUnderTheLibrary, and the matplotlib themes add nothing for it (ChartSeries.HoldsNoData): no series types the default itself",
            @"\bnew(?:\s+DataRangeContribution)?\(0, 1, 0, 1\)",
            "Src/MatPlotLibNet/Models/Series/ChartSeries.cs") { Under = "Src/MatPlotLibNet/Models/Series/", Finds = [
            "        if (X.Length == 0) return new(0, 1, 0, 1);",
            "            : Data.Length == 0 ? new(0, 1, 0, 1) : new(null, null, 0, 1);",
            "        if (X.Length == 0 || Y.Length == 0) return new DataRangeContribution(0, 1, 0, 1);"], Ignores = [
            "        if (HoldsNoData) return NoDataUnderTheLibrary;",
            "        if (X.Length == 0) return new(null, null, null, null);",
            "        if (HoldsNoData) return new(0, 1, -1, 1);"] },
        new("the points a surface fills to every edge (a mesh, a triangulation, the grid of a field or of a contour) are made its pinned range by DataRangeContribution.PinnedOver: no series pins the drawable extent of two arrays itself",
            @"\bPinned\(\s*[\w.]+\.DrawableRange\(\)\s*,\s*[\w.]+\.DrawableRange\(\)\s*\)",
            "Src/MatPlotLibNet/Models/ValueObjects/DataRangeContribution.cs") { Under = "Src/", Finds = [
            "        return DataRangeContribution.Pinned(X.Data.DrawableRange(), Y.Data.DrawableRange());",
            "        return DataRangeContribution.Pinned(X.DrawableRange(), Y.DrawableRange());",
            "        XData.Length == 0 ? default : DataRangeContribution.Pinned(XData.DrawableRange(), YData.DrawableRange());"], Ignores = [
            "    public override DataRangeContribution ComputeDataRange(IAxesContext context) => PinnedOverItsPoints(X.Data, Y.Data);",
            "        return DataRangeContribution.PinnedOver(X, Y);",
            "        return DataRangeContribution.Pinned(extent.Time, extent.Frequency);"] },
        new("the ends of a range along X and one along Y are made a contribution by DataRangeContribution.Spanning (and the points that fill no edge by Over): no series or range code spells the two ranges' ends out itself",
            @"\bnew(?:\s+DataRangeContribution)?\((\w+)\?\.Lo,\s*\1\?\.Hi,\s*(\w+)\?\.Lo,\s*\2\?\.Hi\b",
            "Src/MatPlotLibNet/Models/ValueObjects/DataRangeContribution.cs") { Under = "Src/", Finds = [
            "        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);",
            "                result[count++] = new DataRangeContribution(along?.Lo, along?.Hi, up?.Lo, up?.Hi);",
            "        return new(xr?.Lo, xr?.Hi, yr?.Lo, yr?.Hi, ZMin: z?.Lo, ZMax: z?.Hi);"], Ignores = [
            "        return DataRangeContribution.Spanning(x, y) with { StickyXMin = x?.Lo };",
            "        return new(XStart, XAt(YData.Length - 1), y?.Lo, y?.Hi);",
            "        return new(x?.Lo, x?.Hi, rows.Lo, rows.Hi);"] },
        new("a flat span is opened by half a unit on each side by Range1D.OpenedWhenFlat, at the axis' tolerance: no series, indicator or binning (numpy's edges of a sample of one value) opens its own range by hand, whatever its ends are called",
            @"(?:-=|\+=)\s*0\.5\s*;|\b[\w.]+\s*-\s*0\.5,\s*[\w.]+\s*\+\s*0\.5\b",
            "Src/MatPlotLibNet/Rendering/ValueObjects/Range1D.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "var equal = min == max ? new EqualWidthBins(min - 0.5, max + 0.5, bins) : new EqualWidthBins(min, max, bins);",
            "return new Range1D(xs.Lo - 0.5, xs.Hi + 0.5);",
            "if (low == high) { low -= 0.5; high += 0.5; }"], Ignores = [
            "var (low, high) = new Rendering.Range1D(min, max).OpenedWhenFlat();",
            "double centre = start + 0.5 * width;",
            "double x = Math.Floor(px) + 0.5;"] },
        new("whether a value lies on a range, give or take a ten-billionth of its width (matplotlib's _interval_contains_close, false for NaN), is Range1D.HoldsClose: no site types the tolerance or the two comparisons",
            @"\*\s*1e-10\b|\*\s*(?:Range1D\.)?CloseTolerance\b",
            "Src/MatPlotLibNet/Rendering/ValueObjects/Range1D.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
            "        for (double t = first; t <= max + _base * 1e-10; t += _base)"] },
        OwnedRule.Nowhere("a series that adds nothing to its axes' range returns default, the contribution that holds nothing: no site spells it as four nulls",
            @"\bnew(?:\s+DataRangeContribution)?\(\s*null,\s*null,\s*null,\s*null\s*\)", under: "Src/") with { Finds = [
            "    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);",
            "        if (Nodes.Count == 0) return new(null, null, null, null);",
            "        if (Values.Length == 0) return new DataRangeContribution(null, null, null, null);"], Ignores = [
            "    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;",
            "        if (X.Length == 0) return new(0, 1, 0, 1);",
            "        return new(null, null, 0, 1);"] },
        new("the view of an axes a series' range is asked through is AxesContextAdapter, which keeps the stack of the bars: no package makes a second IAxesContext of its own",
            @"\b(?:class|record|struct)\s+\w+(?:\([^)]*\))?\s*:[^{;]*\b(?:IKeepsBarStack|IAxesContext)\b",
            "Src/MatPlotLibNet/Rendering/AxesContextAdapter.cs") { Under = "Src/", Finds = [
            "    private sealed class AxesSnapshot(Axes axes) : " + "IAxesContext",
            "internal class AxesContextAdapter : IKeepsBarStack"], Ignores = [
            "    private static SeriesSummary Describe(ISeries series, IAxesContext context)",
            "internal interface IKeepsBarStack : " + "IAxesContext"] },
        OwnedRule.Nowhere("whether a patch takes part in the range is asked of the theme's AxisRangeLook (PatchesRange, Counts): no renderer reads its add_patch flag",
            @"\bPatchesLikeAddPatch\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "        if (theme.Looks.Range.PatchesLikeAddPatch) ranges.Add(patch.Extent);"] },
        OwnedRule.Nowhere("the lowest and the highest finite value of a sample is double[].DrawableRange(): no site skips what is not finite in a loop and tracks a minimum and a maximum itself (a fold over points, positions, extents, a window of ticks or a running row is another rule: it keeps no sample's finite values)",
            @"!(?:double\.IsFinite\((\w+)\)|(\w+)\.IsDrawable\(\))\)\s*continue;\s*if\s*\(\s*\w+\s*<\s*\w+\s*\)\s*\w+\s*=\s*\w+;", under: "Src/") with { AcrossLines = true, Finds = [
            "foreach (var v in data)\n{\n    if (!double.IsFinite(v)) continue;\n    if (v < min) min = v;\n    if (v > max) max = v;\n}",
            "for (int k = 0; k < count; k++)\n{\n    double v = data[k];\n    if (!double.IsFinite(v)) continue;\n    if (v < mn) mn = v;\n}",
            "foreach (double radius in R)\n{\n    if (!radius.IsDrawable()) continue;\n    if (radius < min) min = radius;\n}"], Ignores = [
            "foreach (var v in data)\n{\n    if (!double.IsFinite(v)) continue;\n    sum += v;\n}",
            "foreach (double v in data)\n{\n    if (v < dataMin) dataMin = v;\n    if (v > dataMax) dataMax = v;\n}",
            "var range = Range1D.Empty;\nforeach (double value in values)\n{\n    range = range.IncludingFrom(value, lowest);\n}"] },
    ];
}
