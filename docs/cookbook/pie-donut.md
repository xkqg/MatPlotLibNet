# Pie & Donut Charts

## Pie chart

```csharp
double[] sizes = [35, 25, 20, 12, 8];
string[] labels = ["Python", "C#", "Java", "Go", "Rust"];

Plt.Create()
    .WithTitle("Language Popularity")
    .Pie(sizes, labels)
    .Save("pie.svg");
```

## Donut chart

```csharp
Plt.Create()
    .WithTitle("Revenue Split")
    .AddSubPlot(1, 1, 1, ax => ax
        .Donut(sizes, labels, s => s.InnerRadius = 0.4))
    .Save("donut.svg");
```

## Nested pie (sunburst)

```csharp
var root = new TreeNode
{
    Label = "Revenue",
    Children = new[]
    {
        new TreeNode { Label = "Electronics", Value = 42, Color = Colors.Blue },
        new TreeNode { Label = "Apparel", Value = 28, Color = Colors.Orange },
        new TreeNode { Label = "Grocery", Value = 30, Color = Colors.Green },
    }
};

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NestedPie(root))
    .Save("nested_pie.svg");
```
