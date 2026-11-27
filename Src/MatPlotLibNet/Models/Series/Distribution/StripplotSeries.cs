// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a strip plot series that draws randomly jittered dots per category to show individual data points.</summary>
public sealed class StripplotSeries : DatasetSeries, IHasColor, IHasAlpha, IDotSeries, ISeriesKind<StripplotSeries>
{
    /// <summary>How far a dot may stray sideways from its category, in category units: each dot moves by a random
    /// amount between minus and plus this value, seeded per category so the picture is reproducible. Given none, the dots stray
    /// by up to 0.1 under the matplotlib themes, as seaborn's (<c>stripplot(jitter=True)</c>), and as far as <see cref="Rests"/> says
    /// under every other theme; a jitter that is given is drawn as given.</summary>
    public double Jitter { get => Dot.Jitter ?? Rests.Jitter; set => Dot = Dot with { Jitter = value }; }

    /// <summary>Diameter of each dot, in pixels. Given none, the dots are 5 points across under the matplotlib themes, as seaborn's
    /// (<c>stripplot(size=5)</c>), and as <see cref="Rests"/> says under every other theme; a size that is given is drawn as pixels.</summary>
    public double MarkerSize { get => Dot.Diameter ?? Rests.Diameter; set => Dot = Dot with { Diameter = value }; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The opacity of the dots, 0 to 1. Given none, the dots are opaque under the matplotlib themes, as seaborn's (its
    /// <c>ax.scatter</c> is given no alpha), and as <see cref="Rests"/> says under every other theme; an opacity that is given is drawn as
    /// given.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => Dot.Alpha ?? Rests.Alpha; set => Dot = Dot with { Alpha = value.ThrowIfNotOpacity(nameof(Alpha)) }; }

    /// <summary>What was named of the dots (the diameter, the opacity, the jitter); each part <see langword="null"/> while none was.</summary>
    internal GivenDot Dot { get; set => Change(ref field, value); }

    /// <summary>What a strip plot's dots rest at under the library's own themes: their diameter, opacity and stray.</summary>
    private static readonly RestingDot Rests = new(5, 0.8, 0.2);

    GivenDot IDotSeries.Given => Dot;

    RestingDot IDotSeries.Resting => Rests;

    /// <summary>Initializes a new instance of <see cref="StripplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public StripplotSeries(double[][] datasets) : base(datasets) { }

    /// <inheritdoc />
    /// <remarks>seaborn's <c>stripplot</c> asks the axes once, through a scout <c>scatter</c>, and colours its groups from its palette's first colour on (<c>SeriesRenderer.GroupColor</c>).</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Dots;

    static string ISeriesKind<StripplotSeries>.Kind => "stripplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Datasets = Datasets,
        Jitter = Dot.Jitter,
        MarkerSize = Dot.Diameter,
        Color = Color,
        Alpha = Dot.Alpha
    };

    /// <summary>The reader of a <see cref="StripplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static StripplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stripplot(dto.Datasets ?? []);
        if (dto.Jitter is { } jitter) s.Jitter = jitter;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
