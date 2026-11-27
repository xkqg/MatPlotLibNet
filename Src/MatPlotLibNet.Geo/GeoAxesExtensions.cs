// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using H3.Extensions;
using H3.Model;
using H3;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using System.Globalization;
using System.Numerics;

namespace MatPlotLibNet.Geo;

/// <summary>Extension methods for adding geographic features to an <see cref="AxesBuilder"/>.
/// Provides a cartopy-like API: <c>ax.WithProjection(Robinson).Coastlines().Borders()</c>.</summary>
/// <remarks>A map axes has ONE projection: <c>WithProjection</c> records it, and every layer reads it. A layer verb given a
/// projection of its own must give the one the axes has (the same kind with the same parameters); a layer verb given none
/// before the axes has one is refused.</remarks>
public static partial class GeoAxesExtensions
{
    /// <summary>The projection each map axes was given, kept for as long as the axes builder lives.</summary>
    private static readonly ConditionalWeakTable<AxesBuilder, IGeoProjection> Recorded = new();

    /// <summary>The projection the axes was given.</summary>
    /// <exception cref="InvalidOperationException">The axes was given none.</exception>
    private static IGeoProjection OfTheAxes(AxesBuilder ax) =>
        Recorded.TryGetValue(ax, out var recorded)
            ? recorded
            : throw new InvalidOperationException("The axes has no projection: call WithProjection first.");

    /// <summary><paramref name="projection"/>, once it is known to be the axes' projection when the axes has one.</summary>
    /// <exception cref="ArgumentException">The axes was given another projection.</exception>
    private static IGeoProjection AgreeingWithTheAxes(AxesBuilder ax, IGeoProjection projection)
    {
        if (Recorded.TryGetValue(ax, out var recorded) && !recorded.IsSameProjectionAs(projection))
            throw new ArgumentException(
                $"The axes was given the {recorded.Described()} projection with WithProjection, and this layer asks for {projection.Described()}. " +
                "One axes has one projection.", nameof(projection));
        return projection;
    }

    /// <summary>The places a projection can place: their projected coordinates and the index of each in the arrays given.</summary>
    /// <param name="X">The projected X of each place.</param>
    /// <param name="Y">The projected Y of each place.</param>
    /// <param name="Kept">The index of each place in the arrays it was taken from.</param>
    private readonly record struct PlacedPositions(double[] X, double[] Y, int[] Kept);

    /// <summary>Projects the places on <paramref name="projection"/>, leaving out the ones it cannot place.</summary>
    /// <exception cref="ArgumentException">The arrays differ in length.</exception>
    private static PlacedPositions ProjectedPlaces(IGeoProjection projection, double[] latitudes, double[] longitudes)
    {
        latitudes.Length.ThrowIfMisaligned(longitudes.Length);
        var x = new List<double>();
        var y = new List<double>();
        var kept = new List<int>();
        for (int i = 0; i < latitudes.Length; i++)
        {
            if (projection.Place(new GeoCoordinate(latitudes[i], longitudes[i])) is not { } place) continue;
            x.Add(place.X);
            y.Add(place.Y);
            kept.Add(i);
        }

        return new([.. x], [.. y], [.. kept]);
    }

    /// <summary>A scatter of bubbles: each drawn with the area it has (see <see cref="BubblesAt"/>).</summary>
    private static ScatterSeries BubbleScatter(BubblePlaces bubbles) => new(bubbles.X, bubbles.Y) { Sizes = bubbles.Areas };

    /// <summary>A country to resize, drawn by a value.</summary>
    private readonly record struct Resizable(GeoFeature Feature, IReadOnlyList<ProjectedPolygon> Polygons, RegionMeasure Measure, double Value);

    /// <summary>The countries that have a value above zero and an area on <paramref name="projection"/>.</summary>
    private static List<Resizable> ResizableRegions(IGeoProjection projection, IReadOnlyDictionary<string, double> valueOf)
    {
        var regions = new List<Resizable>();
        foreach (var country in NaturalEarth110m.Countries())
        {
            double value = ValueByCode(country, valueOf);
            if (!value.IsPositiveFinite()) continue;
            var polygons = ProjectedBy(country, projection);
            if (Measure(polygons) is { } measure) regions.Add(new Resizable(country, polygons, measure, value));
        }

        return regions;
    }

