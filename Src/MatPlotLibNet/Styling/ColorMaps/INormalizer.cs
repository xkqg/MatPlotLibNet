// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Maps a raw data value to a normalized [0, 1] range for colormap lookup.</summary>
public interface INormalizer
{
    /// <summary>Normalizes <paramref name="value"/> within the range [<paramref name="min"/>, <paramref name="max"/>] to [0, 1].</summary>
    double Normalize(double value, double min, double max);
}

/// <summary>What the library adds to a normaliser beyond <see cref="INormalizer.Normalize"/>, each normaliser answering for itself: which
/// normaliser a theme reads it as, what it masks, where the middle of a band stands, the scale its colour range and its contour levels
/// are taken over, the normaliser a colour bar draws for it, and whether a colour bar names decades. Internal, beside the public
/// <see cref="INormalizer"/>, and reached through <see cref="NormalizerRuleExtensions"/>' <c>Rule</c>, which answers for a normaliser
/// the library does not know as it answers for a linear one: every member defaults to that answer, so a normaliser states only where
/// it differs. No site tests which type a normaliser is.</summary>
internal interface INormalizerRule
{
    /// <summary>The normaliser a theme whose colour-map look is <paramref name="look"/> reads this one as; <see langword="null"/> for this
    /// one itself.</summary>
    /// <param name="look">The theme's colour-map look.</param>
    INormalizer? ReadUnder(ColorMapLook look) => null;

    /// <summary>The normaliser a colour bar whose look is <paramref name="look"/> is drawn with for this one; <see langword="null"/> for
    /// this one itself.</summary>
    /// <param name="look">The theme's colour-bar look.</param>
    INormalizer? ForColorBarUnder(ColorBarLook look) => null;

    /// <summary>Whether any value is left out of the colouring over a range that starts at <paramref name="min"/> (<see cref="Masks"/>),
    /// so a grid that cannot be masked is not walked.</summary>
    /// <param name="min">The low end of the range the colours run over.</param>
    bool MasksOver(double min) => false;

    /// <summary>Whether <paramref name="value"/> is left out of the colouring, drawn in the colour map's missing colour.</summary>
    /// <param name="value">The value to colour.</param>
    /// <param name="min">The low end of the range the colours run over.</param>
    bool Masks(double value, double min) => false;

    /// <summary>The value a band between two levels is coloured at: the middle of the band in the space the normaliser spreads values
    /// over, the arithmetic mean by default.</summary>
    /// <param name="low">The band's lower level.</param>
    /// <param name="high">The band's upper level.</param>
    double MiddleOf(double low, double high) => LinearMiddleOf(low, high);

    /// <summary>The arithmetic middle of a band between two levels: the default of <see cref="MiddleOf"/>, and where a normaliser that
    /// names another middle has none to give (a log normaliser over a band that reaches 0).</summary>
    /// <param name="low">The band's lower level.</param>
    /// <param name="high">The band's upper level.</param>
    /// <returns>The value halfway between the two.</returns>
    static double LinearMiddleOf(double low, double high) => low + (high - low) * 0.5;

    /// <summary>The scale the colour range of a series is taken over: the values that scale can place
    /// (<see cref="Rendering.ScaleRule.LowestPlaced"/>); linear by default.</summary>
    Rendering.ScaleRule RangeScale => Rendering.ScaleRule.Linear;

    /// <summary>The scale a contour coloured with this normaliser lays its levels over (<see cref="Rendering.ScaleRule.LevelsOver"/>);
    /// linear by default.</summary>
    Rendering.ScaleRule LevelScale => Rendering.ScaleRule.Linear;

    /// <summary>Whether a colour bar over a range from <paramref name="lo"/> names its ticks as a log axis does (decades, in scientific
    /// notation); no by default.</summary>
    /// <param name="lo">The low end of the bar's range.</param>
    bool NamesDecadesOver(double lo) => false;
}

