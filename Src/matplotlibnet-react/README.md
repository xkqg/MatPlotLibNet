# @matplotlibnet/react

React components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts as inline SVG with optional real-time SignalR updates.

## Installation

```
npm install @matplotlibnet/react @microsoft/signalr
```

## Components

### MplChart -- static chart

Fetches SVG from a MatPlotLibNet.AspNetCore endpoint and renders it inline.

```tsx
<MplChart chartUrl="/api/chart.svg" cssClass="my-chart" />
```

### MplLiveChart -- real-time updates via SignalR

Connects to a ChartHub and updates automatically when the server pushes new data.

```tsx
<MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" cssClass="live" />
```

## Hooks

### useMplChart

```typescript
import { useMplChart } from '@matplotlibnet/react';

const { svgContent, loading, error } = useMplChart('/api/chart.svg');
```

### useMplLiveChart

```typescript
import { useMplLiveChart } from '@matplotlibnet/react';

const { svgContent, isConnected } = useMplLiveChart('sensor-1', '/charts-hub');
```

### ChartSubscriptionClient -- low-level SignalR client

```typescript
import { ChartSubscriptionClient } from '@matplotlibnet/react';

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