    /// <summary>The value <paramref name="valueOf"/> gives a country by its three-letter code (<c>ADM0_A3</c>); no number when it
    /// gives none.</summary>
    private static double ValueByCode(GeoFeature country, IReadOnlyDictionary<string, double> valueOf) =>
        country.Properties.TryGetValue("ADM0_A3", out var code) && valueOf.TryGetValue(code, out double value) ? value : double.NaN;

    /// <summary>A map that draws each region by a value, in one look: its outline wears the theme's neutral grey and is 0.3 wide, its
    /// label names the verb. What differs between the verbs that make one is a value of the verb, not a copy of the look: where it
    /// stands in the drawing order, the one fill the verb chose where the regions are not coloured by their values, and whether its
    /// coordinates are already projected (a cartogram's are).</summary>
    /// <param name="Label">The label of the series, which the legend names it by.</param>
    /// <param name="ZOrder">Where the series stands in the drawing order; <see langword="null"/> to leave it where it stands.</param>
    /// <param name="Fill">The one colour the verb chose to fill its regions in, which the series is drawn in under the library's own
    /// themes and gives way to the theme's cycle under the matplotlib themes (a <c>verbColor</c> in the JSON, not a colour the caller
    /// gave); <see langword="null"/> for a map coloured by its values.</param>
    /// <param name="Projected">Whether the coordinates of its regions are already projected.</param>
    private readonly record struct RegionMap(string Label, int? ZOrder, Color? Fill, bool Projected)
    {
        /// <summary>The countries, each coloured by a value, drawn behind the layers that follow.</summary>
        internal static RegionMap Choropleth => new("Choropleth", -1, null, false);

        /// <summary>The cells of the H3 index, each coloured by a value.</summary>
        internal static RegionMap H3Cells => new("H3 cells", null, null, false);

        /// <summary>The countries resized by a value about their centres, in the blue the verb chose.</summary>
        internal static RegionMap Cartogram => new("Cartogram", -1, CyclePair.Standard.First, true);

        /// <summary>The countries bent by a value so that they still meet, in the blue the verb chose.</summary>
        internal static RegionMap ContiguousCartogram => new("Contiguous cartogram", -1, CyclePair.Standard.First, true);

        /// <summary>The series of <paramref name="regions"/> on <paramref name="projection"/>.</summary>
        /// <param name="regions">The regions; the list is kept, not copied.</param>
        /// <param name="projection">The map projection.</param>
        /// <param name="values">The value of each region; <see langword="null"/> where the regions are drawn in one fill.</param>
        /// <param name="colorMap">The colour map the values are drawn through; <see langword="null"/> for the theme's.</param>
        internal GeoPolygonSeries Of(List<GeoFeature> regions, IGeoProjection projection, double[]? values = null, IColorMap? colorMap = null)
        {
            var series = new GeoPolygonSeries(regions, projection)
            {
                Values = values,
                ColorMap = colorMap,
                VerbColor = Fill,
                IsRawProjected = Projected,
                WearsNeutral = true,
                StrokeWidth = RegionEdgeWidth,
                Label = Label,
            };
            if (ZOrder is { } zOrder) series.ZOrder = zOrder;
            return series;
        }
    }

    extension(AxesBuilder ax)
    {
        /// <summary>Sets the map projection for this axes and configures it for geographic display
        /// (hides standard axes chrome). The layers that follow (<c>Coastlines</c>, <c>Borders</c>, <c>Ocean</c> and <c>Land</c>)
        /// are drawn on it; one that is given a projection of its own must give this one. A map keeps its projection's shape, one
        /// unit of Y as long as one unit of X, centred in its cell, as cartopy's maps do; <c>SetAspect</c> gives it another.</summary>
        /// <exception cref="ArgumentException">The axes was already given another projection.</exception>
        public AxesBuilder WithProjection(IGeoProjection projection)
        {
            AgreeingWithTheAxes(ax, projection);
            Recorded.AddOrUpdate(ax, projection);
            ax.HideAllAxes();
            return ax;
        }

        /// <summary>Adds the Natural Earth coastlines on the axes' projection (<see cref="WithProjection"/>).</summary>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Coastlines(Color? color = null, double lineWidth = CoastlineWidth) =>
            ax.Coastlines(OfTheAxes(ax), color, lineWidth);

