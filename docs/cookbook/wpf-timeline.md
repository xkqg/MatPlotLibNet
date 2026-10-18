---
title: "State timeline in a WPF app: click, pan, zoom and a playback marker"
description: "A complete WPF example of a state timeline with MplChartControl: events with a start and an end, a tooltip and a click per event, pan and zoom along time, stacked categories, and a marker that follows video playback."
---

# State timeline in a WPF app

> **Install:** `dotnet add package MatPlotLibNet.Wpf`
>
> Your project has to target a Windows version of 10.0.19041 or higher, for example
> `<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>`, because the WPF view that draws the chart does not
> exist below it. A project on a bare `net8.0-windows` fails to restore with NU1202.

A state timeline draws events that have a start and an end as coloured bars along a time axis. It suits a video
replay, a machine's up and down periods, or the phases of a process. This page builds one in a WPF window, then
turns it into a replay: click an event to jump to it, drag to move along the recording, scroll to zoom in, and a
marker that follows the playback position.

## The smallest version

The window holds one `MplChartControl`:

```xml
<Window x:Class="TimelineDemo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mpl="clr-namespace:MatPlotLibNet.Wpf;assembly=MatPlotLibNet.Wpf"
        Title="State timeline" Height="260" Width="800">
    <mpl:MplChartControl x:Name="Chart" IsInteractive="True" />
</Window>
```

and the code behind gives it a figure. Each `StateSegment` is a start, an end, a label drawn inside the bar, and a
colour:

```csharp
using System.Windows;
using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace TimelineDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        StateSegment[] events =
        [
            new(0, 15, "Event 1", Colors.Blue),
            new(30, 45, "Event 2", Colors.Orange),
            new(60, 90, "Event 3", Colors.Green),
        ];

        Chart.Figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .StateTimeline(events, s => s.Label = "Events")
                .SetXLim(0, 90)
                .SetXLabel("seconds"))
            .Build();
    }
}
```

With `IsInteractive="True"` you already have, without writing anything more:

- **Tooltip.** Pointing at an event shows its name and the stretch it covers, for example `Events: Event 2 (30–45)`.
- **Pan.** Dragging moves along the time axis, also when the drag starts on an event. The vertical axis stays put,
  because a timeline has nothing to scroll vertically.
- **Zoom.** The mouse wheel zooms in and out around the pointer, along time only.

## Which event did the reader click?

`DataPointClicked` fires when the reader clicks an event. A click is a press and a release on the same spot; moving
the pointer further than a few pixels makes it a drag instead.

```csharp
Chart.DataPointClicked += point =>
{
    // PointIndex is the event's position in the list you passed to StateTimeline.
    var clicked = events[point.PointIndex];
    video.Seek(clicked.Start);          // or point.DataX, the instant under the cursor
};
```

`point.AxesIndex` says which timeline it was when you stack several (next section).

## Several categories under each other

Give each category its own timeline in a stacked layout and let them share one time axis. The first one carries a
key, and the others point at it with `ShareX`. Zooming or dragging any of them then moves all of them.

```csharp
var figure = Plt.Create().WithSize(900, 420);

for (int lane = 0; lane < LaneNames.Length; lane++)
{
    int index = lane;
    figure.AddSubPlot(LaneNames.Length, 1, lane + 1, ax =>
    {
        ax.StateTimeline(Lanes[index], s => s.Label = LaneNames[index])
          .WithTitle(LaneNames[index])
          .SetXLim(0, Length);

        if (index > 0) ax.ShareX("time");
    }, key: lane == 0 ? "time" : null);
}

Chart.Figure = figure.Build();
```

## Time on the axis

Plain numbers work as seconds into a recording, as above. When your events carry real timestamps, pass them as OLE
automation dates and ask for a date axis; the tooltip then shows dates and times as well:

```csharp
var start = new DateTime(2026, 6, 1, 8, 0, 0);
StateSegment[] shift =
[
    new(start.ToOADate(), start.AddHours(6).ToOADate(), "Running", Colors.Green),
    new(start.AddHours(6).ToOADate(), start.AddHours(8).ToOADate(), "Stopped", Colors.Red),
];

Chart.Figure = Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(shift).SetXDateAxis())
    .Build();
```

## Changing the visible range from code

The visible range lives on each axes. Set it, then ask the control to repaint:

```csharp
foreach (var lane in Chart.Figure!.SubPlots)
{
    lane.XAxis.Min = 0;
    lane.XAxis.Max = 30;
}

Chart.InvalidateVisual();
```

