// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>A calculation the library needs more than once is written once, in the place that owns it, and read from there: an
/// angle in the other unit, the spread of a run of values, where a value stands in a colour scale, the edge of a distribution's
/// domain, a power of two, a moving average, the solution of a linear system, a bin, a contour's crossing, a spectrum's
/// decibels.</summary>
internal static class NumericsRules
{
/// <summary>A spectrum's decibels: ten times the logarithm of a power, twenty of a magnitude, in DecibelExtensions alone (the power
    /// spectral density verbs, the magnitude spectrum, the spectrogram under the matplotlib themes).</summary>
    private static readonly OwnedRule Decibels = OwnedRule.Nowhere(
        "a spectrum's decibels are DecibelExtensions' (PowerInDecibels, MagnitudeInDecibels, a grid of powers): no site writes ten or twenty times a logarithm itself",
        @"\b(?:10|20)(?:\.0)?\s*\*\s*Math\.Log10\(", under: "Src/") with { Finds = [
        "            psd[i] = 10 * Math.Log10(power[i]);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        Decibels,
        new("the percentile of values in ascending order is SortOrderExtensions.Percentile, the one linear interpolation between the two ranks around (n - 1) p / 100 that a box, a violin and Vec.Percentile read: no site works out the rank or interpolates between two neighbours of a sorted array itself",
            @"\(\s*\w+\.(?:Length|Count)\s*-\s*1\s*\)\s*\*[^;/]*/\s*100(?:\.0)?\b|\w+\[(\w+)\]\s*\+\s*\(\w+\s*-\s*\1\)\s*\*\s*\(\w+\[\w+\]\s*-\s*\w+\[\1\]\)",
            "Src/MatPlotLibNet/Numerics/SortOrderExtensions.cs") { Under = "Src/", Finds = [
            "        double idx = (sorted.Length - 1) * Math.Clamp(p, 0.0, 100.0) / 100.0;",
            "            return sorted[lower] + (idx - lower) * (sorted[upper] - sorted[lower]);"], Ignores = [
            "        double middle = (values.Length - 1) / 2.0;"] },
        new("a stable order of indices from the largest value to the smallest, equal values in the order they are stored, is SortOrderExtensions.DescendingOrder: no site orders indices by their values from the largest itself, as a comparer type, as a comparison of two indices (a sort's lambda, or one a tie then breaks by the index) or as a LINQ ordering of the indices by their values; an order whose tie is broken by another key (a word cloud's words) is another rule",
            @":\s*IComparer<int>|Comparer<int>\.Create\(|=\s*(\w+)\[b\]\.CompareTo\(\1\[a\]\)\s*;|\(\s*(\w+)\s*,\s*(\w+)\s*\)\s*=>\s*([\w.]+)\[\3\]\.CompareTo\(\4\[\2\]\)|OrderByDescending\(\s*(\w+)\s*=>\s*[\w.]+\[\5\]\s*\)(?!\s*\.ThenBy\(\s*\w+\s*=>\s*[\w.]+\[)",
            "Src/MatPlotLibNet/Numerics/SortOrderExtensions.cs") { Under = "Src/", Finds = [
            "    private readonly struct LargestFirst(double[] values) : IComparer<int>",
            "            int byValue = values[b].CompareTo(values[a]);",
            "            kv.Value.Sort((a, b) => nodeValues[b].CompareTo(nodeValues[a]));",
            "        Array.Sort(order, (x, y) => w.Values[y].CompareTo(w.Values[x]));",
            "        var largestFirst = Comparer<int>.Create((p, q) => q.CompareTo(p));",
            "        return [.. Enumerable.Range(0, ribbons.Count).OrderByDescending(i => sizes[i]).Select(i => ribbons[i])];",
            "            foreach (int i in Enumerable.Range(0, values.Length).OrderByDescending(i => remainders[i]).ThenBy(i => i).Take(seats - handedOut))",
            "        foreach (int i in Enumerable.Range(0, scale.Ticks.Length).OrderByDescending(i => scale.Fractions[i]))"], Ignores = [
            "                int byCentre = items[a].Centre.CompareTo(items[b].Centre);",
            "        Array.Sort(idx, (a, b) => v[a].CompareTo(v[b]));",
            "        int[] order = [.. Enumerable.Range(0, count).OrderByDescending(i => spec.Weights[i]).ThenBy(i => spec.Words[i], StringComparer.Ordinal)];"] },
        OwnedRule.Nowhere("the marching-squares case of a cell and its segments are MarchingSquares' (contourpy's decisions: a corner equal to the level is below, the saddles paired one way): no renderer classifies the corners of a cell itself",
            @"\bcode\s*\|=\s*[1248]\b|>=\s*zLevel\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "                    if (z00 >= zLevel) code |= 1;"] },
        new("an angle in the other unit is value.ToRadians() or value.ToDegrees(): the multiply by pi over 180 is not typed at every site, in either order, and the standard library's own conversion is not called beside it. Left: the Geo package's own public angle block (a different, one-multiply form that every projected number a map's JSON carries is held to: forwarding it to the core's two-operation form moves the last digit of the projected places) and the 3-D rotation script's own degToRad, which is JavaScript text",
            @"Math\.PI\s*\)?\s*/\s*180|\b180(?:\.0)?\s*\)?\s*/\s*Math\.PI|\*\s*180(?:\.0)?\s*/\s*Math\.PI|\bDegreesPerRadian\b|\b(?:DegreesToRadians|RadiansToDegrees)\b",
            "Src/MatPlotLibNet/Extensions/AngleExtensions.cs", "Src/MatPlotLibNet.Geo/Extensions/AngleExtensions.cs",
            "Src/MatPlotLibNet/Transforms/SvgTransform.Svg3DRotationScript.cs") { Finds = [
            "            double step = sweep / pieces * Math.PI / 180.0;"] },
        new("a sum of squared deviations about a mean is MomentsExtensions' Variance and StdDev (ddof 0 for a population, 1 for a sample), whose in-order kernel is the one place it is accumulated: the only other squared sums are a Euclidean distance, a signal's energy, a fit's residual sum and a detrend's regression sum",
            @"\+=\s*(\w+)\s*\*\s*\1\s*;",
            "Src/MatPlotLibNet/Numerics/MomentsExtensions.cs", "Src/MatPlotLibNet/Numerics/MatrixExtensions.cs", "Src/MatPlotLibNet/Numerics/Correlation.cs",
            "Src/MatPlotLibNet/Numerics/LeastSquares.cs", "Src/MatPlotLibNet/Numerics/SpectralDetrend.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "                sum += diff * diff;"] },
        OwnedRule.Nowhere("the standard deviation is MomentsExtensions' StdDev: the square root of a hand-made sum of squares over a count is not typed again",
            @"Math\.Sqrt\(\s*(?:var\w*|sumSq\w*|sqSum\w*|m2|ss|sumSquares|squares|varSum)\s*/",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "        return Math.Sqrt(m2 / row.Length);"] },
        new("a range of no width, a half of a centred scale with no width and a list of boundaries with no bin each map to the middle of the colour map (0.5) in one place: RangeFractionExtensions for the range, INormalizer.cs for the centred half and the empty boundary list",
            @"==\s*0\)\s*return\s*0\.5|==\s*0\s*\?\s*0\.5|nBins\s*<=\s*0\)\s*return\s*0\.5",
            "Src/MatPlotLibNet/Styling/ColorMaps/RangeFractionExtensions.cs", "Src/MatPlotLibNet/Styling/ColorMaps/INormalizer.cs")
        { Under = "Src/MatPlotLibNet/Styling/ColorMaps/", Finds = [
            "            if (range == 0) return 0.5;"] },
        new("a distribution function's input is judged by DistributionDomainExtensions: a shape that is NaN or not above zero is undefined, a probability at either end saturates there",
            @"IsNaN\(\w+\)\s*\|\|[^;]*<=\s*0|\b(?:p|probability)\s*(?:<=\s*0|>=\s*1)\b",
            "Src/MatPlotLibNet/Numerics/DistributionDomainExtensions.cs") { Under = "Src/MatPlotLibNet/Numerics/", Finds = [
            "        if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(x) || a <= 0 || b <= 0) return double.NaN;"] },
        OwnedRule.Nowhere("a power of two is int.IsPow2 and its logarithm int.Log2: the bit trick and the logarithm cast are not typed at every site",
            @"\(\s*\w+\s*&\s*\(\s*\w+\s*-\s*1\s*\)\s*\)\s*[!=]=\s*0|\(int\)\s*Math\.Log2\(",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "        if (window < 4 || (window & (window - 1)) != 0)"] },
        OwnedRule.Nowhere("a simple moving average of a derived series is RollingMean: the seed loop and the add-new, drop-old running sum are not typed again in an indicator",
            @"\bsum\s*\+=\s*\w+\[[^\]]+\]\s*-\s*\w+\[[^\]]+\]",
            under: "Src/MatPlotLibNet/Indicators/") with { Finds = [
            "            sum += illiq[i + _period - 1] - illiq[i - 1];"] },
        new("the largest-magnitude pivot of a column is searched by Linalg's LU and nowhere else: a linear system has one solver",
            @"Math\.Abs\((?:aug|lu)\[[^\]]+\]\)\s*>\s*(?:maxVal|Math\.Abs\()",
            "Src/MatPlotLibNet/Numerics/Linalg.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "                if (Math.Abs(aug[row, col]) > Math.Abs(aug[pivot, col])) pivot = row;"] },
        new("a pivot is judged singular by one tolerance, Linalg's SingularPivot: no other file types the literal in a magnitude test",
            @"Math\.Abs\([^)]*\)\s*[<>]\s*1e-1[45]\b",
            "Src/MatPlotLibNet/Numerics/Linalg.cs") { Under = "Src/MatPlotLibNet/Numerics/", Finds = [
            "            if (Math.Abs(diag) < 1e-14) continue;"] },
        new("the equal bin a value falls in is EqualWidthBins' IndexOf, numpy's rule (an estimate off the value's share of the range, settled by the linspace edges): no histogram, density grid or discretisation casts a share of a range to a bin itself",
            @"\(int\)\s*(?:Math\.(?:Floor|Clamp)\()?\(\s*\(?\s*[\w.\[\]]+\s*-\s*[\w.\[\]]+\s*\)\s*[/*]",
            "Src/MatPlotLibNet/Numerics/EqualWidthBins.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            int b = (int)((values[i] - min) * invWidth);"] },
        new("the edge of a bin is its bins' own: HistogramBins.EdgeAt for the bins a histogram draws, EqualWidthBins.EdgeAt for the bins it counts in; no renderer or table steps from the first edge by the width itself",
            @"\b\w*[Mm]in\s*\+\s*\(?\s*\(?\w+(?:\.\w+)?(?:\s*\+\s*1\))?\s*\*\s*(?:\w+\.)?(?:BinWidth|d[xy])\b|\bx1\s*\+\s*\w+\.BinWidth\b",
            "Src/MatPlotLibNet/Models/ValueObjects/HistogramBins.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            rows[i] = [DataCell.FromNumber(bins.Min + (i * bins.BinWidth)), DataCell.FromNumber(bins.Counts[i])];"] },
        OwnedRule.Nowhere("evenly spaced values over a range are numpy's linspace, EqualWidthBins.Edges: no grid, fit or profile steps from its low end by a share of the range, or by a step of the range over the count less one, itself",
            @"\b\w+\s*\+\s*\(\s*\w+\s*-\s*\w+\s*\)\s*\*\s*\w+\s*/\s*\(?\s*\w+|\bstep\s*=\s*\(\s*\w+\s*-\s*\w+\s*\)\s*/\s*\(\s*\w+\s*-\s*1\s*\)",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "        double[] edges = [.. Enumerable.Range(0, _bins + 1).Select(k => low + (high - low) * k / _bins)];"] },
        new("where a contour's level crosses the edge between two corners is MarchingSquares.CrossingAt, for a grid, a triangulation and a 3-D contour alike: no renderer divides the level's distance from a corner by the edge's rise itself",
            @"\(\s*level\s*-\s*\w+\s*\)\s*/",
            "Src/MatPlotLibNet/Numerics/MarchingSquares.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        double t = (vb - va) == 0 ? 0.5 : (level - va) / (vb - va);"] },
        new("a sample's place in a cell of a pair grid (its share of each variable's span, laid over the cell) is PairGridCellFrame.Map: no painter lays a share over a cell itself",
            @"\b[Cc]ell\.(?:X|Bottom)\s*[-+]\s*\(",
            "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/PairGridCellFrame.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/", Finds = [
            "        double px = cell.X      + (x - xMin) / xSpan * cell.Width;"] },
    ];
}
