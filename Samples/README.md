# MatPlotLibNet Samples

Runnable sample projects demonstrating the MatPlotLibNet charting library.

## Console

Creates charts and exports to SVG, PNG, and PDF.

```
dotnet run --project MatPlotLibNet.Samples.Console
```

Produces: `chart.svg`, `chart.png`, `chart.pdf`, `dashboard.svg`, `heatmap_colormap.svg`, `colormap_comparison.svg`, `gridspec_layout.svg`, `tick_locators.svg`, `bar_labels.svg`, `lttb_downsampling.svg`, `annotations_enhanced.svg`, `financial_dashboard.svg`, `phase_f_indicators.svg`, `contour_labels.svg`, `scientific_paper.svg`, `sparkline_dashboard.svg`

### v0.6.0 samples (12-17)

- **12. Vec** -- SIMD-accelerated numeric computation (returns, mean, std, min/max)
- **13. Financial dashboard** -- `FigureTemplates.FinancialDashboard()` with Bollinger Bands + RSI
- **14. Phase F indicators** -- WilliamsR, OBV, CCI, ParabolicSAR in a 2x2 grid
- **15. Contour labels** -- `ShowLabels` with `LabelFormat` on contour plot
- **16. Scientific paper** -- `FigureTemplates.ScientificPaper()` with 150 DPI, hidden spines
- **17. Sparkline dashboard** -- `FigureTemplates.SparklineDashboard()` with server metrics

## Blazor

Blazor Server app with static and real-time charts.

```
dotnet run --project MatPlotLibNet.Samples.Blazor
```

- `/` -- static bar chart and scatter plot
- `/live` -- real-time chart updating every 3 seconds via SignalR

## Web API

ASP.NET Core minimal API with REST endpoints and SignalR hub.

```
dotnet run --project MatPlotLibNet.Samples.WebApi
```

- `GET /api/chart/sales` -- chart as JSON
- `GET /api/chart/sales.svg` -- chart as SVG
- `/charts-hub` -- SignalR hub (subscribe to `sensor-1` for live updates)

## GraphQL

HotChocolate GraphQL server with queries and subscriptions.

```
dotnet run --project MatPlotLibNet.Samples.GraphQL
```

- `/graphql` -- BananaCakePop playground
- Query: `{ chartSvg(chartId: "demo") }`
- Subscription: `subscription { onChartSvgUpdated(chartId: "live-sensor") }`

## Note

All samples use `<ProjectReference>` to build from source. No NuGet packages required.
