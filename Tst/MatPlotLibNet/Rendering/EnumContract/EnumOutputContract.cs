// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — shared contract helper for rendering-driving enums.
/// Asserts every enum value produces SVG output that is byte-distinct from
/// every other enum value's output, i.e. the renderer actually dispatches
/// on the value instead of silently collapsing to a default.
/// <para>This is the class of bug Phase M.2 uncovered: <c>MarkerStyle</c> had
/// 13 enum members, but only 2 rendered distinctly because the renderer had
/// an unconditional <c>DrawCircle</c> (LineSeriesRenderer) or a single
/// <c>Square</c> branch (ScatterSeriesRenderer). "Property-was-set" tests
/// didn't catch it. This helper does.</para>
/// </summary>
internal static class EnumOutputContract
{
    /// <summary>Renders each enum value via <paramref name="renderSvg"/>,
    /// asserts every pair of outputs differs, and (if supplied) asserts each
    /// value's SVG contains <paramref name="expectedPrimitive"/>(value).
    /// <para>Pass <paramref name="exclude"/> to skip sentinel values that
    /// are legitimately meant to produce nothing (e.g. <c>MarkerStyle.None</c>,
    /// <c>LineStyle.None</c>). They're verified separately via "produces no
    /// relevant primitive" tests in each file.</para></summary>
    public static void EveryValueRendersDistinctOutput<TEnum>(
        Func<TEnum, string> renderSvg,
        Func<TEnum, string>? expectedPrimitive = null,
        IEnumerable<TEnum>? exclude = null) where TEnum : struct, Enum
    {
        var excluded = exclude is null ? new HashSet<TEnum>() : new HashSet<TEnum>(exclude);
        var values = Enum.GetValues<TEnum>().Where(v => !excluded.Contains(v)).ToArray();

        // (1) Each enum value must produce a non-empty SVG.
        var svgs = new Dictionary<TEnum, string>(values.Length);
        foreach (var v in values)
        {
            string svg = renderSvg(v);
            Assert.False(string.IsNullOrEmpty(svg),
                $"{typeof(TEnum).Name}.{v}: renderer returned empty SVG — the contract requires a rendering for every enum value.");
            svgs[v] = svg;
        }

        // (2) If a per-value primitive was declared, its substring must appear.
        if (expectedPrimitive is not null)
        {
            foreach (var v in values)
            {
                string want = expectedPrimitive(v);
                if (string.IsNullOrEmpty(want)) continue;
                Assert.True(svgs[v].Contains(want, StringComparison.Ordinal),
                    $"{typeof(TEnum).Name}.{v}: SVG missing expected primitive '{want}'. " +
                    $"Renderer likely collapsed {v} to a default branch — mirror of the MarkerStyle Phase M.2 bug.");
            }
        }

        // (3) Every pair of distinct enum values must produce DISTINCT SVG.
        // Otherwise the renderer has silently merged branches.
        for (int i = 0; i < values.Length; i++)
        for (int j = i + 1; j < values.Length; j++)
        {
            var a = values[i];
            var b = values[j];
            Assert.True(svgs[a] != svgs[b],
                $"{typeof(TEnum).Name}: value {a} produced byte-identical SVG to {b}. " +
                $"Renderer is not dispatching on the enum — silent-collapse bug.");
        }
    }
}
