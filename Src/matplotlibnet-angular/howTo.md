# How to use @matplotlibnet/angular

## Install

```
npm install @matplotlibnet/angular @microsoft/signalr
```

## 1. Import the module

```typescript
import { MatPlotLibNetModule } from '@matplotlibnet/angular';

@NgModule({
  imports: [MatPlotLibNetModule]
})
export class AppModule {}
```

Or import standalone components directly:

```typescript
import { MplChartComponent, MplLiveChartComponent } from '@matplotlibnet/angular';

@Component({
  imports: [MplChartComponent, MplLiveChartComponent],
  // ...
})
export class DashboardComponent {}
```

## 2. Static chart from an endpoint

`MplChartComponent` fetches SVG from a MatPlotLibNet.AspNetCore endpoint and renders it inline. It re-fetches whenever `chartUrl` changes.

```html
<mpl-chart [chartUrl]="'/api/chart.svg'" [cssClass]="'my-chart'"></mpl-chart>
```

### Server setup (ASP.NET Core)

```csharp
builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();

app.MapChartSvgEndpoint("/api/chart.svg", ctx =>
    Plt.Create()
        .WithTitle("Sales")
        .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])
        .Build());
```

### Dynamic URL from a route parameter

```typescript
@Component({
  template: `<mpl-chart [chartUrl]="chartUrl"></mpl-chart>`,
  imports: [MplChartComponent]
})
export class ChartPage {
  chartUrl = '';

  constructor(private route: ActivatedRoute) {
    this.route.params.subscribe(p => {
      this.chartUrl = `/api/chart/${p['type']}.svg`;
    });
  }
}
```

### Styling the container

```css
.my-chart {
  max-width: 800px;
  margin: 0 auto;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px;
}
```

## 3. Real-time chart with SignalR

`MplLiveChartComponent` connects to a `ChartHub` and updates automatically when the server pushes new data.

```html
<mpl-live-chart
  [chartId]="'sensor-1'"
  [hubUrl]="'/charts-hub'"
  [cssClass]="'live-chart'">
</mpl-live-chart>
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chartId` | `string` | `''` | Identifies which chart to subscribe to |
| `hubUrl` | `string` | `'/charts-hub'` | URL of the SignalR hub endpoint |
| `cssClass` | `string` | `''` | Additional CSS classes for the wrapper div |
| `initialSvg` | `string` | `''` | SVG to display before the first server push |

### Server-side publishing

```csharp
app.MapPost("/api/update", async (IChartPublisher publisher) =>
{
    var figure = Plt.Create()
        .WithTitle("Live Sensor Data")
        .Plot(timestamps, values, l => l.Color = Color.Red)
        .Build();

    await publisher.PublishSvgAsync("sensor-1", figure);
    return Results.Ok();
});
```

### How it works

1. Component calls `ChartSubscriptionClient.connect(hubUrl)` on init
2. Subscribes to the group identified by `chartId`
3. Listens for `UpdateChartSvg` messages from the server
4. Replaces the SVG content when a matching update arrives
5. Unsubscribes and disconnects on component destroy

## 4. Using ChartService directly

For custom fetch logic or programmatic chart loading:

```typescript
import { ChartService } from '@matplotlibnet/angular';

@Component({
  template: `<div [innerHTML]="svg"></div>`
})
export class CustomChartComponent implements OnInit {
  svg = '';

  constructor(private chartService: ChartService) {}

  ngOnInit() {
    this.chartService.getSvg('/api/chart.svg').subscribe(svg => {
      this.svg = svg;
    });
  }
}
```

### Fetch JSON spec

```typescript
this.chartService.getJson('/api/chart').subscribe(spec => {
  console.log(spec); // { title: "Sales", width: 800, ... }
});
```

## 5. Using ChartSubscriptionClient directly

For custom SignalR logic outside the component:

```typescript
import { ChartSubscriptionClient } from '@matplotlibnet/angular';

const client = new ChartSubscriptionClient();

client.onSvgUpdated((chartId, svg) => {
  document.getElementById(chartId)!.innerHTML = svg;
});

client.onChartUpdated((chartId, json) => {
  console.log('Chart JSON received:', json);
});

await client.connect('/charts-hub');
await client.subscribe('sensor-1');

// Later:
await client.unsubscribe('sensor-1');
await client.dispose();
```

## 6. Multiple charts on one page

```html
<div class="dashboard-grid">
  <mpl-chart [chartUrl]="'/api/sales.svg'" [cssClass]="'card'"></mpl-chart>
  <mpl-chart [chartUrl]="'/api/traffic.svg'" [cssClass]="'card'"></mpl-chart>
  <mpl-live-chart [chartId]="'cpu'" [cssClass]="'card'"></mpl-live-chart>
  <mpl-live-chart [chartId]="'memory'" [cssClass]="'card'"></mpl-live-chart>
</div>
```

Each `mpl-live-chart` maintains its own SignalR connection and subscription.

## 7. CORS configuration

If the Angular app runs on a different origin than the ASP.NET Core backend, configure CORS:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

app.UseCors();
```

## 8. Proxy configuration for development

In `proxy.conf.json` (Angular CLI dev server):

```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false
  },
  "/charts-hub": {
    "target": "http://localhost:5000",
    "secure": false,
    "ws": true
  }
}
```

Run with: `ng serve --proxy-config proxy.conf.json`