        /// <summary>Adds the Natural Earth boundaries between countries on land on the axes' projection (<see cref="WithProjection"/>).</summary>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Borders(Color? color = null, double lineWidth = BorderWidth) =>
            ax.Borders(OfTheAxes(ax), color, lineWidth);

        /// <summary>Fills the ocean, the area inside the outline of the axes' projection (<see cref="WithProjection"/>).</summary>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Ocean(Color color) => ax.Ocean(OfTheAxes(ax), color);

        /// <summary>Fills the land on the axes' projection (<see cref="WithProjection"/>).</summary>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Land(Color color) => ax.Land(OfTheAxes(ax), color);

        /// <summary>Colours each country by a value, through a colour map, on the axes' projection (<see cref="WithProjection"/>).
        /// Add <c>WithColorBar()</c> for the scale.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (Natural Earth's <c>ADM0_A3</c>: FRA, NOR, ...).
        /// A country with no value is drawn in the colour map's bad colour; a code no country has is ignored (the 110m data
        /// holds 177 countries and leaves the smallest states out).</param>
        /// <param name="colorMap">The colour map; viridis when none is given.</param>
        /// <param name="configure">Sets the series (the outline colour and width, the label, the normaliser); the outline wears the
        /// theme's neutral grey and is 0.3 wide until it does.</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Choropleth(IReadOnlyDictionary<string, double> valueOf, IColorMap? colorMap = null,
            Action<GeoPolygonSeries>? configure = null) =>
            ax.Choropleth(OfTheAxes(ax), valueOf, colorMap, configure);

        /// <summary>Colours each country by a numeric property of the built-in countries (<c>POP_EST</c>, <c>GDP_MD</c>, ...),
        /// read invariantly, through a colour map, on the axes' projection (<see cref="WithProjection"/>). A country whose property
        /// is missing or not a number is drawn in the colour map's bad colour.</summary>
        /// <param name="property">The name of the property.</param>
        /// <param name="colorMap">The colour map; viridis when none is given.</param>
        /// <param name="configure">Sets the series; the outline wears the theme's neutral grey and is 0.3 wide until it does.</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Choropleth(string property, IColorMap? colorMap = null, Action<GeoPolygonSeries>? configure = null) =>
            ax.Choropleth(OfTheAxes(ax), property, colorMap, configure);

        /// <summary>Colours each country by a value on <paramref name="projection"/>, which must be the axes' (see the overload
        /// without one).</summary>
        /// <param name="projection">The map projection.</param>
        /// <param name="valueOf">The value of each country by its three-letter code.</param>
        /// <param name="colorMap">The colour map; viridis when none is given.</param>
        /// <param name="configure">Sets the series; the outline wears the theme's neutral grey and is 0.3 wide until it does.</param>
        public AxesBuilder Choropleth(IGeoProjection projection, IReadOnlyDictionary<string, double> valueOf,
            IColorMap? colorMap = null, Action<GeoPolygonSeries>? configure = null) =>
            ax.ChoroplethOfCountries(projection, country => ValueByCode(country, valueOf), colorMap, configure);

        /// <summary>Colours each country by a numeric property on <paramref name="projection"/>, which must be the axes' (see the
        /// overload without one).</summary>
        /// <param name="projection">The map projection.</param>
        /// <param name="property">The name of the property.</param>
        /// <param name="colorMap">The colour map; viridis when none is given.</param>
        /// <param name="configure">Sets the series; the outline wears the theme's neutral grey and is 0.3 wide until it does.</param>
        public AxesBuilder Choropleth(IGeoProjection projection, string property, IColorMap? colorMap = null,
            Action<GeoPolygonSeries>? configure = null) =>
            ax.ChoroplethOfCountries(projection, country => country.NumberOf(property), colorMap, configure);

