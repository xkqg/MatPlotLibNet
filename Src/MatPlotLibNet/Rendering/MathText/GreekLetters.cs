// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Maps LaTeX Greek letter command names to their Unicode equivalents.</summary>
public static class GreekLetters
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // Lowercase (24)
        ["alpha"]   = "\u03B1", ["beta"]    = "\u03B2", ["gamma"]   = "\u03B3",
        ["delta"]   = "\u03B4", ["epsilon"] = "\u03B5", ["zeta"]    = "\u03B6",
        ["eta"]     = "\u03B7", ["theta"]   = "\u03B8", ["iota"]    = "\u03B9",
        ["kappa"]   = "\u03BA", ["lambda"]  = "\u03BB", ["mu"]      = "\u03BC",
        ["nu"]      = "\u03BD", ["xi"]      = "\u03BE", ["omicron"] = "\u03BF",
        ["pi"]      = "\u03C0", ["rho"]     = "\u03C1", ["sigma"]   = "\u03C3",
        ["tau"]     = "\u03C4", ["upsilon"] = "\u03C5", ["phi"]     = "\u03C6",
        ["chi"]     = "\u03C7", ["psi"]     = "\u03C8", ["omega"]   = "\u03C9",

        // Uppercase (24)
        ["Alpha"]   = "\u0391", ["Beta"]    = "\u0392", ["Gamma"]   = "\u0393",
        ["Delta"]   = "\u0394", ["Epsilon"] = "\u0395", ["Zeta"]    = "\u0396",
        ["Eta"]     = "\u0397", ["Theta"]   = "\u0398", ["Iota"]    = "\u0399",
        ["Kappa"]   = "\u039A", ["Lambda"]  = "\u039B", ["Mu"]      = "\u039C",
        ["Nu"]      = "\u039D", ["Xi"]      = "\u039E", ["Omicron"] = "\u039F",
        ["Pi"]      = "\u03A0", ["Rho"]     = "\u03A1", ["Sigma"]   = "\u03A3",
        ["Tau"]     = "\u03A4", ["Upsilon"] = "\u03A5", ["Phi"]     = "\u03A6",
        ["Chi"]     = "\u03A7", ["Psi"]     = "\u03A8", ["Omega"]   = "\u03A9",
    };

    /// <summary>Returns the Unicode string for the given command name, or <see langword="null"/> if not found.</summary>
    public static string? TryGet(string name) =>
        Map.TryGetValue(name, out var v) ? v : null;
}
