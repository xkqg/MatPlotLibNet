# How to use @matplotlibnet/vue

## Install

```
npm install @matplotlibnet/vue @microsoft/signalr
```

## 1. Static chart

Fetch and render SVG from a MatPlotLibNet.AspNetCore endpoint:

```vue
<script setup>
import { MplChart } from '@matplotlibnet/vue';
</script>

<template>
  <MplChart chartUrl="/api/chart/sales.svg" cssClass="my-chart" />
</template>
```

## 2. Real-time chart via SignalR

Subscribe to live updates pushed by `IChartPublisher` on the server:

```vue
<script setup>
import { MplLiveChart } from '@matplotlibnet/vue';
</script>

<template>
  <MplLiveChart
    chartId="sensor-1"
    hubUrl="/charts-hub"
    cssClass="live"
    initialSvg="<svg><text x='10' y='20'>Loading...</text></svg>"
  />
</template>
```

## 3. Using composables directly

### useMplChart

Fetches chart SVG with reactive URL support:

```vue
<script setup>
import { ref } from 'vue';
import { useMplChart } from '@matplotlibnet/vue';

const url = ref('/api/chart/sales.svg');
const { svgContent, loading, error } = useMplChart(url);
</script>

<template>
  <p v-if="loading">Loading...</p>
  <p v-else-if="error">Error: {{ error.message }}</p>
  <div v-else v-html="svgContent" />
</template>
```

### useMplLiveChart

Manages SignalR connection lifecycle automatically:

```vue
<script setup>
import { useMplLiveChart } from '@matplotlibnet/vue';

const { svgContent, isConnected } = useMplLiveChart('sensor-1', '/charts-hub');
</script>

<template>
  <span>{{ isConnected ? 'Connected' : 'Connecting...' }}</span>
  <div v-html="svgContent" />
</template>
```

## 4. Low-level SignalR client

For full control over the connection:

```typescript
import { ChartSubscriptionClient } from '@matplotlibnet/vue';

const client = new ChartSubscriptionClient();

client.onSvgUpdated((chartId, svg) => {
  console.log(`Chart ${chartId} updated`);
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

```vue
<script setup>
import { MplChart, MplLiveChart } from '@matplotlibnet/vue';
</script>

<template>
  <div class="grid">
    <MplChart chartUrl="/api/chart/sales.svg" />
    <MplChart chartUrl="/api/chart/traffic.svg" />
    <MplLiveChart chartId="sensor-1" />
    <MplLiveChart chartId="sensor-2" />
  </div>
</template>
```

## 6. Server setup

Requires a MatPlotLibNet.AspNetCore backend:

```csharp
builder.Services.AddMatPlotLibNetSignalR();

app.MapChartHub();
app.MapChartSvgEndpoint("/api/chart/sales.svg", ctx => BuildSalesChart());
```

## 7. CORS configuration

If the Vue app runs on a different port (e.g. Vite on :5173):

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
