// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>An argument the library refuses is refused by the owner of its rule, in its words and with one exception type: arrays
/// that do not agree in length, a number that is not finite or outside its range (an opacity outside 0 to 1 included), a value that
/// is no member of its enum.</summary>
internal static class RefusalRules
{
/// <summary>Whether arrays agree in length (one value per point, one label per row, one callback per subplot, every row of a table
    /// as long as the first, a function's result one value per sample) is <c>ArrayLengthExtensions</c>' to refuse, in its words: no site
    /// compares two lengths (an array's <c>Length</c>, a list's <c>Count</c>, a grid's <c>GetLength</c>) and throws itself, under an
    /// <c>if</c>, a ternary or a test of every row. A compare that only branches (an array of another length handed on as it is, a
    /// cache sized again) is not a refusal and is not read; a matrix's <c>Rows</c> and <c>Cols</c> are <c>Mat</c>'s shape, the
    /// conformability of a product, not the length of an array.</summary>
    private static readonly OwnedRule LengthsAgree = new(
        "whether arrays agree in length (one value per point, one label per row, every row as long as the first, a function's result one value per sample) is ArrayLengthExtensions' to refuse, in its words: no site compares two lengths and throws itself",
        @"\bif\s*\([^;{}]*?(?:\.(?:Length|Count)\b|\bGetLength\(\s*\d\s*\))\s*!=[^;{}]*?\)\s*\{?\s*throw\b|\bif\s*\([^;{}]*?!=\s*[\w.\[\]()]*(?:\.(?:Length|Count)\b|\bGetLength\(\s*\d\s*\))[^;{}]*?\)\s*\{?\s*throw\b|(?:\.(?:Length|Count)\b|\bGetLength\(\s*\d\s*\))\s*==\s*[^;]*?\?[^;]*?:\s*throw\b|\.Any\([^;]*?(?:\.(?:Length|Count)\b|\bGetLength\(\s*\d\s*\))\s*!=[^;]*?\)\s*\)\s*\{?\s*throw\b",
        "Src/MatPlotLibNet/Extensions/ArrayLengthExtensions.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "if (x.Length != y.Length)\n    throw new ArgumentException(\"X and Y spans must have equal length.\");",
        "if (kinds.Length != dates.Length)\n{\n    throw new ArgumentException(\"one kind per date\", nameof(kinds));\n}",
        "if (y.Length != x.Length) throw new ArgumentException(\"differ\");",
        "if (configures.Length != rows * cols)\n    throw new ArgumentException(\"one per subplot\");",
        "if (names.Length != flows.GetLength(0))\n    throw new ArgumentException(\"names\");",
        "if (spec.Words.Count != spec.Weights.Count)\n    throw new ArgumentException(\"weights\");",
        "if (_signalCount != 0 && input.Length != _signalCount)\n    throw new ArgumentException(\"rows\");",
        "if (weights is null || weights.Length != n)\n{\n    throw new InvalidOperationException(\"weights\");\n}",
        "if (n != column.Length) throw new ArgumentException(\"columns\");",
        "categories.Length == values.Length && x.Length == values.Length\n    ? new BarSeries(categories, values)\n    : throw new ArgumentException(\"bars\");",
        "if (Data is { Count: > 0 } rows && rows.Any(row => row.Count != rows[0].Count))\n{\n    throw new ArgumentException(\"rows\", \"data\");\n}",
        "if (durations.Length != observed.Length)\n{\n    throw Refused(\"flags\");\n}"], Ignores = [
        "if (values.Length != y.Length)\n{\n    return new(values, y);\n}",
        "if (_x.Length != Count)\n{\n    Array.Resize(ref _x, Count);\n}",
        "if (anchor < 0 || anchor >= Prices.Length)\n    throw new ArgumentOutOfRangeException(nameof(anchor));",
        "if (values.Length == 0)\n    throw new ArgumentException(\"empty\");",
        "if (a.Cols != b.Rows)\n    throw new ArgumentException(\"Cannot multiply\");"] };

/// <summary>Whether a number is finite (not NaN, not an infinity), alone or with a range (finite and 0 or more, finite and above 0,
    /// finite and other than 0, missing or finite and at least 1), is <c>ArgumentRangeExtensions</c>' to refuse, in its words and with its
    /// one exception type (R52): no site tests a number for finiteness, by <c>double.IsFinite</c>, <c>IsInfinity</c>, an infinity
    /// compared, or a predicate built on them (<c>IsPositiveFinite</c>, <c>IsDrawable</c>), and throws itself, under an
    /// <c>if</c>, a ternary or a test of every value. The result of a function the caller handed over is <c>ArrayLengthExtensions</c>'
    /// (<c>ThrowIfNotGivenFor</c>: one finite value per sample). A test that only branches (a value left out, a fallback) is not a
    /// refusal and is not read; a bare NaN test is not either (it refuses what is missing and lets an infinity pass: another rule).</summary>
    private static readonly OwnedRule FiniteNumber = new(
        "whether a number is finite, alone or with a range, is ArgumentRangeExtensions' to refuse (a function's result, ArrayLengthExtensions.ThrowIfNotGivenFor's), in its words and with one exception type: no site tests finiteness and throws itself",
        @"\b(?:IsFinite|IsInfinity|IsPositiveInfinity|IsNegativeInfinity|IsRealNumber|IsPositiveFinite|IsDrawable|IsPart)\b[^;{}]*?(?:\)\s*\{?\s*throw\b|[?:]\s*throw\b)"
        + @"|[=!]=\s*double\.(?:Positive|Negative)Infinity\b[^;{}]*?(?:\)\s*\{?\s*throw\b|[?:]\s*throw\b)"
        + @"|\bIsInfinity\([^()]*\)\s*\?\s*""[^""]*""[^;]*;\s*if\s*\([^)]*\)\s*\{?\s*throw\b",
        "Src/MatPlotLibNet/Numerics/ArgumentRangeExtensions.cs", "Src/MatPlotLibNet/Extensions/ArrayLengthExtensions.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "if (!double.IsFinite(location))\n    throw new ArgumentOutOfRangeException(nameof(location), location, \"finite\");",
        "if (!double.IsFinite(scale) || scale == 0)\n{\n    throw new ArgumentOutOfRangeException(nameof(scale));\n}",
        "if (!(dpi > 0) || double.IsInfinity(dpi))\n{\n    throw new ArgumentOutOfRangeException(nameof(dpi));\n}",
        "set => GivenEdgeWidth = double.IsFinite(value) && value >= 0\n    ? value\n    : throw new ArgumentOutOfRangeException(nameof(value));",
        "bound is { } value && !double.IsFinite(value)\n    ? throw new ArgumentOutOfRangeException(name)\n    : bound;",
        "if (readAt.Length == 0 || readAt.Any(time => !double.IsFinite(time)))\n{\n    throw new ArgumentException(\"times\");\n}",
        "if (!brickSize.IsPositiveFinite())\n{\n    throw new ArgumentOutOfRangeException(nameof(brickSize));\n}",
        "if (baseline is { } level && !level.IsDrawable())\n{\n    throw new ArgumentException(\"baseline\");\n}",
        "if (!part.IsPart())\n{\n    throw new ArgumentOutOfRangeException(\"part\");\n}",
        "if (!(durations[i] >= 0 && double.IsFinite(durations[i])))\n{\n    throw Refused(\"duration\");\n}",
        "if (!double.IsFinite(weight))\n{\n    throw new InvalidOperationException(\"weight\");\n}",
        "if (x == double.PositiveInfinity) throw new ArgumentException(\"x\");",
        "string? reason = double.IsNaN(flow) ? \"not a number\" : double.IsInfinity(flow) ? \"infinite\" : flow < 0 ? \"negative\" : null;\nif (reason is not null)\n    throw new ArgumentException(reason, nameof(flows));"], Ignores = [
        "if (!value.IsPositiveFinite()) continue;",
        "double h = bandwidth is { } given && given.IsPositiveFinite() ? given : Bandwidth(sorted);",
        "if (!double.IsFinite(v))\n{\n    return double.NaN;\n}",
        "double[] means = [.. subgroups.Select(s => s.All(double.IsFinite) ? s.Sum() / size : double.NaN)];",
        "if (double.IsNaN(shares.Ca))\n    throw new ArgumentException(\"no cations\");",
        "JsonTokenType.String when named && reader.ValueTextEquals(\"Infinity\"u8) => double.PositiveInfinity,"] };

