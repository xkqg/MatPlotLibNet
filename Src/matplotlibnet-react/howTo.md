# How to use @matplotlibnet/react

## Install

```
npm install @matplotlibnet/react @microsoft/signalr
```

## 1. Static chart

Fetch and render SVG from a MatPlotLibNet.AspNetCore endpoint:

```tsx
import { MplChart } from '@matplotlibnet/react';

function Dashboard() {
  return <MplChart chartUrl="/api/chart/sales.svg" cssClass="my-chart" />;
}
```

## 2. Real-time chart via SignalR

Subscribe to live updates pushed by `IChartPublisher` on the server:

```tsx
import { MplLiveChart } from '@matplotlibnet/react';

function LiveDashboard() {
  return (
    <MplLiveChart
      chartId="sensor-1"
      hubUrl="/charts-hub"
      cssClass="live"
      initialSvg="<svg><text x='10' y='20'>Loading...</text></svg>"
    />
  );
}
```

## 3. Using hooks directly

### useMplChart

Fetches chart SVG and exposes loading/error state:

```typescript
import { useMplChart } from '@matplotlibnet/react';

function MyChart() {
  const { svgContent, loading, error } = useMplChart('/api/chart.svg');

  if (loading) return <p>Loading...</p>;
  if (error) return <p>Error: {error.message}</p>;
  return <div dangerouslySetInnerHTML={{ __html: svgContent }} />;
}
```

### useMplLiveChart

Manages SignalR connection lifecycle automatically:

```typescript
import { useMplLiveChart } from '@matplotlibnet/react';

function LiveSensor() {
  const { svgContent, isConnected } = useMplLiveChart('sensor-1', '/charts-hub');

  return (
    <div>
      <span>{isConnected ? 'Connected' : 'Connecting...'}</span>
      <div dangerouslySetInnerHTML={{ __html: svgContent }} />
    </div>
  );
}
```

## 4. Low-level SignalR client

For full control over the connection:

```typescript
import { ChartSubscriptionClient } from '@matplotlibnet/react';

const client = new ChartSubscriptionClient();

client.onSvgUpdated((chartId, svg) => {
  console.log(`Chart ${chartId} updated`);
  document.getElementById('chart')!.innerHTML = svg;
});

client.onChartUpdated((chartId, json) => {
  console.log(`Chart ${chartId} JSON:`, json);
});

await client.connect('/charts-hub');
await client.subscribe('sensor-1');

// Later:
await client.unsubscribe('sensor-1');
await client.dispose();
```

## 5. Multiple charts

```tsx
function MultiChart() {
  return (
    <div className="grid">
      <MplChart chartUrl="/api/chart/sales.svg" />
      <MplChart chartUrl="/api/chart/traffic.svg" />
      <MplLiveChart chartId="sensor-1" />
      <MplLiveChart chartId="sensor-2" />
    </div>
  );
}
```

## 6. Server setup

Requires a MatPlotLibNet.AspNetCore backend:

```csharp
builder.Services.AddMatPlotLibNetSignalR();

app.MapChartHub();
app.MapChartSvgEndpoint("/api/chart/sales.svg", ctx => BuildSalesChart());
```

## 7. CORS configuration

If the React app runs on a different port (e.g. Vite on :5173):

```csharp
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

app.UseCors();
```

## 8. Proxy configuration (Vite)

In `vite.config.ts`:

```typescript
export default defineConfig({
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/charts-hub': { target: 'http://localhost:5000', ws: true }
    }
  }
});
```
