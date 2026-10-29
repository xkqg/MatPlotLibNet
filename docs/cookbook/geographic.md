---
title: "Maps and map projections in C#"
description: "Draw maps in C# with 13 map projections, embedded Natural Earth coastlines and borders, GeoJSON, choropleth shading, bubbles, flows and cartograms."
---

# Geographic Maps

> **Requires:** `dotnet add package MatPlotLibNet.Geo`

## World map with coastlines (Robinson)

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;

Plt.Create()
    .WithTitle("World — Robinson Projection")
    .WithSize(1000, 500)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Coastlines(GeoProjection.Robinson)
        .Borders(GeoProjection.Robinson))
    .Save("world_robinson.svg");
```

![World map — Robinson](../images/geo_robinson.png)

## Globe view (Orthographic)

Center the globe on any point with `OrthographicAt(lat, lon)`:

```csharp
var proj = GeoProjection.OrthographicAt(45, -30);

Plt.Create()
    .WithTitle("Globe — 45°N, 30°W")
    .WithSize(600, 600)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(proj)
        .Ocean(proj, Css4Colors.LightBlue)
        .Land(proj, Css4Colors.LightGreen)
        .Coastlines(proj, Css4Colors.DarkBlue, lineWidth: 0.8)
        .Borders(proj, Colors.Gray, lineWidth: 0.3))
    .Save("globe.svg");
```

![Globe — Orthographic](../images/geo_globe.png)

## Full-featured world map (Mollweide)

Combine `Land()`, `Ocean()`, `Coastlines()`, and `Borders()` for a complete map:

```csharp
var proj = GeoProjection.Mollweide;

Plt.Create()
    .WithTitle("Equal-Area World Map — Mollweide")
    .WithSize(1000, 500)
    .WithTheme(Theme.Dark)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(proj)
        .Ocean(proj, Color.FromHex("#1a3a5c"))
        .Land(proj, Color.FromHex("#2d5a27"))
        .Coastlines(proj, Colors.White, lineWidth: 0.5)
        .Borders(proj, Color.FromHex("#888888"), lineWidth: 0.2))
    .Save("mollweide_full.svg");
```

## Centered projections

Several projections accept center coordinates:

```csharp
// Orthographic centered on Tokyo
var tokyo = GeoProjection.OrthographicAt(centerLat: 35.7, centerLon: 139.7);

// Stereographic centered on the North Pole
var northPole = GeoProjection.StereographicAt(centerLat: 90, centerLon: 0);

// Azimuthal equidistant centered on London
var london = GeoProjection.AzimuthalEquidistantAt(centerLat: 51.5, centerLon: -0.1);

// Transverse Mercator with custom central meridian
var utmZone33 = GeoProjection.TransverseMercatorAt(centerLon: 15);

// Albers Equal Area with custom standard parallels (US map)
var us = GeoProjection.AlbersEqualAreaWith(sp1: 29.5, sp2: 45.5, centerLon: -96, centerLat: 37.5);

// Lambert Conformal with custom standard parallels
var europe = GeoProjection.LambertConformalWith(sp1: 35, sp2: 65, centerLon: 10, centerLat: 52);
```

## Projection comparison (2×2 grid)

```csharp
var projections = new (string Name, IGeoProjection Proj)[]
{
    ("Robinson", GeoProjection.Robinson),
    ("Mollweide", GeoProjection.Mollweide),
    ("Natural Earth", GeoProjection.NaturalEarth),
    ("Equal Earth", GeoProjection.EqualEarth),
};

var builder = Plt.Create()
    .WithTitle("Projection Comparison")
    .WithSize(1200, 800);

for (int i = 0; i < 4; i++)
{
    var (name, proj) = projections[i];
    builder.AddSubPlot(2, 2, i + 1, ax => ax
        .WithProjection(proj)
        .Coastlines(proj, Css4Colors.DarkBlue, lineWidth: 0.6)
        .Land(proj, Colors.Wheat)
        .WithTitle(name));
}

builder.TightLayout().Save("projection_comparison.svg");
```

## Coastline and border styling

Fine-tune line appearance with `color` and `lineWidth` parameters:

```csharp
var proj = GeoProjection.Robinson;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(proj)
        // Thick coastlines in dark blue
        .Coastlines(proj, color: Colors.Navy, lineWidth: 1.2)
        // Thin borders in light gray
        .Borders(proj, color: Colors.LightGray, lineWidth: 0.15))
    .Save("styled_borders.svg");
