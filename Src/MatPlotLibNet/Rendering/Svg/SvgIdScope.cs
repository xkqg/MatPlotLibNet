// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The one owner of the ids a render context writes (clip paths, hatch patterns, gradients). Every subplot is drawn by
/// a context of its own that counts from zero, so the ids of two subplots would be the same unless each carries a prefix of
/// its own: the first subplot keeps the plain ids, so the SVG of a one-plot figure is the same bytes as ever, and each later
/// subplot writes <c>s1-clip-0</c>, <c>s2-hatch-0</c>, and so on.</summary>
/// <param name="Prefix">The text in front of every id; <see langword="null"/> or empty for none.</param>
internal readonly record struct SvgIdScope(string? Prefix)
{
    /// <summary>The scope of the subplot at <paramref name="index"/>: none for the first, <c>s{index}-</c> for the others.</summary>
    internal static SvgIdScope ForSubplot(int index) => new(index == 0 ? null : $"s{index}-");

    /// <summary>The id of clip path number <paramref name="number"/>.</summary>
    internal string Clip(int number) => $"{Prefix}clip-{number}";

    /// <summary>The id of hatch pattern number <paramref name="number"/>.</summary>
    internal string Hatch(int number) => $"{Prefix}hatch-{number}";

    /// <summary>The id of gradient number <paramref name="number"/>.</summary>
    internal string Gradient(int number) => $"{Prefix}grad-{number}";
}
