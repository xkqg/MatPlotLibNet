# Geographic Maps

## World map with coastlines (Robinson)

```csharp
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Coastlines(GeoProjection.Robinson)
        .Borders(GeoProjection.Robinson))
    .Save("world_robinson.svg");
```

## Globe view (Orthographic)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.OrthographicAt(45, -30))
        .Coastlines(GeoProjection.OrthographicAt(45, -30), Colors.DarkBlue)
        .Ocean(GeoProjection.OrthographicAt(45, -30), Colors.LightBlue))
    .Save("globe.svg");
```

## Equal-area world map (Mollweide)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Mollweide)
        .Coastlines(GeoProjection.Mollweide)
        .Land(GeoProjection.Mollweide, Colors.LightGreen)
        .Ocean(GeoProjection.Mollweide, Colors.LightBlue))
    .Save("mollweide.svg");
```

## Available projections (13 total)

| Projection | API | Use case |
|---|---|---|
| PlateCarree | `GeoProjection.PlateCarree` | Simple equirectangular |
| Mercator | `GeoProjection.Mercator` | Web maps |
| Robinson | `GeoProjection.Robinson` | World maps (compromise) |
| Orthographic | `GeoProjection.OrthographicAt(lat, lon)` | Globe view |
| Lambert Conformal | `GeoProjection.LambertConformal` | Mid-latitude regions |
| Mollweide | `GeoProjection.Mollweide` | Global equal-area |
| Sinusoidal | `GeoProjection.Sinusoidal` | Simple equal-area |
| Albers Equal Area | `GeoProjection.AlbersEqualArea` | US maps |
| Azimuthal Equidistant | `GeoProjection.AzimuthalEquidistant` | Polar/aviation |
| Stereographic | `GeoProjection.Stereographic` | Polar regions |
| Transverse Mercator | `GeoProjection.TransverseMercator` | UTM zones |
| Natural Earth | `GeoProjection.NaturalEarth` | World maps (smooth) |
| Equal Earth | `GeoProjection.EqualEarth` | Modern equal-area |