```

## Loading custom GeoJSON

Use `GeoJsonReader.Parse()` to load your own GeoJSON files:

```csharp
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Series;

string json = File.ReadAllText("my_regions.geojson");
List<GeoFeature> features = GeoJsonReader.Parse(json);

var proj = GeoProjection.Robinson;

// Access feature properties
foreach (var feature in features)
{
    string name = feature.Properties["NAME"];
    Console.WriteLine($"Feature: {name}, Type: {feature.Geometry.Type}");
}

// Add custom features as a GeoPolygonSeries
var series = new GeoPolygonSeries(features, proj)
{
    Color = Colors.CornflowerBlue,
    StrokeColor = Css4Colors.DarkBlue,
    StrokeWidth = 0.5,
    Label = "My Regions"
};
```

## Edge handling

MatPlotLibNet handles common geographic edge cases automatically:

```csharp
using MatPlotLibNet.Geo.GeoJson;

// Russia crosses the antimeridian (dateline) — automatically split
var proj = GeoProjection.Robinson;

Plt.Create()
    .WithTitle("Dateline Handling — Russia renders correctly")
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(proj)
        .Land(proj, Css4Colors.LightGreen)
        .Coastlines(proj))
    .Save("dateline.svg");

// The GeoClipping utility handles edge cases:
// - SplitAtDateline()  — splits polygons crossing ±180° longitude
// - FilterNaN()        — removes NaN points from projected coordinates
// - ClipToBounds()     — clips to projection boundary
```

## A map coloured by a value (choropleth)

`Choropleth` colours each country by a value through a colour map. The values are joined to the built-in countries by their
three-letter code (`ADM0_A3`: FRA, NOR, and so on), or `Choropleth("POP_EST")` reads a numeric property of the features
instead (`GeoFeature.NumberOf` parses it invariantly). `WithColorBar()` shows what the colours mean, with ticks at round
numbers, and a country with no value takes the colour map's bad colour:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

var populationOf = NaturalEarth110m.Countries()
    .Where(country => country.NumberOf("POP_EST") > 0)
    .ToDictionary(country => country.Properties["ADM0_A3"], country => Math.Log10(country.NumberOf("POP_EST")));

Plt.Create()
    .WithTitle("Population by country (log10)")
    .WithSize(1000, 560)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Ocean(Color.FromHex("#dfeaf5"))
        .Choropleth(populationOf, ColorMaps.Plasma)
        .WithColorBar()
        .WithLegend(visible: false))
    .Save("choropleth_world.svg");
```

![A world map with each country coloured by its population](../images/choropleth_world.png)

## Bubbles on a map

`Points` and `Bubbles` mark places by latitude and longitude. The area of a bubble is in proportion to its value, the
largest 400 square points. A place the projection cannot place, such as the far side of an orthographic globe, is left out;
`HexBins(latitudes, longitudes)` bins places into hexagons in the plane of the map instead.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;

double[] cityLat = [35.68, 28.61, 31.23, -23.55, 19.43, 30.04, 19.08, 39.90, 23.81, 40.71, 6.52, 41.01, 51.51, -33.87];
double[] cityLon = [139.69, 77.21, 121.47, -46.63, -99.13, 31.24, 72.88, 116.41, 90.41, -74.01, 3.38, 28.98, -0.13, 151.21];
double[] cityMillions = [37.4, 31.0, 27.1, 22.0, 21.8, 21.3, 20.7, 20.5, 21.0, 18.8, 15.4, 15.5, 9.5, 5.3];

Plt.Create()
    .WithTitle("Largest cities — population in millions")
    .WithSize(1000, 520)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Ocean(Color.FromHex("#1a3a5c"))
        .Land(Color.FromHex("#2d5a27"))
        .Borders(Color.FromHex("#4c7a45"), 0.2)
        .Bubbles(cityLat, cityLon, cityMillions, s => { s.Color = Color.FromHex("#FFB000"); s.Alpha = 0.75; })
        .WithLegend(visible: false))
    .Save("bubble_map.svg");
