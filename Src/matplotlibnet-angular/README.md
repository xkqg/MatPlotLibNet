# @matplotlibnet/angular

Angular components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts as inline SVG with optional real-time SignalR updates.

## Installation

```
npm install @matplotlibnet/angular @microsoft/signalr
```

## Components

### MplChartComponent -- static chart

Fetches SVG from a MatPlotLibNet.AspNetCore endpoint and renders it inline.

```html
<mpl-chart [chartUrl]="'/api/chart.svg'" [cssClass]="'my-chart'"></mpl-chart>
```

### MplLiveChartComponent -- real-time updates via SignalR

Connects to a ChartHub and updates automatically when the server pushes new data.

```html
<mpl-live-chart [chartId]="'sensor-1'" [hubUrl]="'/charts-hub'" [cssClass]="'live'"></mpl-live-chart>
```

### ChartService -- HTTP client

```typescript
import { ChartService } from '@matplotlibnet/angular';

constructor(private chartService: ChartService) {}

this.chartService.getSvg('/api/chart.svg').subscribe(svg => { ... });
this.chartService.getJson('/api/chart').subscribe(spec => { ... });
```

### ChartSubscriptionClient -- SignalR client

Low-level client mirroring the C# `IChartSubscriptionClient` interface:

```typescript
const client = new ChartSubscriptionClient();
client.onSvgUpdated((chartId, svg) => { ... });
await client.connect('/charts-hub');
await client.subscribe('sensor-1');
```

## Server Setup

Requires a MatPlotLibNet.AspNetCore backend:

```csharp
builder.Services.AddMatPlotLibNetSignalR();
app.MapChartHub();
app.MapChartSvgEndpoint("/api/chart.svg", ctx => BuildChart());
```

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