    /// <summary>Whether a collection a verb needs at least one of holds none, or a matrix has no row or no column, is
    /// <c>ArrayLengthExtensions</c>' to refuse, with one exception type and the sentence the site gives it (what is missing and what
    /// the chart draws from it): no site compares a length, or the rows or columns of a matrix, with zero and throws an
    /// <see cref="ArgumentException"/> itself, under an <c>if</c>. A state that is not an argument (a builder with
    /// no tile, a pair grid read from a document that holds none) is an <see cref="InvalidOperationException"/> and not read.</summary>
    private static readonly OwnedRule NothingGiven = new(
        "whether a collection a verb needs at least one of holds none, or a matrix has no row or no column, is ArrayLengthExtensions' to refuse (items.ThrowIfEmpty(sentence), matrix.ThrowIfNoCell(what)), with one exception type: no site compares a length, a count or a row or column count with zero and throws an ArgumentException itself",
        @"\bif\s*\((?:[^;{}]*?(?:\.(?:Length|Count)|\.GetLength\(\d\)|\b(?:rows|cols|columns|Rows|Cols|Columns))\s*(?:==\s*0|<\s*1|<=\s*0)|[^;{}]*?\bis\s*\{\s*(?:Length|Count):\s*0\s*\}|\s*!\s*[\w.]+\.Any\(\))[^;{}]*?\)\s*\{?\s*throw\s+new\s+ArgumentException\("
        + @"|\bif\s*\(\s*[\w.]+\s+is\s+not\s*\{\s*(?:Length|Count):\s*>\s*0\s*\}\s*\w*\s*\)\s*\{?\s*throw\s+new\s+ArgumentException\("
        + @"|\bis\s*\{\s*(?:Length|Count):\s*>\s*0\s*\}[^;]*?:\s*throw\s+new\s+ArgumentException\(",
        "Src/MatPlotLibNet/Extensions/ArrayLengthExtensions.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "if (groups.Count == 0)\n{\n    throw new ArgumentException(\"No groups were given.\", nameof(groups));\n}",
        "if (columns.Length == 0) throw new ArgumentException(\"At least one column must be specified.\", nameof(columns));",
        "int rows = matrix.GetLength(0), columns = matrix.GetLength(1);\nif (rows == 0 || columns == 0)\n{\n    throw new ArgumentException(\"no cell\", nameof(matrix));\n}",
        "if (matrix.GetLength(1) == 0) throw new ArgumentException(\"no cell\", nameof(matrix));",
        "if (x is null || x.Length == 0) throw new ArgumentException(\"x\", nameof(x));",
        "if (!series.Any())\n    throw new ArgumentException(\"No series.\", nameof(series));",
        "if (frames is { Length: 0 })\n{\n    throw new ArgumentException(\"frames\");\n}",
        "if (pictures.Count < 1) throw new ArgumentException(\"A step-through SVG needs at least one frame.\", nameof(frames));",
        "if (Colors is not { Length: > 0 } colors)\n{\n    throw new ArgumentException(\"needs a colour\", \"colors\");\n}",
        "Colors is { Length: > 0 } colors\n    ? new ListedColorMap(colors) { UnderColor = UnderColor }\n    : throw new ArgumentException(\"needs a colour\", \"colors\");"], Ignores = [
        "if (Colors is not { Length: > 0 } colors)\n{\n    throw new InvalidOperationException(\"needs a colour\");\n}",
        "var shown = Title is { Length: > 0 } title ? title : \"Untitled\";",
        "if (_tiles.Count == 0)\n{\n    throw new InvalidOperationException(\"An ops dashboard needs at least one tile.\");\n}",
        "if (values.Length == 0)\n{\n    return default;\n}",
        "if (x.Length == 0 || y.Length == 0)\n    return Array.Empty<double>();",
        "if (rows == 0 || cols == 0) return default;",
        "if (rows == 0 || columns == 0)\n{\n    throw new InvalidOperationException(\"no cell\");\n}",
        "if (values.Length != y.Length) throw new ArgumentException(\"differ\");",
        "if (durations.Length == 0) throw Refused(\"has no subjects; a survival curve is estimated from 1 or more.\");"] };

    /// <summary>Whether a value is outside the range it may have (at least 1, from 0 to 10, an index of a list, a power of ten) is
    /// <c>ArgumentRangeExtensions</c>' to refuse, and whether it is a member of its enum is <c>EnumMemberExtensions</c>', in the
    /// sentence the site gives and as the only two places that make the <see cref="ArgumentOutOfRangeException"/>: no site makes one itself, nor asks the library's own <c>ThrowIfLessThan</c>,
    /// <c>ThrowIfNegative</c> or <c>ThrowIfGreaterThanOrEqual</c> (another sentence for the same rule). A site tells the bounds
    /// (<c>ThrowIfBelow</c>, <c>ThrowIfNotIn</c>, <c>ThrowIfNotIndexOf</c>) or the condition (<c>ThrowIfNot</c>, <c>NotAMember</c>) and
    /// the owner makes the refusal.</summary>
    private static readonly OwnedRule OutOfRange = new(
        "whether a value is outside the range it may have is ArgumentRangeExtensions' to refuse, whether it is a member of its enum EnumMemberExtensions', each in one sentence: no site makes an ArgumentOutOfRangeException or asks the library's ThrowIfLessThan, ThrowIfNegative, ThrowIfGreaterThan... itself",
        @"new\s+ArgumentOutOfRangeException\b|\bArgumentOutOfRangeException\s*\.\s*ThrowIf\w+",
        "Src/MatPlotLibNet/Numerics/ArgumentRangeExtensions.cs", "Src/MatPlotLibNet/Extensions/EnumMemberExtensions.cs") { Under = "Src/", Finds = [
        "throw new ArgumentOutOfRangeException(nameof(bins), bins, \"A volume profile has 1 bin or more.\");",
        "_ => throw new ArgumentOutOfRangeException(nameof(side), side, \"Not an edge of the plot.\"),",
        "ArgumentOutOfRangeException.ThrowIfLessThan(maxCols, 1);",
        "ArgumentOutOfRangeException.ThrowIfNegative(index);",
        "ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _window.Count);"], Ignores = [
        "/// <exception cref=\"ArgumentOutOfRangeException\">The value is below the floor.</exception>",
        "catch (ArgumentOutOfRangeException) { }",
        "value.ThrowIfBelow(1, nameof(bins));",
        "_ => throw side.NotAMember(nameof(side), \"Not an edge of the plot.\"),"] };

    /// <summary>Whether a parameter is below or above a number it may not pass is the owner's to refuse (<c>ThrowIfBelow</c>,
    /// <c>ThrowIfNotIn</c>, <c>ThrowIfNotAbove</c>, <c>ThrowIfNot</c>), with one exception type: no site compares a parameter with a
    /// number written there and throws an <see cref="ArgumentException"/> itself, under an <c>if</c>. A comparison that is another
    /// rule (how many samples a window needs, <see cref="EnoughData"/>'s; the lengths of two arrays) is not read.</summary>
    private static readonly OwnedRule NumberOutsideItsRange = OwnedRule.Nowhere(
        "whether a parameter is below or above a number it may not pass is ArgumentRangeExtensions' to refuse (ThrowIfBelow, ThrowIfNotIn, ThrowIfNotAbove, ThrowIfNot), with one exception type: no site compares it with a number written there and throws an ArgumentException itself",
        @"\bif\s*\(\s*(?:!\s*\(\s*)?(?![\w.]*(?:\.Length|\.Count|Held)\s*[<>])[\w.]+\s*(?:<=?|>=?)\s*-?\d[\d.]*\s*\)*\s*\{?\s*throw\s+new\s+ArgumentException\b|\bif\s*\(\s*[\w.]+\s+is\s+not\s*\(\s*[<>]=?\s*\d",
        "Src/") with { AcrossLines = true, Finds = [
        "if (bands < 1)\n{\n    throw new ArgumentException(\"bands\", nameof(bands));\n}",
        "if (levels < 1)\n    throw new ArgumentException(\"levels\", nameof(levels));",
        "if (!(sigma > 0))\n{\n    throw new ArgumentException(\"spread\", nameof(sample));\n}",
        "if (value <= 0)\n{\n    throw new ArgumentException(\"observation\", nameof(sample));\n}",
        "if (fastLimit is not (> 0 and <= 1))\n    throw new ArgumentException(\"fastLimit\", nameof(fastLimit));"], Ignores = [
        "if (x.Length < needed)\n    throw new ArgumentException(\"too short\");",
        "if (maxLags >= x.Length)\n{\n    throw new ArgumentException(\"lags\");\n}",
        "if (bands < 1)\n{\n    return;\n}",
        "if (positions.Count < 2)\n    throw new ArgumentException(\"At least 2 stops required.\", paramName);",
        "if (seriesHeld > 0)\n{\n    throw new ArgumentException(\"This axis holds a series.\", nameof(transform));\n}",
        "if (values.Length == 0) throw new ArgumentException(\"empty\");"] };

    /// <summary>Whether a value is the index of an item of a list (from 0 to one below the count) is the owner's to refuse
    /// (<c>ThrowIfNotIndexOf</c>), whoever's fault the index is, in one sentence and with one exception type: no site compares an
    /// index with the count of a list under an <c>if</c> and throws an <see cref="ArgumentException"/> itself, written
    /// <c>(uint)i &gt;= (uint)n</c> or <c>i &lt; 0 || i &gt;= n</c>. The same comparison that only branches (a hover that is not over an
    /// axes, a cell off the grid) is not a refusal and is not read.</summary>
    private static readonly OwnedRule IndexOfAList = OwnedRule.Nowhere(
        "whether a value is the index of an item of a list is ArgumentRangeExtensions' to refuse (ThrowIfNotIndexOf), whoever's fault the index is, in one sentence and with one exception type: no site compares an index with a count and throws an ArgumentException itself",
        @"\bif\s*\(\s*(?:\(uint\)\s*[\w.\[\]]+\s*>=?\s*\(uint\)\s*[\w.\[\]()]+|(\w+)\s*<\s*0\s*\|\|\s*\1\s*>=?\s*[\w.\[\]()]+)\s*\)\s*\{?\s*(?:(?!\s*(?:return|continue|break)\b)[^;]*;\s*){0,2}?throw\s+new\s+ArgumentException\b",
        "Src/") with { AcrossLines = true, Finds = [
        "if (node < 0 || node >= nodeCount)\n    throw new ArgumentException(\"link\");",
        "if ((uint)triangles[i] >= (uint)points)\n{\n    string valid = points == 0 ? \"none\" : $\"to {points - 1}\";\n    throw new ArgumentException(valid, nameof(triangles));\n}",
        "if (index < 0 || index > items.Count) throw new ArgumentException(\"index\");"], Ignores = [
        "if (axesIndex < 0 || axesIndex >= figure.SubPlots.Count)\n    return null;",
        "if (idx < 0 || idx >= count) return;\nthrow new ArgumentException(\"x\");",
        "if ((uint)index >= (uint)count)\n    throw new ArgumentOutOfRangeException(nameof(index));",
        "if (x < 0 || y >= count)\n    throw new ArgumentException(\"two different names\");"] };

    /// <summary>A count of items the data holds: an array's or a list's <c>Length</c>/<c>Count</c>, a count named so
    /// (<c>FeatureCount</c>, <c>SegmentLength</c>), a grid's <c>GetLength</c>, a local <c>n</c>, <c>count</c> or <c>held</c>.</summary>
    private const string HeldCount = @"(?:\b\w*(?:Length|Count)\b(?:\(\s*\d*\s*\))?|\b(?:n|count|held)\b)";

    /// <summary>A comparison of a count with the count a parameter needs; <c>&lt; 1</c>, <c>&lt;= 0</c>, <c>&gt; 0</c> and
    /// <c>&gt;= 1</c> (none at all) are <see cref="NothingGiven"/>'s.</summary>
    private const string Needs = @"(?:<(?!=)(?!\s*1\b(?!\s*[-+*/.]))|<=(?!\s*0\b(?!\s*[-+*/.]))|(?<![=-])>(?!=)(?!\s*0\b(?!\s*[-+*/.]))|(?<![=-])>=(?!\s*1\b(?!\s*[-+*/.])))";

    /// <summary>The count on either side of the comparison (<c>x.Length &lt; needed</c>, <c>maxLags &gt;= x.Length</c>).</summary>
    private const string CountComparedWithANeed = "(?:" + HeldCount + @"\s*" + Needs + @"|(?<![<>=!])(?:<=?|(?<![=-])>=?)\s*[\w.]*?" + HeldCount + ")";

    /// <summary>Whether the data holds enough items for what a parameter needs of it (two segments of samples for a coherence, two
    /// signals in a row for a dispersion index, more samples than lags for a correlation, two stops for a colour map), or no more than
    /// it takes (ten features for a turbulence index, a padding no shorter than the signal), is <c>DataCountExtensions</c>' to refuse
    /// (<c>held.ThrowIfFewerThan</c>, <c>held.ThrowIfMoreThan</c>), in one sentence and with one exception type: no site compares a
    /// count with the count it needs and throws an <see cref="ArgumentException"/> itself, under an <c>if</c>, a ternary, a property
    /// pattern or a <c>when</c>. None at all is <see cref="NothingGiven"/>'s, an index against a count <see cref="IndexOfAList"/>'s, two
    /// lengths that differ <see cref="LengthsAgree"/>'s; a comparison that only branches, or a parameter against another with no count
    /// (a low-pass period below the high-pass one), is not read.</summary>
    private static readonly OwnedRule EnoughData = new(
        "whether the data holds enough items for what a parameter needs of it, or no more than it takes, is DataCountExtensions' to refuse (held.ThrowIfFewerThan, held.ThrowIfMoreThan), in one sentence and with one exception type: no site compares a count with the count it needs and throws an ArgumentException itself",
        @"\bif\s*\((?![^;{}]*?(?:\(uint\)|<\s*0\s*\|\|))[^;{}]*?" + CountComparedWithANeed
        + @"[^;{}]*?\)\s*\{?\s*(?:(?!\s*(?:return|continue|break)\b)(?:[^;{}]|\{[^{};]*\})*;\s*){0,2}?throw\s+new\s+ArgumentException\b"
        + "|" + CountComparedWithANeed + @"[^;?:{}]*\?[^;]*?:\s*throw\s+new\s+ArgumentException\b"
        + @"|\bis\s+(?:not\s+)?\{\s*(?:Length|Count):\s*" + Needs + @"[^}]*\}[^;]*?(?:\)\s*\{?\s*|[?:]\s*)throw\s+new\s+ArgumentException\b"
        + @"|\bwhen\s+[^;]*?" + HeldCount + @"\s*" + Needs + @"[^;]*?=>\s*throw\s+new\s+ArgumentException\b",
        "Src/MatPlotLibNet/Numerics/DataCountExtensions.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "if (x.Length < 2 * options.SegmentLength)\n{\n    throw new ArgumentException(\"two segments\", nameof(options));\n}",
        "if (BarCount > 0 && FeatureCount < 2)\n    throw new ArgumentException(\"signals\", nameof(signals));",
        "if (input.Length > MaxFeatures)\n    throw new ArgumentException(\"features\", nameof(input));",
        "else if (source.Length < options.Lag + 2)\n{\n    throw new ArgumentException(\"triples\", nameof(source));\n}",
        "if (sample.Length < atLeast)\n{\n    string count = sample.Length == 1 ? \"1\" : $\"{sample.Length}\";\n    throw new ArgumentException(count, paramName);\n}",
        "if (maxLags >= x.Length)\n{\n    throw new ArgumentException(\"lags\", nameof(options));\n}",
        "if (n < required)\n    throw new ArgumentException(\"levels\", nameof(signal));",
        "if (options.Overlap >= options.SegmentLength)\n{\n    throw new ArgumentException(\"overlap\", nameof(options));\n}",
        "if (options.PadTo != 0 && options.PadTo < signal.Length)\n{\n    throw new ArgumentException(\"padding\", nameof(options));\n}",
        "if (positions.Count < 2)\n    throw new ArgumentException(\"At least 2 stops required.\", paramName);",
        "if (x.Length <= 1) throw new ArgumentException(\"two\");",
        "if (x.Count > 1) throw new ArgumentException(\"one at most\");",
        "if (matrix.GetLength(0) < 2) throw new ArgumentException(\"rows\");",
        "if (items.Count() < 3) throw new ArgumentException(\"few\");",
        "return x.Length >= 2 ? x : throw new ArgumentException(\"few\", nameof(x));",
        "if (x is { Length: < 2 }) throw new ArgumentException(\"few\");",
        "return x is not { Count: >= 3 } ? throw new ArgumentException(\"few\") : x;",
        "_ when values.Length < 2 => throw new ArgumentException(\"few\"),"], Ignores = [
        "if (values.Length == 0) throw new ArgumentException(\"empty\");",
        "if (x.Length < 1) throw new ArgumentException(\"empty\");",
        "if (x.Count <= 0) throw new ArgumentException(\"none\");",
        "if (x is not { Length: > 0 }) throw new ArgumentException(\"given\");",
        "if (index < 0 || index >= items.Count) throw new ArgumentException(\"index\");",
        "if ((uint)i >= (uint)list.Count) throw new ArgumentException(\"i\");",
        "if (x.Length != y.Length) throw new ArgumentException(\"differ\");",
        "if (x.Length < 2)\n{\n    return default;\n}\nthrow new ArgumentException(\"later\");",
        "if (lpPeriod >= hpPeriod)\n    throw new ArgumentException(\"lp\", nameof(lpPeriod));",
        "if (x.Length < 2) throw new InvalidOperationException(\"state\");",
        "if (seriesHeld > 0) throw new ArgumentException(\"held\", nameof(transform));",
        "x.Length.ThrowIfFewerThan(2, nameof(x), Need);"] };

    /// <summary>Whether every value of every group of a list is an amount is the owner's to refuse, once for the list
    /// (<c>groups.ThrowIfAnyNotAmount</c>, which names the group and the place of a value): no site loops over the groups itself to
    /// ask each group's values.</summary>
    private static readonly OwnedRule EveryValueOfEveryGroup = new(
        "whether every value of every group of a list is an amount is BarGroupExtensions' to refuse, once for the list: no site loops over the groups itself to ask each group's values",
        @"\bforeach\s*\(\s*var\s+(\w+)\s+in\s+[\w.]+\s*\)\s*\{?\s*\1\.Values\.ThrowIfAnyNotAmount\(",
        "Src/MatPlotLibNet/Models/Series/Categorical/BarGroupExtensions.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "foreach (var layer in layers)\n    layer.Values.ThrowIfAnyNotAmount(nameof(layers), (layer.Label, x), What);",
        "foreach (var group in ranks)\n{\n    group.Values.ThrowIfAnyNotAmount(nameof(ranks), (group.Label, x), What, least: 1);\n}"], Ignores = [
        "foreach (var layer in layers)\n    layer.Values.ThrowIfNotFinite(nameof(layers));",
        "groups.ThrowIfAnyNotAmount(nameof(groups), places, What);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        LengthsAgree,
        FiniteNumber,
        NothingGiven,
        OutOfRange,
        NumberOutsideItsRange,
        IndexOfAList,
        EnoughData,
        EveryValueOfEveryGroup,
    ];
}
