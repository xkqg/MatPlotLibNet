// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Threading;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Samples.Wpf;

/// <summary>
/// A replay application in one window: events laid out as one timeline per category, a marker that follows the
/// playback position, and a click on an event that seeks to it. There is no video here, only a clock that
/// stands in for one; replace <see cref="Seek"/> and the tick with your player.
/// <para>Drag pans along the recording, the wheel zooms along it, and all three lanes move together because they
/// share one time axis.</para>
/// </summary>
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
