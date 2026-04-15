# @matplotlibnet/vue

Vue 3 components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts as inline SVG with optional real-time SignalR updates.

## Installation

```
npm install @matplotlibnet/vue @microsoft/signalr
```

## Components

### MplChart -- static chart

Fetches SVG from a MatPlotLibNet.AspNetCore endpoint and renders it inline.

```vue
<MplChart chartUrl="/api/chart.svg" cssClass="my-chart" />
```

### MplLiveChart -- real-time updates via SignalR

Connects to a ChartHub and updates automatically when the server pushes new data.

```vue
<MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" cssClass="live" />
```

## Composables

### useMplChart

```typescript
import { useMplChart } from '@matplotlibnet/vue';

const { svgContent, loading, error } = useMplChart('/api/chart.svg');
// or with a reactive ref:
const url = ref('/api/chart.svg');
const { svgContent } = useMplChart(url);
```

### useMplLiveChart

```typescript
import { useMplLiveChart } from '@matplotlibnet/vue';

const { svgContent, isConnected } = useMplLiveChart('sensor-1', '/charts-hub');
```

### ChartSubscriptionClient -- low-level SignalR client

```typescript
import { ChartSubscriptionClient } from '@matplotlibnet/vue';

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
