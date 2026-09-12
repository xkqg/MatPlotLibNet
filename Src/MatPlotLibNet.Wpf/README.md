# MatPlotLibNet.Wpf

MatPlotLibNet.Wpf is a WPF chart control for MatPlotLibNet. It renders charts natively with SkiaSharp.

## Quick Start

```xml
<wpf:MplChartControl Figure="{Binding MyFigure}" IsInteractive="True" />
```

The control supports all 10 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), **legend drag (press-and-hold a legend item to reposition; v1.7.2)**, crosshair, and hover tooltip.
