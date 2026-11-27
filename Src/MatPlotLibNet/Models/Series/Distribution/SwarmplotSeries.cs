// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a swarm plot series that draws non-overlapping dots per category using beeswarm layout.</summary>
/// <remarks>Under the matplotlib themes the dots are placed as seaborn's <c>swarmplot</c> places them: lowest first, each as near the
/// middle of its category as it can stand without touching a dot below it, and no further from it than 0.4 of a category. Under every
/// other theme they are packed in steps of a diameter either side of the category.</remarks>
public sealed class SwarmplotSeries : DatasetSeries, IHasColor, IHasAlpha, IDotSeries, ISeriesKind<SwarmplotSeries>
{
    /// <summary>Diameter of each dot, in pixels; the swarm layout spaces the dots so they do not overlap at this size. Given none, the
    /// dots are 5 points across under the matplotlib themes, as seaborn's (<c>swarmplot(size=5)</c>), and as <see cref="Rests"/> says
    /// under every other theme; a size that is given is drawn as pixels.</summary>
    public double MarkerSize { get => Dot.Diameter ?? Rests.Diameter; set => Dot = Dot with { Diameter = value }; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The opacity of the dots, 0 to 1. Given none, the dots are opaque under the matplotlib themes, as seaborn's (its
    /// <c>ax.scatter</c> is given no alpha), and as <see cref="Rests"/> says under every other theme; an opacity that is given is drawn as
    /// given.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => Dot.Alpha ?? Rests.Alpha; set => Dot = Dot with { Alpha = value.ThrowIfNotOpacity(nameof(Alpha)) }; }

    /// <summary>What was named of the dots (the diameter, the opacity); each part <see langword="null"/> while none was.</summary>
    internal GivenDot Dot { get; set => Change(ref field, value); }

    /// <summary>What a swarm plot's dots rest at under the library's own themes: their diameter and opacity.</summary>
    private static readonly RestingDot Rests = new(5, 0.8);

    GivenDot IDotSeries.Given => Dot;

    RestingDot IDotSeries.Resting => Rests;

    /// <summary>Initializes a new instance of <see cref="SwarmplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public SwarmplotSeries(double[][] datasets) : base(datasets) { }

    /// <inheritdoc />
    /// <remarks>seaborn's <c>swarmplot</c> asks the axes once, through a scout <c>scatter</c>, and colours its groups from its palette's first colour on (<c>SeriesRenderer.GroupColor</c>).</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Dots;

    static string ISeriesKind<SwarmplotSeries>.Kind => "swarmplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Datasets = Datasets,
        MarkerSize = Dot.Diameter,
        Color = Color,
        Alpha = Dot.Alpha
    };

    /// <summary>The reader of a <see cref="SwarmplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SwarmplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Swarmplot(dto.Datasets ?? []);
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
