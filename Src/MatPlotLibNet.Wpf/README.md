# MatPlotLibNet.Wpf

MatPlotLibNet.Wpf is a WPF chart control for MatPlotLibNet. It renders charts natively with SkiaSharp.

## Requirements

The package targets `net8.0-windows10.0.19041.0`, because `SkiaSharp.Views.WPF` ships no WPF view for a lower
Windows platform version. A project on a bare `net8.0-windows`, `net9.0-windows` or `net10.0-windows` has platform
version 7.0 and fails to restore with NU1202. Name the version in your own project file:

```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
```

The same works for `net9.0-` and `net10.0-windows10.0.19041.0`.

## Quick Start

```xml
<wpf:MplChartControl Figure="{Binding MyFigure}" IsInteractive="True" />
```

## Live data

For a chart that keeps changing, bind a `StreamingFigure` to `MplStreamingChartControl` instead. It listens to
the figure and redraws itself when data arrives, so the figure stays the same object for the life of the chart:

```xml
<wpf:MplStreamingChartControl StreamingFigure="{Binding LiveChart}" />
```

## Interaction

The chart control supports all 10 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), **legend drag (press-and-hold a legend item to reposition; v1.7.2)**, crosshair, and hover tooltip.
