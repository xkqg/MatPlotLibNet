# Sankey Diagrams

## Process industry distribution (5-column cascade)

```csharp
Plt.Create()
    .WithTitle("Sankey — Process industry product distribution")
    .WithSize(1000, 600)
    .WithSankeyHover()
    .AddSubPlot(1, 1, 1, ax => ax
        .HideAllAxes()
        .Sankey(nodes, links, s =>
        {
            s.NodeWidth = 24;
            s.NodePadding = 14;
            s.Iterations = 20;
            s.LinkColorMode = SankeyLinkColorMode.Gradient;
        }))
    .Save("sankey.svg");
```

![Sankey — process distribution](../images/sankey_process_distribution.png)

## Income statement (J&J-style flow)

Sub-labels carry Y/Y change indicators coloured green (profit) or red (cost):

![Sankey — income statement](../images/sankey_income_statement.png)

## Customer journey alluvial (4 timesteps)

Explicit column pinning places the same page labels across time-steps:

![Sankey — customer journey](../images/sankey_customer_journey.png)

## Vertical orientation (top-to-bottom)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .HideAllAxes()
        .Sankey(nodes, links, s =>
        {
            s.Orient = SankeyOrientation.Vertical;
            s.LinkColorMode = SankeyLinkColorMode.Gradient;
        }))
    .Save("sankey_vertical.svg");
```

![Sankey — vertical](../images/sankey_vertical.png)

## Severity cascade (4 timepoints)

Dense many-to-many transitions where relaxation iterations minimize crossings:

![Sankey — severity cascade](../images/sankey_severity_cascade.png)
