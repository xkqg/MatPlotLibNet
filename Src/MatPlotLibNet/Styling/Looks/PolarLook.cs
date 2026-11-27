// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>Where a polar axes puts its spoke labels and its title, and how wide a bar on it is given no width.</summary>
internal sealed record PolarLook : IThemeKeys<PolarLook>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's polar axes are named as a whole (<see cref="Matplotlib"/>, told by the pad of its spoke labels); whether its
    /// title rises over what stands below it is the text look's (<see cref="TextLook.TitleOverWhatIsBelow"/>), one fact for every
    /// axes.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.PolarsLikeMatplotlib = (ThetaLabelPad is not null).OrNullAtRest(false);

    /// <inheritdoc />
    public static PolarLook ReadKeys(ThemeDto keys) => keys.PolarsLikeMatplotlib == true ? Matplotlib : Library;

    /// <summary>The library's own polar axes: each spoke label beside the end of its spoke, the title 0.05 of the circle's side above it, a
    /// bar 0.3 radians wide.</summary>
    internal static PolarLook Library { get; } = new();

    /// <summary>A polar axes as matplotlib 3.11.2 draws one under its default style: each spoke label centred 7 points past its tick's pad
    /// (<see cref="ThetaLabelPad"/>), the title at <c>axes.titley</c> (<see cref="TitleRise"/> 0; over the spoke labels that reach above
    /// the circle where the text look says so, <see cref="TextLook.TitleOverWhatIsBelow"/>), a bar 0.8 radians wide (<see cref="BarWidth"/>).
    /// The classic style draws the same.</summary>
    internal static PolarLook Matplotlib { get; } = new() { ThetaLabelPad = 7.0.PointsInPixels, TitleRise = 0, BarWidth = 0.8 };

    /// <summary>How wide a bar of a polar axes is given no width, in radians (<see cref="Models.Series.PolarBarSeries.BarWidth"/>): the
    /// library's own 0.3; 0.8 under the matplotlib looks, as <c>Axes.bar</c> rests at <c>width=0.8</c> on a polar axes too (printed with
    /// 3.11.2 under both styles: <c>ax.bar([0, 1, 2], [1, 2, 3])</c> on a polar axes makes wedges <c>get_width()</c> 0.8 wide).</summary>
    public double BarWidth { get; init; } = 0.3;

    /// <summary>How far past its tick's pad a spoke label's middle stands from the circle, in pixels, the label centred on that point
    /// across and down: matplotlib's <c>ThetaTick.update_position</c>, <c>pad = self._pad + 7</c> points along the spoke's angle, where
    /// <c>_pad</c> is the tick's pad and, for a tick pointing out, its length. <see langword="null"/>: the library's own place beside the
    /// end of the spoke.</summary>
    public double? ThetaLabelPad { get; init; }

    /// <summary>How far above the circle the title's gap starts, as a share of the circle's side: the library's own 0.05 (matplotlib's
    /// <c>PolarAxes.clear</c> sets <c>title.set_y(1.05)</c>, which <c>set_title</c> resets in 3.11.2); 0 under the matplotlib looks, whose
    /// title stands at <c>axes.titley</c>.</summary>
    public double TitleRise { get; init; } = 0.05;

    /// <summary>The width of a polar bar: the one given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal double BarWidthOver(double? given) => given ?? BarWidth;
}
