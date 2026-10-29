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

namespace MatPlotLibNet.Geo;

/// <summary>Extension methods for adding geographic features to an <see cref="AxesBuilder"/>.
/// Provides a cartopy-like API: <c>ax.WithProjection(Robinson).Coastlines().Borders()</c>.</summary>
/// <remarks>A map axes has ONE projection: <c>WithProjection</c> records it, and every layer reads it. A layer verb given a
/// projection of its own must give the one the axes has (the same kind with the same parameters); a layer verb given none
/// before the axes has one is refused.</remarks>
public static class GeoAxesExtensions
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
            var place = projection.Forward(latitudes[i], longitudes[i]);
            if (double.IsNaN(place.X) || double.IsNaN(place.Y)) continue;
            x.Add(place.X);
            y.Add(place.Y);
            kept.Add(i);
        }

        return new([.. x], [.. y], [.. kept]);
    }

    /// <summary>A scatter of bubbles: the area of each in proportion to its value, the largest 400 square points. A place of no
    /// value (zero, below zero, no number) has none.</summary>
    private static ScatterSeries BubblesOf(double[] x, double[] y, double[] values)
    {
        var drawn = Enumerable.Range(0, values.Length).Where(i => values[i] > 0 && double.IsFinite(values[i])).ToArray();
        double largest = drawn.Length == 0 ? 1 : drawn.Max(i => values[i]);
        return new ScatterSeries([.. drawn.Select(i => x[i])], [.. drawn.Select(i => y[i])])
        {
            Sizes = [.. drawn.Select(i => 400 * values[i] / largest)],
        };
    }

    /// <summary>A country to resize, drawn by a value.</summary>
    private readonly record struct Resizable(GeoFeature Feature, IReadOnlyList<ProjectedPolygon> Polygons, RegionMeasure Measure, double Value);

    /// <summary>The countries that have a value above zero and an area on <paramref name="projection"/>.</summary>
    private static List<Resizable> ResizableRegions(IGeoProjection projection, IReadOnlyDictionary<string, double> valueOf)
    {
        var regions = new List<Resizable>();
        foreach (var country in NaturalEarth110m.Countries())
        {
            if (!country.Properties.TryGetValue("ADM0_A3", out var code) || !valueOf.TryGetValue(code, out double value)
                || !(value > 0) || !double.IsFinite(value)) continue;
            var polygons = country.ProjectedBy(projection);
            if (polygons.Measure() is { } measure) regions.Add(new Resizable(country, polygons, measure, value));
        }

        return regions;
    }

    /// <summary>The line of a flow in the plane of the map: its great circle cut into pieces, each piece projected; where the
    /// circle crosses the antimeridian, or a position cannot be placed, the line has a gap (a pair of NaN), so the pieces are not
    /// joined across the map.</summary>
    private static FlowPath FlowLine(IGeoProjection projection, GeoFlow flow)
    {
        var x = new List<double>();
        var y = new List<double>();
        double? previousLon = null;
        foreach (var place in GreatCircle.Between(flow.From, flow.To, 64))
        {
            var at = projection.Forward(place.Latitude, place.Longitude);
            bool crosses = previousLon is { } before && Math.Abs(place.Longitude - before) > 180;
            if ((crosses || double.IsNaN(at.X)) && x.Count > 0 && !double.IsNaN(x[^1]))
            {
                x.Add(double.NaN);
                y.Add(double.NaN);
            }

            if (!double.IsNaN(at.X) && !double.IsNaN(at.Y))
            {
                x.Add(at.X);
                y.Add(at.Y);
            }

            previousLon = place.Longitude;
        }

        return new FlowPath([.. x], [.. y]);
    }

    /// <summary>The points of a flow's line in the plane of the map, with a pair of NaN where the line has a gap.</summary>
    private readonly record struct FlowPath(double[] X, double[] Y);

    extension(AxesBuilder ax)
    {
        /// <summary>Sets the map projection for this axes and configures it for geographic display
        /// (hides standard axes chrome). The layers that follow (<c>Coastlines</c>, <c>Borders</c>, <c>Ocean</c> and <c>Land</c>)
        /// are drawn on it; one that is given a projection of its own must give this one.</summary>
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
        public AxesBuilder Coastlines(Color? color = null, double lineWidth = 0.5) =>
            ax.Coastlines(OfTheAxes(ax), color, lineWidth);

        /// <summary>Adds the Natural Earth country borders on the axes' projection (<see cref="WithProjection"/>).</summary>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Borders(Color? color = null, double lineWidth = 0.3) =>
            ax.Borders(OfTheAxes(ax), color, lineWidth);

        /// <summary>Fills the ocean on the axes' projection (<see cref="WithProjection"/>).</summary>
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
            ax.ChoroplethOfCountries(projection, country => country.Properties.TryGetValue("ADM0_A3", out var code)
                && valueOf.TryGetValue(code, out double value) ? value : double.NaN, colorMap, configure);

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
            return ax.AddSeries(new GeoPolygonSeries(countries, projection)
            {
                Values = [.. countries.Select(valueOf)],
                ColorMap = colorMap,
                WearsNeutral = true,
                StrokeWidth = 0.3,
                Label = "Choropleth",
                ZOrder = -1,
            }, configure);
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

        /// <summary>Marks places on the map with bubbles whose area is in proportion to a value: the largest value has the area of
        /// 400 square points (a scatter's size, so a bubble is drawn by the scatter rule), the others in proportion.</summary>
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
            return ax.AddSeries(BubblesOf(places.X, places.Y, [.. places.Kept.Select(i => values[i])]), configure);
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

        /// <summary>Draws flows between places: a line along the great circle from the start of each to its end, as wide as its
        /// quantity (the largest flow is <paramref name="maxWidth"/> wide). A line that crosses the antimeridian is cut where it
        /// leaves one edge of the map and starts again at the other; a flow with an end the projection cannot place, and a flow of
        /// no quantity (zero, below zero, no number), is not drawn.</summary>
        /// <param name="flows">The flows.</param>
        /// <param name="color">The colour of every line; the lines share the next colour of the axes when none is given.</param>
        /// <param name="maxWidth">The width of the largest flow, in points.</param>
        /// <param name="configure">Sets each <see cref="LineSeries"/> that is drawn (once for each flow).</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Flows(IReadOnlyList<GeoFlow> flows, Color? color = null, double maxWidth = 4,
            Action<LineSeries>? configure = null)
        {
            var projection = OfTheAxes(ax);
            var drawn = flows.Where(flow => flow.Value > 0 && double.IsFinite(flow.Value)
                && !double.IsNaN(projection.Forward(flow.From.Latitude, flow.From.Longitude).X)
                && !double.IsNaN(projection.Forward(flow.To.Latitude, flow.To.Longitude).X)).ToList();
            if (drawn.Count == 0) return ax;

            double largest = drawn.Max(flow => flow.Value);
            int group = ax.NextColorGroup();
            foreach (var flow in drawn)
            {
                var line = FlowLine(projection, flow);
                ax.AddSeries(new LineSeries(line.X, line.Y)
                {
                    LineWidth = maxWidth * flow.Value / largest,
                    Color = color,
                    ColorGroup = color is null ? group : null,
                }, configure);
            }

            return ax;
        }

        /// <summary>Resizes each country by a value without moving its centre: a non-contiguous cartogram. The area of a country is
        /// in proportion to its value, and the countries together keep the area they have on the map. A country of no value
        /// (zero, below zero, no number) or that is not in <paramref name="valueOf"/> is not drawn.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (<c>ADM0_A3</c>).</param>
        /// <param name="configure">Sets the <see cref="GeoPolygonSeries"/> (fill, outline, label).</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Cartogram(IReadOnlyDictionary<string, double> valueOf, Action<GeoPolygonSeries>? configure = null)
        {
            var projection = OfTheAxes(ax);
            var regions = ResizableRegions(projection, valueOf);
            double scale = regions.Count == 0 ? 0 : regions.Sum(r => r.Measure.Area) / regions.Sum(r => r.Value);
            var resized = regions.Select(r => new GeoFeature(
                r.Polygons.ScaledAbout(r.Measure.Centroid, Math.Sqrt(scale * r.Value / r.Measure.Area)),
                new Dictionary<string, string>(r.Feature.Properties))).ToList();
            return ax.AddSeries(new GeoPolygonSeries(resized, projection)
            {
                IsRawProjected = true,
                Color = Colors.Tab10Blue,
                WearsNeutral = true,
                StrokeWidth = 0.3,
                Label = "Cartogram",
                ZOrder = -1,
            }, configure);
        }

        /// <summary>Draws a circle for each country at the centre of its area, as large as its value: a Dorling cartogram without the
        /// relaxing of circles that overlap. The area of the circle is in proportion to the value, the largest being 400 square
        /// points, as for <see cref="Bubbles"/>.</summary>
        /// <param name="valueOf">The value of each country by its three-letter code (<c>ADM0_A3</c>); a country of no value is not drawn.</param>
        /// <param name="configure">Sets the <see cref="ScatterSeries"/>.</param>
        /// <exception cref="InvalidOperationException">The axes was given no projection.</exception>
        public AxesBuilder Dorling(IReadOnlyDictionary<string, double> valueOf, Action<ScatterSeries>? configure = null)
        {
            var regions = ResizableRegions(OfTheAxes(ax), valueOf);
            return ax.AddSeries(BubblesOf([.. regions.Select(r => r.Measure.Centroid.X)], [.. regions.Select(r => r.Measure.Centroid.Y)],
                [.. regions.Select(r => r.Value)]), configure);
        }

        /// <summary>Adds Natural Earth 110m coastlines to the axes. Coastlines are LineString
        /// geometries — <paramref name="color"/> sets the stroke color (default: black).</summary>
        public AxesBuilder Coastlines(IGeoProjection projection,
            Color? color = null, double lineWidth = 0.5)
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

        /// <summary>Adds Natural Earth 110m country borders to the axes. <paramref name="color"/>
        /// sets the stroke color (default: the theme's neutral grey).</summary>
        public AxesBuilder Borders(IGeoProjection projection,
            Color? color = null, double lineWidth = 0.3)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var features = NaturalEarth110m.Countries();
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
        /// polygon spanning the full projection bounds (drawn first so land/coastlines layer on top).</summary>
        public AxesBuilder Ocean(IGeoProjection projection, Color color)
        {
            projection = AgreeingWithTheAxes(ax, projection);
            var bounds = projection.Bounds;
            var oceanFeature = new GeoFeature(
                new GeoGeometry
                {
                    Type = "Polygon",
                    Rings = [[(bounds.XMin, bounds.YMin), (bounds.XMax, bounds.YMin),
                              (bounds.XMax, bounds.YMax), (bounds.XMin, bounds.YMax),
                              (bounds.XMin, bounds.YMin)]]
                },
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
}
