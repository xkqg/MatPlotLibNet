# MatPlotLibNet.Wpf

WPF chart control for MatPlotLibNet. Renders charts natively via SkiaSharp.

## Quick Start

```xml
<wpf:MplChartControl Figure="{Binding MyFigure}" IsInteractive="True" />
```

All 10 interaction modifiers work: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), **legend drag (press-and-hold a legend item to reposition; v1.7.2)**, crosshair, hover tooltip.
