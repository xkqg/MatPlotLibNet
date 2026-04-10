// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Maps LaTeX math symbol command names to their Unicode equivalents.</summary>
public static class MathSymbols
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["pm"]      = "\u00B1", // ±
        ["times"]   = "\u00D7", // ×
        ["div"]     = "\u00F7", // ÷
        ["leq"]     = "\u2264", // ≤
        ["geq"]     = "\u2265", // ≥
        ["neq"]     = "\u2260", // ≠
        ["infty"]   = "\u221E", // ∞
        ["approx"]  = "\u2248", // ≈
        ["cdot"]    = "\u22C5", // ⋅
        ["degree"]  = "\u00B0", // °
        ["circ"]    = "\u00B0", // ° (alternate)
        ["in"]      = "\u2208", // ∈
        ["notin"]   = "\u2209", // ∉
        ["subset"]  = "\u2282", // ⊂
        ["supset"]  = "\u2283", // ⊃
        ["cup"]     = "\u222A", // ∪
        ["cap"]     = "\u2229", // ∩
        ["sum"]     = "\u2211", // Σ (display)
        ["prod"]    = "\u220F", // Π (display)
        ["int"]     = "\u222B", // ∫
        ["partial"] = "\u2202", // ∂
        ["nabla"]   = "\u2207", // ∇
        ["forall"]  = "\u2200", // ∀
        ["exists"]  = "\u2203", // ∃
        ["neg"]     = "\u00AC", // ¬
        ["wedge"]   = "\u2227", // ∧
        ["vee"]     = "\u2228", // ∨
        ["sim"]     = "\u223C", // ∼
        ["equiv"]   = "\u2261", // ≡
        ["propto"]  = "\u221D", // ∝
        ["perp"]    = "\u22A5", // ⊥
        ["mid"]     = "\u2223", // ∣
        ["angle"]   = "\u2220", // ∠
        ["sqrt"]    = "\u221A", // √ (simplified — no radical)
        ["ldots"]   = "\u2026", // …
        ["cdots"]   = "\u22EF", // ⋯
        ["to"]      = "\u2192", // →
        ["gets"]    = "\u2190", // ←
        ["leftrightarrow"] = "\u2194", // ↔
        ["uparrow"] = "\u2191", // ↑
        ["downarrow"] = "\u2193", // ↓
    };

    /// <summary>Returns the Unicode string for the given command name, or <see langword="null"/> if not found.</summary>
    public static string? TryGet(string name) =>
        Map.TryGetValue(name, out var v) ? v : null;
}
