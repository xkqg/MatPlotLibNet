// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides the five Seaborn perceptually-uniform colormaps ported to matplotlib 3.x:
/// <c>rocket</c>, <c>mako</c>, <c>crest</c>, <c>flare</c>, and <c>icefire</c>.
/// All reversed variants (<c>*_r</c>) are registered automatically.
/// </summary>
public static class PerceptualColorMaps2
{
    /// <summary>
    /// Sequential — near-black (dark purple) through magenta and salmon to warm cream.
    /// Perceptually uniform; well-suited for heatmaps and density plots.
    /// </summary>
    public static IColorMap Rocket { get; } = new LinearColorMap("rocket",
    [
        Color.FromHex("#03051A"),
        Color.FromHex("#520956"),
        Color.FromHex("#9E1269"),
        Color.FromHex("#CC3E5B"),
        Color.FromHex("#E87B62"),
        Color.FromHex("#F5C884"),
        Color.FromHex("#FAEDC8"),
    ]);

    /// <summary>
    /// Sequential — near-black through deep navy and teal to light mint.
    /// Perceptually uniform cool variant; pairs well with <see cref="Rocket"/>.
    /// </summary>
    public static IColorMap Mako { get; } = new LinearColorMap("mako",
    [
        Color.FromHex("#0B0405"),
        Color.FromHex("#1A2E6E"),
        Color.FromHex("#1C7290"),
        Color.FromHex("#2AAA8A"),
        Color.FromHex("#84CF7D"),
        Color.FromHex("#C5E8A6"),
        Color.FromHex("#DEF5E5"),
    ]);

    /// <summary>
    /// Sequential — light blue-green to deep navy.
    /// Lower contrast than <see cref="Mako"/>; suited for subtle background gradients.
    /// </summary>
    public static IColorMap Crest { get; } = new LinearColorMap("crest",
    [
        Color.FromHex("#E8F8F2"),
        Color.FromHex("#A8DDD0"),
        Color.FromHex("#65BDC0"),
        Color.FromHex("#2F9CB0"),
        Color.FromHex("#1C7090"),
        Color.FromHex("#154868"),
        Color.FromHex("#0B2040"),
    ]);

    /// <summary>
    /// Sequential — warm creamy-white through amber and orange to deep red.
    /// Perceptually uniform warm variant; pairs well with <see cref="Crest"/>.
    /// </summary>
    public static IColorMap Flare { get; } = new LinearColorMap("flare",
    [
        Color.FromHex("#FAF0E0"),
        Color.FromHex("#F8D090"),
        Color.FromHex("#F5A040"),
        Color.FromHex("#E86820"),
        Color.FromHex("#C03818"),
        Color.FromHex("#921010"),
        Color.FromHex("#610012"),
    ]);

    /// <summary>
    /// Diverging — cool blue through near-black to warm orange.
    /// The dark midpoint makes this suitable for datasets with a meaningful zero.
    /// </summary>
    public static IColorMap Icefire { get; } = new LinearColorMap("icefire",
    [
        Color.FromHex("#1F4EB4"),
        Color.FromHex("#6090C8"),
        Color.FromHex("#A8C8E4"),
        Color.FromHex("#E0F0F8"),
        Color.FromHex("#1A1A1A"),
        Color.FromHex("#F0C050"),
        Color.FromHex("#E06010"),
        Color.FromHex("#B03008"),
        Color.FromHex("#7A1400"),
    ]);
}