/// <summary>What a normaliser over a range with width supplies: where a value stands in that range. The range itself, a missing value and
/// a range of no width are decided once, in <see cref="RangeFractionExtensions.Normalized"/>, which every normaliser of this kind
/// answers <see cref="INormalizer.Normalize"/> through. Internal: it is what the library adds to <see cref="INormalizer"/>, not part of
/// the contract a caller implements.</summary>
internal interface IRangeFraction
{
    /// <summary>Where <paramref name="value"/> stands between <paramref name="min"/> and <paramref name="max"/>, in [0, 1], for a range
    /// that has width.</summary>
    /// <param name="value">The value.</param>
    /// <param name="min">The low end of the range.</param>
    /// <param name="max">The high end of the range.</param>
    double Fraction(double value, double min, double max);
}

/// <summary>Linear normalization: <c>(value - min) / (max - min)</c>, clamped to [0, 1].</summary>
public sealed class LinearNormalizer : INormalizer, IRangeFraction, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new LinearNormalizerDto();

    /// <summary>The shared instance; the normalizer holds no state.</summary>
    public static LinearNormalizer Instance { get; } = new();

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => this.Normalized(value, min, max);

    /// <inheritdoc />
    double IRangeFraction.Fraction(double value, double min, double max)
    {
        double range = max - min;
        return ((value - min) / range).Saturate();
    }
}

/// <summary>Logarithmic normalization that compresses high values: <c>log(1 + value - min) / log(1 + max - min)</c>. Under the
/// matplotlib themes (<c>ColorMapLook.LikeLogNorm</c>) a series that names it is coloured as matplotlib's <c>LogNorm</c> colours it,
/// <c>log(value / vmin) / log(vmax / vmin)</c> with a value of 0 or below left out; the two agree only where the minimum is 1.</summary>
public sealed class LogNormalizer : INormalizer, IRangeFraction, IJsonDescribed<NormalizerDto>, INormalizerRule
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new LogNormalizerDto();

    /// <inheritdoc />
    /// <remarks>matplotlib's <see cref="LogNorm"/> under a theme that colours so (<see cref="ColorMapLook.LikeLogNorm"/>).</remarks>
    INormalizer? INormalizerRule.ReadUnder(ColorMapLook look) => look.LikeLogNorm ? LogNorm.Instance : null;

    /// <inheritdoc />
    /// <remarks>A contour coloured with it lays the decades of a log scale, under every theme.</remarks>
    ScaleRule INormalizerRule.LevelScale => ScaleRule.Log;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => this.Normalized(value, min, max);

    /// <inheritdoc />
    double IRangeFraction.Fraction(double value, double min, double max)
    {
        double range = max - min;
        double clamped = Math.Clamp(value, min, max);
        return Math.Log(1 + clamped - min) / Math.Log(1 + range);
    }
}

/// <summary>Two-slope normalization with a center point that maps to 0.5. Useful for diverging data
/// where values above and below center should use different halves of a diverging colormap.</summary>
public sealed class TwoSlopeNormalizer : INormalizer, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new TwoSlopeNormalizerDto(Center);

    /// <summary>The data value that maps to the middle of the colour map (0.5).</summary>
    public double Center { get; }

    /// <summary>Creates a two-slope normalizer with the specified center value.</summary>
    public TwoSlopeNormalizer(double center) => Center = center;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => CenteredNormNormalizer.Between(value, min, Center, max);
}

/// <summary>A normaliser that sends the values between two boundaries to one colour: a colour bar drawn for it is bands, one
/// per bin, with a tick on the boundaries it names, since the colour of a value says which bin it is in and nothing about where
/// in the bin it stands. Internal: the bar asks a normaliser for this capability and holds no list of types.</summary>
internal interface IDiscreteNormalizer
{
    /// <summary>The boundaries of the bins, ascending: <c>n + 1</c> of them for <c>n</c> bins.</summary>
    double[] Boundaries { get; }

    /// <summary>The fraction of the colour map bin <paramref name="band"/> is drawn in, from 0 for the map's first colour to 1 for its
    /// last.</summary>
    /// <param name="band">The bin, from 0 for the lowest.</param>
    double MapFractionOf(int band);

    /// <summary>The most boundaries matplotlib's colour bar names for a bar of bands (<c>FixedLocator(boundaries, nbins=10)</c>, the
    /// locator <c>colorbar.py</c> gives both a <c>BoundaryNorm</c> and a <c>ContourSet</c>): the one number every bar that thins reads.</summary>
    const int MostTicks = 10;

