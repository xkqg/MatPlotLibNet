# MatPlotLibNet.Geo

Geographic projections and map rendering for MatPlotLibNet.

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

Embeds Natural Earth 110m resolution data:
- Coastlines (~50KB)
- Country borders (~150KB)

Data is public domain from [Natural Earth](https://www.naturalearthdata.com/).