        private AxesBuilder ChoroplethOfCountries(IGeoProjection projection, Func<GeoFeature, double> valueOf, IColorMap? colorMap,
            Action<GeoPolygonSeries>? configure)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var countries = NaturalEarth110m.Countries();
            return ax.AddSeries(RegionMap.Choropleth.Of(countries, projection, [.. countries.Select(valueOf)], colorMap), configure);
        }

        /// <summary>Marks places on the map: a scatter at the projected position of each place.</summary>
        /// <param name="latitudes">The latitude of each place, in degrees.</param>
        /// <param name="longitudes">The longitude of each place, in degrees.</param>
        /// <param name="configure">Sets the <see cref="ScatterSeries"/> (colour, marker, label).</param>
        /// <exception cref="ArgumentException">The arrays differ in length; the axes is left as it was.</exception>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        /// <remarks>A place the projection cannot place (the far side of an orthographic globe) is left out.</remarks>
        public AxesBuilder Points(double[] latitudes, double[] longitudes, Action<ScatterSeries>? configure = null)
        {
            var places = ProjectedPlaces(OfTheAxes(ax), latitudes, longitudes);
            return ax.AddSeries(new ScatterSeries(places.X, places.Y), configure);
        }

        /// <summary>Marks places on the map with bubbles whose area is in proportion to a value: the largest value has the size of
        /// 400 points squared (a circle 20 points across, as a scatter's size of 400 is drawn), the others in proportion.</summary>
        /// <param name="latitudes">The latitude of each place, in degrees.</param>
        /// <param name="longitudes">The longitude of each place, in degrees.</param>
        /// <param name="values">The value of each place; a place of no value (zero, below zero, no number) has no bubble.</param>
        /// <param name="configure">Sets the <see cref="ScatterSeries"/>; its <c>Sizes</c> can be rescaled here.</param>
        /// <exception cref="ArgumentException">The arrays differ in length; the axes is left as it was.</exception>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Bubbles(double[] latitudes, double[] longitudes, double[] values, Action<ScatterSeries>? configure = null)
        {
            latitudes.Length.ThrowIfMisaligned(values.Length);
            var places = ProjectedPlaces(OfTheAxes(ax), latitudes, longitudes);
            double[] kept = [.. places.Kept.Select(i => values[i])];
            return ax.AddSeries(BubbleScatter(BubblesAt(kept, places.X, places.Y)), configure);
        }

        /// <summary>Bins places into hexagons, in the plane of the map: the bins are as wide in the picture everywhere, whatever the
        /// projection stretches.</summary>
        /// <param name="latitudes">The latitude of each place, in degrees.</param>
        /// <param name="longitudes">The longitude of each place, in degrees.</param>
        /// <param name="configure">Sets the <see cref="HexbinSeries"/> (grid size, colour map, label).</param>
        /// <exception cref="ArgumentException">The arrays differ in length; the axes is left as it was.</exception>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder HexBins(double[] latitudes, double[] longitudes, Action<HexbinSeries>? configure = null)
        {
            var places = ProjectedPlaces(OfTheAxes(ax), latitudes, longitudes);
            return ax.AddSeries(new HexbinSeries(places.X, places.Y), configure);
        }

        /// <summary>Colours cells of Uber's H3 index by a value, through a colour map, on the axes' projection
        /// (<see cref="WithProjection"/>): each cell is drawn as the polygon of its boundary, cut where it crosses the antimeridian.
        /// Add <c>WithColorBar()</c> for the scale.</summary>
        /// <param name="valueOf">The value of each cell by its index, as H3 writes one (<c>8928308280fffff</c>; either case). Cells
        /// of different resolutions can be mixed; a value that is no number is drawn in the colour map's bad colour.</param>
        /// <param name="colorMap">The colour map; viridis when none is given.</param>
        /// <param name="configure">Sets the <see cref="GeoPolygonSeries"/> (the outline colour and width, the label, the normaliser);
        /// the outline wears the theme's neutral grey and is 0.3 wide until it does. Each region keeps its index in the property
        /// <c>h3</c>.</param>
        /// <exception cref="ArgumentException">An index is not the index of an H3 cell; the axes is left as it was.</exception>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder H3Cells(IReadOnlyDictionary<string, double> valueOf, IColorMap? colorMap = null,
            Action<GeoPolygonSeries>? configure = null) =>
            ax.WithH3Cells(OfTheAxes(ax), AsH3Cells(valueOf), colorMap, configure);

        /// <summary>Counts places into the cells of Uber's H3 index at one resolution and colours each cell that holds a place by
        /// how many it holds, on the axes' projection (<see cref="WithProjection"/>): the bins are as large on the globe everywhere,
        /// whatever the projection stretches. A place with a coordinate that is no number is left out.</summary>
        /// <param name="latitudes">The latitude of each place, in degrees.</param>
        /// <param name="longitudes">The longitude of each place, in degrees.</param>
        /// <param name="resolution">The resolution of the cells, from 0 (122 cells over the globe, about 1,100 km across) to 15
        /// (under a metre); each step is about seven times as many cells.</param>
        /// <param name="configure">Sets the <see cref="GeoPolygonSeries"/>, as for <c>H3Cells</c> (the colour map among it).</param>
        /// <exception cref="ArgumentException">The arrays differ in length; the axes is left as it was.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The resolution is below 0 or above 15; the axes is left as it was.</exception>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder H3Bins(double[] latitudes, double[] longitudes, int resolution, Action<GeoPolygonSeries>? configure = null) =>
            ax.WithH3Cells(OfTheAxes(ax), BinnedIntoH3Cells(latitudes, longitudes, resolution), null, configure);

        private AxesBuilder WithH3Cells(IGeoProjection projection, H3CellMap map, IColorMap? colorMap, Action<GeoPolygonSeries>? configure) =>
            ax.AddSeries(RegionMap.H3Cells.Of(map.Cells, projection, map.Values, colorMap), configure);

        /// <summary>Draws flows between places: a line along the great circle from the start of each to its end, as wide as its
        /// quantity (the largest flow is <paramref name="maxWidth"/> wide). A line that crosses the antimeridian is cut where it
        /// leaves one edge of the map and starts again at the other; a flow with an end the projection cannot place, and a flow of
        /// no quantity (zero, below zero, no number), is not drawn.</summary>
        /// <param name="flows">The flows.</param>
        /// <param name="color">The colour of every line; the lines share the next colour of the axes when none is given.</param>
        /// <param name="maxWidth">The width of the largest flow, in pixels.</param>
        /// <param name="configure">Sets each <see cref="LineSeries"/> that is drawn (once for each flow).</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Flows(IReadOnlyList<GeoFlow> flows, Color? color = null, double maxWidth = 4,
            Action<LineSeries>? configure = null)
        {
            var projection = OfTheAxes(ax);
            var drawn = new List<GeoFlowPath>();
            foreach (var flow in flows)
            {
                if (flow.PathOn(projection) is { } path) drawn.Add(path);
            }

            if (drawn.Count == 0) return ax;

            double largest = drawn.Max(path => path.Value);
            LineSeries[] lines = [.. drawn.Select(line => new LineSeries(line.X, line.Y) { LineWidth = maxWidth * line.Value / largest, Color = color })];
            // Flows given no colour wear one colour of the cycle together.
            if (color is null) ax.Axes.InColorGroup(lines);
            foreach (var line in lines) ax.AddSeries(line, configure);

            return ax;
        }

        /// <summary>Resizes each country by a value without moving its centre: a non-contiguous cartogram. The area of a country is
        /// in proportion to its value, and the countries together keep the area they have on the map. A country of no value
        /// (zero, below zero, no number) or that is not in <paramref name="valueOf"/> is not drawn. For countries that still meet at
        /// their borders, see <see cref="ContiguousCartogram"/>.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (<c>ADM0_A3</c>).</param>
        /// <param name="configure">Sets the <see cref="GeoPolygonSeries"/> (fill, outline, label).</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Cartogram(IReadOnlyDictionary<string, double> valueOf, Action<GeoPolygonSeries>? configure = null)
        {
            var projection = OfTheAxes(ax);
            var regions = ResizableRegions(projection, valueOf);
            double scale = regions.Count == 0 ? 0 : regions.Sum(r => r.Measure.Area) / regions.Sum(r => r.Value);
            var resized = regions.Select(r => new GeoFeature(
                ScaledAbout(r.Polygons, r.Measure.Centroid, Math.Sqrt(scale * r.Value / r.Measure.Area)),
                new Dictionary<string, string>(r.Feature.Properties))).ToList();
            return ax.AddSeries(RegionMap.Cartogram.Of(resized, projection), configure);
        }

        /// <summary>Resizes the countries by a value so that they still meet at their borders: a contiguous cartogram, made by the
        /// fast flow-based method of Gastner, Seguy and More. Every country ends with an area in proportion to its value, and the
        /// countries together keep the area they have on the map; each country is bent out of its shape to get there, and a border
        /// two countries share stays one line.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (<c>ADM0_A3</c>). A country whose share of the
        /// values is too small to draw its outline (zero among them) takes the smallest share its outline allows: at least 2.5 %
        /// of its share of all the borders, and at least 0.025 % of the map. A country that is not in the dictionary, or whose
        /// value is below zero or no number, keeps about the area it has. With no value above zero the map is drawn as it is.</param>
        /// <param name="configure">Sets the <see cref="GeoPolygonSeries"/> (fill, outline, label).</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        /// <remarks>The countries are moved on a lattice 512 cells across the longer side of the map, and the flow is laid again on
        /// the moved countries until each is within 1 % of its area, at most twenty times. On a Robinson world map of population 148 of
        /// the 149 countries with a share large enough to draw end within 1 %; a country a few cells across (Mongolia) ends within 10 %.
        /// That map takes about 0.8 s of processor time to make.</remarks>
        public AxesBuilder ContiguousCartogram(IReadOnlyDictionary<string, double> valueOf, Action<GeoPolygonSeries>? configure = null)
        {
            var projection = OfTheAxes(ax);
            var flowed = FlowedOn(NaturalEarth110m.Countries(), projection, country => ValueByCode(country, valueOf));
            return ax.AddSeries(RegionMap.ContiguousCartogram.Of(flowed, projection), configure);
        }

        /// <summary>Draws a circle for each country at the centre of its area, as large as its value: a Dorling cartogram without the
        /// relaxing of circles that overlap. The area of the circle is in proportion to the value, the largest being a size of 400
        /// points squared, as for <see cref="Bubbles"/>.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (<c>ADM0_A3</c>); a country of no value is not drawn.</param>
        /// <param name="configure">Sets the <see cref="ScatterSeries"/>.</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Dorling(IReadOnlyDictionary<string, double> valueOf, Action<ScatterSeries>? configure = null)
        {
            var regions = ResizableRegions(OfTheAxes(ax), valueOf);
            double[] values = [.. regions.Select(r => r.Value)];
            return ax.AddSeries(BubbleScatter(BubblesAt(values, 
                [.. regions.Select(r => r.Measure.Centroid.X)], [.. regions.Select(r => r.Measure.Centroid.Y)])), configure);
        }

        /// <summary>Adds Natural Earth 110m coastlines to the axes. Coastlines are LineString
        /// geometries — <paramref name="color"/> sets the stroke color (default: black).</summary>
        public AxesBuilder Coastlines(IGeoProjection projection,
            Color? color = null, double lineWidth = CoastlineWidth)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var features = NaturalEarth110m.Coastlines();
            var series = new GeoPolygonSeries(features, projection)
            {
                StrokeColor = color ?? Colors.Black,
                StrokeWidth = lineWidth,
                Label = "Coastlines"
            };
            return ax.AddSeries(series);
        }

        /// <summary>Adds the Natural Earth 110m boundaries between countries on land to the axes, as cartopy's <c>BORDERS</c> draws them
        /// (a coast is the coastlines' to draw). <paramref name="color"/> sets the stroke color (default: the theme's neutral grey).</summary>
        public AxesBuilder Borders(IGeoProjection projection,
            Color? color = null, double lineWidth = BorderWidth)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var features = NaturalEarth110m.BoundaryLines();
            var series = new GeoPolygonSeries(features, projection)
            {
                StrokeColor = color,
                WearsNeutral = color is null,
                StrokeWidth = lineWidth,
                Label = "Borders"
            };
            return ax.AddSeries(series);
        }

        /// <summary>Fills ocean areas with the specified color. Ocean is rendered as a single
        /// polygon filling the projection's outline, as cartopy's ocean does: the oval of a world map, the circle of a globe, the
        /// rectangle of the bounds for a cylindrical or a conic map (drawn first so land/coastlines layer on top).</summary>
        public AxesBuilder Ocean(IGeoProjection projection, Color color)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var oceanFeature = new GeoFeature(
                GeoGeometry.OfPolygons([GeoPolygon.FromRings([.. projection.Outline()], [])]),
                new Dictionary<string, string>());

            var series = new GeoPolygonSeries([oceanFeature], projection)
            {
                Color = color,
                Label = "Ocean",
                ZOrder = -2,  // behind everything
                IsRawProjected = true,  // ring uses projected (X, Y), not (lon, lat)
            };
            return ax.AddSeries(series);
        }

        /// <summary>Fills land areas with the specified color using country polygons.</summary>
        public AxesBuilder Land(IGeoProjection projection, Color color)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var features = NaturalEarth110m.Countries();
            var series = new GeoPolygonSeries(features, projection)
            {
                Color = color,
                Label = "Land",
                ZOrder = -1,  // above ocean, below coastlines/borders
            };
            return ax.AddSeries(series);
        }
    }

    /// <summary>How wide the lines of a map are drawn when the verb that adds them is given no width: the coastlines, the borders between
    /// countries, and the outline of each region of a map coloured or resized by a value. The one place these widths are written; the
    /// verbs' default parameters read them. They are the library's look under every theme (cartopy names no width of its own for a
    /// feature: it draws one at matplotlib's <c>patch.linewidth</c>, 1.0 under both the classic and the v2 style), so they are values,
    /// not a look a theme chooses. The width of the coastlines.</summary>
    internal const double CoastlineWidth = 0.5;

    /// <summary>The width of the borders between countries on land (<see cref="CoastlineWidth"/>).</summary>
    internal const double BorderWidth = 0.3;

    /// <summary>The width of the outline of each region of a choropleth, a map of H3 cells and a cartogram (<see cref="CoastlineWidth"/>).</summary>
    internal const double RegionEdgeWidth = 0.3;

    /// <summary>The area of the bubble of the largest value on a map, in points squared (<see cref="BubblesAt"/>).</summary>
    internal const double LargestBubbleArea = 400;

    /// <summary>The places that have a value, with the area of the bubble each draws: the one rule for the size of a bubble on a map,
    /// its area in proportion to its value and the largest value's <see cref="LargestBubbleArea"/>. A place whose value is no
    /// quantity (zero, below zero, no number, infinite) has no bubble and takes nothing from the others. <c>Bubbles</c> and
    /// <c>Dorling</c> both draw by it.</summary>
    /// <param name="x">The projected X of each place, one per value.</param>
    /// <param name="y">The projected Y of each place, one per value.</param>
    /// <returns>The bubbles, in the order of the places; none when no value is a quantity.</returns>
    /// <param name="values">The value it works on.</param>
    internal static BubblePlaces BubblesAt(double[] values, double[] x, double[] y)
    {
        int drawn = 0;
        double largest = 0;
        foreach (double value in values)
        {
            if (!value.IsPositiveFinite()) continue;
            drawn++;
            largest = Math.Max(largest, value);
        }

        var bx = new double[drawn];
        var by = new double[drawn];
        var areas = new double[drawn];
        int at = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (!values[i].IsPositiveFinite()) continue;
            bx[at] = x[i];
            by[at] = y[i];
            areas[at++] = LargestBubbleArea * values[i] / largest;
        }

        return new BubblePlaces(bx, by, areas);
    }

    /// <summary>The bubbles of a map: the places that have a quantity, each with the area of its bubble.</summary>
    /// <param name="X">The projected X of each bubble.</param>
    /// <param name="Y">The projected Y of each bubble.</param>
    /// <param name="Areas">The area of each bubble, in points squared (a scatter's size: its square root is the points across).</param>
    internal readonly record struct BubblePlaces(double[] X, double[] Y, double[] Areas);

    /// <summary>A polygon of a region as the map draws it: its rings in the plane of the projection.</summary>
    /// <param name="Exterior">The ring that bounds the polygon.</param>
    /// <param name="Holes">The rings cut out of it.</param>
    internal readonly record struct ProjectedPolygon(IReadOnlyList<ProjectedPoint> Exterior, IReadOnlyList<IReadOnlyList<ProjectedPoint>> Holes);

    /// <summary>What a region measures in the plane of its projection.</summary>
    /// <param name="Area">The area of its polygons, their holes taken out.</param>
    /// <param name="Centroid">The centre of that area.</param>
    internal readonly record struct RegionMeasure(double Area, ProjectedPoint Centroid);

    /// <summary>The cells of a map of H3 cells: each a region with its index, and the value it is coloured by.</summary>
    /// <param name="Cells">The region of each cell, its index in the property <see cref="H3IndexProperty"/>.</param>
    /// <param name="Values">The value of each cell, parallel to <paramref name="Cells"/>.</param>
    internal readonly record struct H3CellMap(List<GeoFeature> Cells, double[] Values);
}