    /// <summary>The most boundaries the bar names with a tick: more are thinned as matplotlib's <c>FixedLocator(boundaries, nbins)</c>
    /// thins them, to every <c>step</c>-th. <see cref="int.MaxValue"/> names every one.</summary>
    int MostNamed { get; }
}

/// <summary>Discrete boundary normalization that maps values to bins defined by boundary values.
/// Values between consecutive boundaries map to the same normalized value.</summary>
public sealed class BoundaryNormalizer : INormalizer, IDiscreteNormalizer, IJsonDescribed<NormalizerDto>, INormalizerRule
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new BoundaryNormalizerDto(_boundaries);

    /// <inheritdoc />
    /// <remarks>A bar that steps a colour per band names at most the ten boundaries matplotlib's does (<see cref="ThinnedForBar"/>).</remarks>
    INormalizer? INormalizerRule.ForColorBarUnder(ColorBarLook look) => look.StepPerColour ? ThinnedForBar() : null;

    /// <inheritdoc />
    double[] IDiscreteNormalizer.Boundaries => _boundaries;

    /// <inheritdoc />
    double IDiscreteNormalizer.MapFractionOf(int band) => (double)band / (_boundaries.Length - 1);

    /// <inheritdoc />
    int IDiscreteNormalizer.MostNamed => _mostNamed;

    private readonly double[] _boundaries;

    /// <summary>The most boundaries a colour bar of these bands names: every one, until <see cref="ThinnedForBar"/> says ten.</summary>
    private int _mostNamed = int.MaxValue;

    /// <summary>Creates a boundary normalizer with the specified boundary values (must be sorted ascending).</summary>
    public BoundaryNormalizer(double[] boundaries) => _boundaries = boundaries;

    /// <summary>The same bins, for a colour bar that names at most the ten boundaries matplotlib's does
    /// (<see cref="IDiscreteNormalizer.MostTicks"/>): the normaliser a bar under the matplotlib themes is drawn with, which colours every
    /// value as this one does.</summary>
    /// <returns>The normaliser with at most that many boundaries named.</returns>
    internal BoundaryNormalizer ThinnedForBar() => new(_boundaries) { _mostNamed = IDiscreteNormalizer.MostTicks };

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        int nBins = _boundaries.Length - 1;
        if (nBins <= 0) return 0.5;

        for (int i = 0; i < nBins; i++)
        {
            if (value < _boundaries[i + 1])
                return (double)i / nBins;
        }

        return (double)(nBins - 1) / nBins;
    }
}

/// <summary>Symmetric logarithmic normalization. Linear within ±<see cref="Linthresh"/>,
/// log-compressed beyond. Useful for data that spans positive and negative values including zero.</summary>
public sealed class SymLogNormalizer : INormalizer, IRangeFraction, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new SymLogNormalizerDto(Linthresh, Base, LinScale);

    /// <summary>The half-width of the linear region around zero: values within ±this are scaled linearly, beyond it logarithmically. <see cref="SymlogTransform.DefaultLinThresh"/> by default.</summary>
    public double Linthresh { get; }

    /// <summary>The logarithm base of the compressed region beyond ±<see cref="Linthresh"/>. <see cref="SymlogTransform.DefaultBase"/> by default.</summary>
    public double Base { get; }

    /// <summary>How much room the linear region ±<see cref="Linthresh"/> takes on the transformed scale, matplotlib's <c>linscale</c>: each half is stretched by <c>LinScale / (1 - 1/Base)</c> (<see cref="SymlogTransform"/>), as matplotlib's <c>SymLogNorm</c> stretches it. <see cref="SymlogTransform.DefaultLinScale"/> by default.</summary>
    public double LinScale { get; }

    /// <summary>Creates a symmetric-log normalizer.</summary>
    public SymLogNormalizer(double linthresh = SymlogTransform.DefaultLinThresh, double @base = SymlogTransform.DefaultBase, double linScale = SymlogTransform.DefaultLinScale)
    {
        Linthresh = linthresh;
        Base = @base;
        LinScale = linScale;
    }

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => this.Normalized(value, min, max);

    /// <inheritdoc />
    double IRangeFraction.Fraction(double value, double min, double max)
    {
        double Transform(double v) => SymlogTransform.Forward(v, Linthresh, Base, LinScale);

        double t = Transform(Math.Clamp(value, min, max));
        return LinearNormalizer.Instance.Normalize(t, Transform(min), Transform(max));
    }
}