```

![Fourteen amber bubbles at the largest cities on a dark green world map](../images/bubble_map.png)

## Flows between places

`Flows` draws a line along the great circle between the two places of each `GeoFlow`, as wide as its quantity. A flow that
crosses the antimeridian is cut where it leaves the map on one side and starts again on the other, as the one over the
Pacific is here:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;

GeoFlow[] routes =
[
    new(new GeoCoordinate(35.68, 139.69), new GeoCoordinate(37.77, -122.42), 9),   // Tokyo to San Francisco
    new(new GeoCoordinate(51.51, -0.13), new GeoCoordinate(40.71, -74.01), 7),     // London to New York
    new(new GeoCoordinate(-33.87, 151.21), new GeoCoordinate(51.51, -0.13), 4),    // Sydney to London
    new(new GeoCoordinate(-23.55, -46.63), new GeoCoordinate(6.52, 3.38), 3),      // São Paulo to Lagos
    new(new GeoCoordinate(31.23, 121.47), new GeoCoordinate(25.2, 55.27), 5),      // Shanghai to Dubai
];

Plt.Create()
    .WithTitle("Freight routes — width by tonnes")
    .WithSize(1000, 520)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Ocean(Color.FromHex("#1a3a5c"))
        .Land(Color.FromHex("#2d5a27"))
        .Flows(routes, Color.FromHex("#FFB000"), maxWidth: 4)
        .WithLegend(visible: false))
    .Save("flow_map.svg");
```

![Five amber curves between ports on a dark green world map, the widest from Tokyo to San Francisco](../images/flow_map.png)

## Countries resized by a value

`Cartogram` resizes each country so that its area is in proportion to its value, about its own centre, so every country
keeps its place and its shape and only its size changes. `Dorling` puts a bubble at the centre of each country instead. A
country with no value above zero, or that is not in the dictionary, is not drawn.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;

var populationOf = NaturalEarth110m.Countries()
    .Where(country => country.NumberOf("POP_EST") > 0)
    .ToDictionary(country => country.Properties["ADM0_A3"], country => country.NumberOf("POP_EST"));

Plt.Create()
    .WithTitle("Population — countries resized, and a bubble at each")
    .WithSize(1000, 960)
    .AddSubPlot(2, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Ocean(Color.FromHex("#dfeaf5"))
        .Cartogram(populationOf, s => { s.Color = Color.FromHex("#d95f02"); s.StrokeColor = Colors.White; })
        .WithLegend(visible: false))
    .AddSubPlot(2, 1, 2, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Ocean(Color.FromHex("#dfeaf5"))
        .Land(Color.FromHex("#cfcfcf"))
        .Dorling(populationOf, s => { s.Color = Color.FromHex("#d95f02"); s.Alpha = 0.7; })
        .WithLegend(visible: false))
    .Save("cartogram_dorling.svg");
```

![Above, the countries resized by population; below, a bubble at the centre of each country](../images/cartogram_dorling.png)

## One projection, and a map in JSON

A map axes has one projection: `WithProjection` records it and every layer reads it, so `Land(color)`, `Ocean(color)`,
`Borders()` and `Coastlines()` need no projection of their own (a layer asked for before the axes has one is refused with
"call WithProjection first"). A layer that is given a projection has to give the axes' own, the same kind with the same
numbers, or it is refused with an `ArgumentException` naming both.

A figure that holds a map comes back from JSON whole: the series writes its projection (its kind and the numbers it was made
with, by their PROJ names `lon_0`, `lat_0`, `lat_1`, `lat_2`), its features (the id of the built-in set, or a GeoJSON
FeatureCollection with the holes kept) and its values, colour map and normaliser. A process that reads such JSON before it has
used any type of `MatPlotLibNet.Geo` calls `GeoPolygonSeries.RegisterReader()` once. A region with a hole, such as Lesotho
inside South Africa, is drawn as one path with the hole left unfilled.

## Embedded data — Natural Earth 110m

MatPlotLibNet.Geo ships with embedded Natural Earth 110m data (no internet required):

```csharp
using MatPlotLibNet.Geo.Data;

// 134 coastline features (LineString geometries)
var coastlines = NaturalEarth110m.Coastlines();

// 177 country features (Polygon geometries with properties)
var countries = NaturalEarth110m.Countries();

