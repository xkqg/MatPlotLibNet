# MatPlotLibNet.Geo

MatPlotLibNet.Geo adds geographic projections and maps to MatPlotLibNet: coastlines, borders, filled land and ocean, countries
coloured by a value, places, bubbles, hexagon bins, flows and cartograms.

## Quick Start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Coastlines()
        .Borders())
    .Save("world_map.svg");
```

A map axes has one projection. `WithProjection` records it and every layer reads it, so `Land(color)`, `Ocean(color)`,
`Borders()` and `Coastlines()` need no projection of their own. A layer asked for before the axes has a projection is refused
with "call WithProjection first", and a layer that is given a projection has to give the axes' own (the same kind with the
same numbers) or it is refused with an `ArgumentException` naming both.

## A map coloured by a value

`Choropleth` colours each country by a value through a colour map. The values are joined to the built-in countries by their
three-letter code (`ADM0_A3`), or `Choropleth("POP_EST")` reads a numeric property of the features:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling.ColorMaps;

var populationMillions = new Dictionary<string, double> { ["FRA"] = 68, ["DEU"] = 84, ["ESP"] = 48 };

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Choropleth(populationMillions, ColorMaps.Viridis)
        .WithColorBar())
    .Save("choropleth.svg");
```

A country with no value is drawn in the colour map's bad colour, and the colour bar reads the range of the values that can be
drawn.

## Data on a map

| Verb | Draws |
|---|---|
| `Points(latitudes, longitudes)` | a scatter of places |
| `Bubbles(latitudes, longitudes, values)` | a bubble at each place, its area in proportion to its value |
| `HexBins(latitudes, longitudes)` | hexagon bins in the plane of the map |
| `Flows(flows, color, maxWidth)` | a line along the great circle for each `GeoFlow`, as wide as its quantity, cut where it crosses the antimeridian |
| `Cartogram(values)` | each country resized so that its area is in proportion to its value, about its own centre |
| `Dorling(values)` | a bubble at the centre of each country |

A place the projection cannot place, such as the far side of an orthographic globe, is left out.

## A map in JSON

A figure that holds a map comes back from JSON whole: the series writes its projection (its kind and the numbers it was made
with), its features (the id of the built-in set, or a GeoJSON FeatureCollection with the holes kept), and its values, colour
map and normaliser. The package registers the reader when it loads; a process that reads such JSON before it has used any type
of this package calls `GeoPolygonSeries.RegisterReader()` once. A 1.23.0 reader leaves the map out with the diagnostic for a
series kind it does not know.

## Projections

| Projection | Use case |
|---|---|
| `PlateCarree` | Simplest, identity mapping |
| `Mercator` | Web maps, navigation |
| `Robinson` | World maps (compromise) |
| `Orthographic` | Globe view |
| `LambertConformal` | Mid-latitude regions (US, Europe) |

The package ships 13 projections; the [cookbook page](https://xkqg.github.io/MatPlotLibNet/cookbook/geographic.html) lists them
all and shows every layer with a picture.

## Data

The package embeds Natural Earth data at 110m resolution:
- Coastlines (~50KB)
- Country borders (~150KB)

The data comes from [Natural Earth](https://www.naturalearthdata.com/) and is public domain.
