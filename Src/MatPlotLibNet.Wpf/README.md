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

Set `IsInteractive="True"` and the chart control handles all 15 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click; a series with several legend entries, such as the named layers of a stack plot, toggles whole), reset (double-click, Home or Escape), data cursor (click on a point to pin it), the trendline, level and Fibonacci drawing tools, removing a selected drawing (Delete), crosshair (passive) and hover tooltip. Pointer positions are converted with the range and scale the chart is drawn with, on log and symlog axes too.

Clicking a data point raises `DataPointClicked` with the point's position in the arrays you plotted; on a
`StateTimelineSeries` it is the segment's position in the list you passed, and hovering a segment shows the event
and the stretch it covers. A timeline pans and zooms along time only, and stacked timelines that share one time axis
(`ShareX`) move together. The complete window, with a click that seeks and a marker that follows playback, is
[State timeline in WPF](https://xkqg.github.io/MatPlotLibNet/cookbook/wpf-timeline.html). To move a line while the chart is on screen, such as a playback marker, change its
`ReferenceLine.Value` and call `InvalidateVisual()` on the control. When you hand the control a different
`Figure`, pan and zoom follow the new one.