/// <summary>Power-law normalization: <c>((value − min) / (max − min))^gamma</c>.
/// gamma &lt; 1 expands low values; gamma &gt; 1 compresses them.</summary>
public sealed class PowerNormNormalizer : INormalizer, IRangeFraction, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new PowerNormalizerDto(Gamma);

    /// <summary>The exponent applied to the normalized value: below 1 it expands low values, above 1 it compresses them, and 1 (the default) leaves the scale linear.</summary>
    public double Gamma { get; }

    /// <summary>Creates a power-norm normalizer with the specified gamma.</summary>
    public PowerNormNormalizer(double gamma = 1.0) => Gamma = gamma;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => this.Normalized(value, min, max);

    /// <inheritdoc />
    double IRangeFraction.Fraction(double value, double min, double max) =>
        Math.Pow(LinearNormalizer.Instance.Normalize(value, min, max), Gamma);
}

/// <summary>Centered normalization that maps a chosen <see cref="Vcenter"/> to 0.5.
/// Independently scales the lower and upper halves. Optionally constrains a symmetric
/// half-range around the center.</summary>
public sealed class CenteredNormNormalizer : INormalizer, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new CenteredNormalizerDto(Vcenter, Halfrange);

    /// <summary>The data value that maps to the middle of the colour map (0.5). 0 by default.</summary>
    public double Vcenter { get; }

    /// <summary>When set, the distance either side of <see cref="Vcenter"/> that spans the whole map, which makes the scale symmetric; <see langword="null"/> lets the data's own minimum and maximum bound each side.</summary>
    public double? Halfrange { get; }

    /// <summary>Creates a centered normalizer.</summary>
    public CenteredNormNormalizer(double vcenter = 0.0, double? halfrange = null)
    {
        Vcenter = vcenter;
        Halfrange = halfrange;
    }

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double lo = Halfrange.HasValue ? Vcenter - Halfrange.Value : min;
        double hi = Halfrange.HasValue ? Vcenter + Halfrange.Value : max;
        return Between(value, lo, Vcenter, hi);
    }

    /// <summary>Where <paramref name="value"/> stands on a scale that runs from <paramref name="lo"/> to <paramref name="hi"/> with its
    /// middle at <paramref name="center"/>: the value is held to the scale, each half fills its half of the colour map independently,
    /// and a half with no width (the centre at an end) maps to the middle. The one rule of this scale and of
    /// <see cref="TwoSlopeNormalizer"/>, which is this scale over the data range.</summary>
    /// <param name="value">The value.</param>
    /// <param name="lo">The low end of the scale.</param>
    /// <param name="center">The value that maps to 0.5.</param>
    /// <param name="hi">The high end of the scale.</param>
    /// <returns>The position in [0, 1].</returns>
    internal static double Between(double value, double lo, double center, double hi)
    {
        double clamped = Math.Clamp(value, lo, hi);
        bool below = clamped <= center;
        double span = below ? center - lo : hi - center;
        if (span == 0) return 0.5;
        return below ? 0.5 * (clamped - lo) / span : 0.5 + 0.5 * (clamped - center) / span;
    }
}

/// <summary>No-op normalization: the value is passed through as-is, clamped to [0, 1].
/// Use when the data is already in the normalized range.</summary>
public sealed class NoNormNormalizer : INormalizer, IJsonDescribed<NormalizerDto>
{
    /// <inheritdoc />
    NormalizerDto IJsonDescribed<NormalizerDto>.Describe() => new NoNormNormalizerDto();

    /// <summary>The shared instance; the normalizer holds no state.</summary>
    public static NoNormNormalizer Instance { get; } = new();

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => value.Saturate();
}
