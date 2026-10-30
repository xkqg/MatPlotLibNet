---
title: "Sankey diagrams in C#"
description: "Draw Sankey diagrams in C#: weighted flows between nodes, with hover focus that dims everything the node does not reach."
---

# Sankey Diagrams

> **Install:** `dotnet add package MatPlotLibNet`

`.WithSankeyHover()` enables ECharts-style `focus: adjacency`. When you hover a
node, the chart dims every link that is not reachable upstream or downstream
from that node, so the selected flow chain stands out. The nodes are keyboard
accessible: each node has `tabindex="0"`, so `Tab` moves the focus from node to
node. You can set the dim opacities on the theme with
`WithInteractionTheme(new InteractionTheme(SankeyDimLinkOpacity: 0.04, SankeyDimNodeOpacity: 0.2))`.

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

Sub-labels show year-over-year change indicators. They are coloured green for
profit and red for cost:

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

This chart has dense many-to-many transitions. The relaxation iterations
minimize the crossings:

![Sankey — severity cascade](../images/sankey_severity_cascade.png)

## Chord diagram: flows that go round

A Sankey diagram needs flows that run one way, from a column to the next. When the flows go both ways between the same groups,
as people moving between regions do, a chord diagram draws them as a ring of arcs with a ribbon through the centre for each flow.
An arc is as long as the row of its group, and a ribbon takes the colour of the group that sends more of the two.

```csharp
using MatPlotLibNet;

string[] regions = ["North", "South", "East", "West"];
double[,] moves =
{
    { 0, 12, 8, 3 },
    { 9, 0, 5, 7 },
    { 4, 6, 0, 10 },
    { 2, 8, 11, 0 },
};

Plt.Create()
    .WithTitle("People moving between regions")
    .WithSize(560, 560)
    .TightLayout()
    .AddSubPlot(1, 1, 1, ax => ax.Chord(regions, moves))
    .Save("region_chord.svg");
```

![A ring of four arcs, North, South, East and West, joined through the centre by ribbons](../images/region_chord.png)

The ribbons inside an arc run in the order that makes the ribbons of a complete graph cross least, the names stand outside the ring
and never read upside down, and a matrix that cannot be drawn (not square, a negative flow, all zero) is refused by name. The table
beside the chart is source, target and value for each flow there is.
