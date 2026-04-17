// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        ["iint"]    = "\u222C", // ∬
        ["iiint"]   = "\u222D", // ∭
        ["oint"]    = "\u222E", // ∮
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

        // ---- v1.3.0 additions ----

        // Blackboard bold (double-struck)
        ["mathbb{R}"] = "\u211D", // ℝ
        ["mathbb{C}"] = "\u2102", // ℂ
        ["mathbb{Z}"] = "\u2124", // ℤ
        ["mathbb{N}"] = "\u2115", // ℕ
        ["mathbb{Q}"] = "\u211A", // ℚ

        // Additional arrows
        ["Rightarrow"]     = "\u21D2", // ⇒
        ["Leftarrow"]      = "\u21D0", // ⇐
        ["Leftrightarrow"] = "\u21D4", // ⇔
        ["mapsto"]         = "\u21A6", // ↦
        ["hookrightarrow"] = "\u21AA", // ↪
        ["hookleftarrow"]  = "\u21A9", // ↩

        // Additional relations
        ["ll"]     = "\u226A", // ≪
        ["gg"]     = "\u226B", // ≫
        ["cong"]   = "\u2245", // ≅
        ["simeq"]  = "\u2243", // ≃
        ["models"] = "\u22A8", // ⊨
        ["prec"]   = "\u227A", // ≺
        ["succ"]   = "\u227B", // ≻

        // Additional binary operators
        ["otimes"]  = "\u2297", // ⊗
        ["oplus"]   = "\u2295", // ⊕
        ["star"]    = "\u22C6", // ⋆
        ["ast"]     = "\u2217", // ∗
        ["bullet"]  = "\u2219", // ∙
        ["diamond"] = "\u22C4", // ⋄
        ["mp"]      = "\u2213", // ∓
        ["dagger"]  = "\u2020", // †

        // Set / logic operators
        ["emptyset"] = "\u2205", // ∅
        ["setminus"] = "\u2216", // ∖
        ["lnot"]     = "\u00AC", // ¬ (alias)
        ["land"]     = "\u2227", // ∧ (alias)
        ["lor"]      = "\u2228", // ∨ (alias)

        // Miscellaneous
        ["hbar"]   = "\u210F", // ℏ
        ["ell"]    = "\u2113", // ℓ
        ["Re"]     = "\u211C", // ℜ
        ["Im"]     = "\u2111", // ℑ
        ["aleph"]  = "\u2135", // ℵ
        ["wp"]     = "\u2118", // ℘
        ["prime"]  = "\u2032", // ′
        ["dprime"] = "\u2033", // ″
    };

    /// <summary>Returns the Unicode string for the given command name, or <see langword="null"/> if not found.</summary>
    public static string? TryGet(string name) =>
        Map.TryGetValue(name, out var v) ? v : null;
}