// Access country properties
foreach (var country in countries)
{
    if (country.Properties.TryGetValue("NAME", out var name))
        Console.WriteLine(name);
}
```

## Available projections (13 total)

| Projection | API | Use case |
|---|---|---|
| PlateCarree | `GeoProjection.PlateCarree` | Simple equirectangular |
| Mercator | `GeoProjection.Mercator` | Web maps |
| Robinson | `GeoProjection.Robinson` | World maps (compromise) |
| Orthographic | `GeoProjection.OrthographicAt(lat, lon)` | Globe view |
| Lambert Conformal | `GeoProjection.LambertConformalWith(sp1, sp2, ...)` | Mid-latitude regions |
| Mollweide | `GeoProjection.Mollweide` | Global equal-area |
| Sinusoidal | `GeoProjection.Sinusoidal` | Simple equal-area |
| Albers Equal Area | `GeoProjection.AlbersEqualAreaWith(sp1, sp2, ...)` | US / continental maps |
| Azimuthal Equidistant | `GeoProjection.AzimuthalEquidistantAt(lat, lon)` | Polar / aviation |
| Stereographic | `GeoProjection.StereographicAt(lat, lon)` | Polar regions |
| Transverse Mercator | `GeoProjection.TransverseMercatorAt(lon)` | UTM zones |
| Natural Earth | `GeoProjection.NaturalEarth` | World maps (smooth) |
| Equal Earth | `GeoProjection.EqualEarth` | Modern equal-area |

## Fluent API reference

| Method | Parameters | Description |
|---|---|---|
| `.WithProjection(proj)` | `IGeoProjection` | Set the axes' one projection, hide axes chrome |
| `.Coastlines(...)` | `color?`, `lineWidth` (0.5) | Natural Earth 110m coastlines; takes the axes' projection, or one equal to it |
| `.Borders(...)` | `color?`, `lineWidth` (0.3) | Natural Earth 110m country borders; takes the axes' projection, or one equal to it |
| `.Ocean(color)` | `Color` | Fill ocean areas |
| `.Land(color)` | `Color` | Fill land areas |
| `.Choropleth(values, colorMap)`, `.Choropleth("POP_EST", colorMap)` | values by `ADM0_A3`, or a numeric property | Colour each country by a value |
| `.Points(lat, lon)`, `.Bubbles(lat, lon, values)`, `.HexBins(lat, lon)` | arrays of latitudes and longitudes | Places, bubbles and hexagon bins |
| `.Flows(flows, color, maxWidth)` | `GeoFlow[]` | A great-circle line for each flow, as wide as its quantity |
| `.Cartogram(values)`, `.Dorling(values)` | values by `ADM0_A3` | Countries resized by a value, or a bubble at each |
| `GeoJsonReader.Parse(json)` | `string` | Parse custom GeoJSON FeatureCollection |
| `NaturalEarth110m.Coastlines()` | — | 134 coastline features |
| `NaturalEarth110m.Countries()` | — | 177 country polygon features |

## Four-projection comparison

Render the same world dataset under four different projections in a 2 × 2 grid.

```csharp
var projections = new (string Name, IGeoProjection Proj)[]
{
    ("Mercator",    GeoProjection.Mercator),
    ("Robinson",    GeoProjection.Robinson),
    ("Mollweide",   GeoProjection.Mollweide),
    ("Equal Earth", GeoProjection.EqualEarth),
};

var fig = Plt.Create().WithTitle("Four Projections").WithSize(1200, 700);
for (int i = 0; i < projections.Length; i++)
{
    var (name, proj) = projections[i];
    fig.AddSubPlot(2, 2, i + 1, ax => ax
        .WithTitle(name)
        .WithProjection(proj)
        .Ocean(proj, Color.FromHex("#1a3a5c"))
        .Land(proj, Color.FromHex("#2d5a27"))
        .Coastlines(proj, Colors.White, lineWidth: 0.5)
        .Borders(proj, Color.FromHex("#888888"), lineWidth: 0.2));
}
fig.Save("geo_projection_grid.svg");
```

![Four-projection comparison](../images/geo_projection_grid.png)

## Night-side globe

This globe uses `Theme.Dark` and `OrthographicAt` to draw a dark ocean, muted land, and glowing coastlines.

```csharp
var proj = GeoProjection.OrthographicAt(20, 10);

Plt.Create()
    .WithTitle("Night-side Globe")
    .WithSize(700, 700)
    .WithTheme(Theme.Dark)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(proj)
        .Ocean(proj, Color.FromHex("#0a1628"))
        .Land(proj, Color.FromHex("#1a3a1a"))
        .Coastlines(proj, Color.FromHex("#7fbbff"), lineWidth: 0.8)
        .Borders(proj, Color.FromHex("#3a6a8a"), lineWidth: 0.25))
    .Save("geo_nightside.svg");
```

![Night-side globe](../images/geo_nightside.png)
