# MatPlotLibNet.Geo

MatPlotLibNet.Geo adds geographic projections and map rendering to MatPlotLibNet.

## Quick Start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(GeoProjection.Robinson)
        .Coastlines(GeoProjection.Robinson)
        .Borders(GeoProjection.Robinson))
    .Save("world_map.svg");
```

## Projections

| Projection | Use case |
|---|---|
| `PlateCarree` | Simplest, identity mapping |
| `Mercator` | Web maps, navigation |
| `Robinson` | World maps (compromise) |
| `Orthographic` | Globe view |
| `LambertConformal` | Mid-latitude regions (US, Europe) |

## Data

The package embeds Natural Earth data at 110m resolution:
- Coastlines (~50KB)
- Country borders (~150KB)

The data comes from [Natural Earth](https://www.naturalearthdata.com/) and is public domain.