## A marker for the playback position

`AxVLine` draws a vertical line, and hands you the line so you can move it later. Change its `Value` on every
playback tick and repaint; nothing is rebuilt, and the reader's zoom stays as it was:

```csharp
ReferenceLine? playhead = null;
// ... inside the axes configuration:   .AxVLine(0, line => playhead = line)

void OnPlaybackTick(double seconds)
{
    playhead!.Value = seconds;
    Chart.InvalidateVisual();
}
```

## All of it in one window

The sample project has this as a running window: three categories, a clock standing in for the video, a click that
seeks, a marker in every lane, and buttons that set the visible range. Open `MainWindow` of
`Samples/MatPlotLibNet.Samples.Wpf` and press **Replay timeline…**. The code behind it:

```csharp
using System.Windows;
using System.Windows.Threading;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Samples.Wpf;

public partial class ReplayWindow : Window
{
    private const double Length = 120.0;   // seconds in the recording

    private static readonly string[] LaneNames = ["Speech", "Motion", "Alerts"];

    // Events as an application would store them: one list per category, with a start and an end in seconds.
    private static readonly StateSegment[][] Lanes =
    [
        [new(0, 22, "Greeting", Colors.Blue), new(30, 65, "Explanation", Colors.Orange), new(80, 115, "Questions", Colors.Green)],
        [new(10, 18, "Walk-in", Colors.Gray), new(40, 52, "Gesture", Colors.RebeccaPurple), new(95, 110, "Walk-out", Colors.Gray)],
        [new(35, 38, "Phone", Colors.Red), new(88, 92, "Door", Colors.Red)],
    ];

    private readonly ReferenceLine[] _playheads = new ReferenceLine[LaneNames.Length];
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private double _position;

    public ReplayWindow()
    {
        InitializeComponent();

        Chart.Figure = BuildFigure();
        Chart.DataPointClicked += point => Seek(Lanes[point.AxesIndex][point.PointIndex].Start);
        _clock.Tick += (_, _) => Advance(0.1);

        ShowPosition();
    }

    private Figure BuildFigure()
    {
        var figure = Plt.Create().WithSize(900, 420);

        for (int lane = 0; lane < LaneNames.Length; lane++)
        {
            int index = lane;
            figure.AddSubPlot(LaneNames.Length, 1, lane + 1, ax =>
            {
                ax.StateTimeline(Lanes[index], s => s.Label = LaneNames[index])
                  .WithTitle(LaneNames[index])
                  .SetXLim(0, Length)
                  .AxVLine(0, line => _playheads[index] = line);

                if (index > 0) ax.ShareX("time");                       // the lanes share one time window
                if (index == LaneNames.Length - 1) ax.SetXLabel("seconds");
            }, key: lane == 0 ? "time" : null);
        }

        return figure.Build();
    }

    private void OnPlayClicked(object sender, RoutedEventArgs e)
    {
        _clock.IsEnabled = !_clock.IsEnabled;
        PlayButton.Content = _clock.IsEnabled ? "Pause" : "Play";
    }

    private void OnFirstClicked(object sender, RoutedEventArgs e) => ShowRange(0, 30);

    private void OnAllClicked(object sender, RoutedEventArgs e) => ShowRange(0, Length);

    /// <summary>Changes the visible time range from code. The lanes share one window, but each axes keeps its own
    /// limits, so the new range is written to all of them.</summary>
    private void ShowRange(double from, double to)
    {
        foreach (var lane in Chart.Figure!.SubPlots)
        {
            lane.XAxis.Min = from;
            lane.XAxis.Max = to;
        }

        Chart.InvalidateVisual();
    }

    /// <summary>Moves the playback position, and with it the marker in every lane.</summary>
    private void Seek(double seconds)
    {
        _position = Math.Clamp(seconds, 0, Length);
        foreach (var playhead in _playheads) playhead.Value = _position;

        ShowPosition();
        Chart.InvalidateVisual();
    }

    private void Advance(double seconds)
    {
        if (_position + seconds >= Length)
        {
            _clock.Stop();
            PlayButton.Content = "Play";
        }

        Seek(_position + seconds);
    }

    private void ShowPosition() => PositionText.Text = $"Position {_position:F1} s of {Length:F0} s — click an event to jump to it";
}
```

Replace `Seek` and the clock tick with your player: the click handler calls `Seek` with the start of the event, and
the player's position changed event calls `Seek` with the current time.
