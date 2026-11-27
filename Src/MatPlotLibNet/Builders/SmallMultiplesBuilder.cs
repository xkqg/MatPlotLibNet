// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>Small multiples: one mini panel per series, wrapped into a grid, every panel on the SAME axes so
/// the shapes compare — Tufte's answer to the chart whose legend has become the picture. Beyond five or six
/// lines on one axes nobody can tell them apart; twenty panels, each named INSIDE its own frame, read as
/// twenty comparable shapes (Task Manager's per-core grid, Grafana's repeated panel).
/// <para>The builder owns the wrap (balanced — five panels at four per row read as 3+2, never 4+1), the shared
/// limits, the pinned window and the in-panel label, so a caller never writes row/column arithmetic. It is
/// rebuilt per frame by design: it holds no data of its own.</para></summary>
public sealed class SmallMultiplesBuilder
{
    private readonly List<PanelSpec> _panels = [];
    private int _maxCols = DefaultMaxCols;
    private double _panelWidth = DefaultPanelWidth;
    private double _panelHeight = DefaultPanelHeight;
    private MinMaxRange? _yLimits;
    private DateTime? _windowEnd;
    private TimeSpan _windowSpan;
    private string? _title;
    private Action<AxesBuilder>? _configurePanel;

    /// <summary>Panels per row before the grid wraps.</summary>
    public const int DefaultMaxCols = 4;

    /// <summary>A panel's default width, in figure units.</summary>
    public const double DefaultPanelWidth = 300;

    /// <summary>A panel's default height — a strip, not a chart: the shape is what a reader takes in.</summary>
    public const double DefaultPanelHeight = 120;

    /// <summary>Where the in-panel label sits, as a fraction of the panel: top-left, just inside the frame.</summary>
    private const double LabelX = 0.03;
    private const double LabelY = 0.88;

    /// <summary>A figure title above the grid.</summary>
    public SmallMultiplesBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Panels per row before the grid wraps (default <see cref="DefaultMaxCols"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCols"/> is below one.</exception>
    public SmallMultiplesBuilder WithMaxCols(int maxCols)
    {
        _maxCols = maxCols.ThrowIfBelow(1, nameof(maxCols));
        return this;
    }

    /// <summary>Every panel's size; the figure is the grid of them.</summary>
    public SmallMultiplesBuilder WithPanelSize(double width, double height)
    {
        _panelWidth = width;
        _panelHeight = height;
        return this;
    }

    /// <summary>One y range for every panel — what makes the shapes comparable.</summary>
    public SmallMultiplesBuilder WithSharedYLimits(double min, double max)
    {
        _yLimits = new MinMaxRange(min, max);
        return this;
    }

    /// <summary>Pin every panel's x axis to the same time window, with time-aware ticks.</summary>
    public SmallMultiplesBuilder WithWindow(DateTime end, TimeSpan span)
    {
        _windowEnd = end;
        _windowSpan = span;
        return this;
    }

    /// <summary>Runs on every panel after its line is placed — a reference line, a grid, a colour.</summary>
    public SmallMultiplesBuilder ConfigurePanel(Action<AxesBuilder> configure)
    {
        _configurePanel = configure;
        return this;
    }

    /// <summary>One panel: its name (drawn inside the frame) and its line.</summary>
    public SmallMultiplesBuilder AddPanel(string label, double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _panels.Add(new PanelSpec(label, x, y, configure));
        return this;
    }

    /// <summary>Lay the panels out. Balanced wrap, shared axes, label in the corner, no legend, no title per
    /// panel — a panel spends its whole height on the shape.</summary>
    /// <exception cref="InvalidOperationException">No panel was added.</exception>
    public FigureBuilder Build()
    {
        if (_panels.Count == 0)
        {
            throw new InvalidOperationException("Small multiples need at least one panel.");
        }

        var wrap = PanelWrap.Balanced(_panels.Count, _maxCols);

        var figure = Plt.Create()
            .WithSize(_panelWidth * wrap.Columns, _panelHeight * wrap.Rows)
            .WithGridSpec(wrap.Rows, wrap.Columns)
            .TightLayout();
        if (_title is not null)
        {
            figure.WithTitle(_title);
        }

        for (int i = 0; i < _panels.Count; i++)
        {
            var spec = _panels[i];
            figure.AddSubPlot(wrap.At(i), ax =>
            {
                ax.Plot(spec.X, spec.Y, spec.Configure);
                if (_yLimits is { } y)
                {
                    ax.SetYLim(y.Min, y.Max);
                }
                if (_windowEnd is { } end)
                {
                    ax.PinTimeWindow(end, _windowSpan);
                }
                ax.Annotate(spec.Label, LabelX, LabelY, a => a.Coordinates = AnnotationCoordinates.AxesFraction);
                ax.OpenFrame();
                ax.WithLegend(visible: false);
                _configurePanel?.Invoke(ax);
            });
        }

        return figure;
    }

    private readonly record struct PanelSpec(string Label, double[] X, double[] Y, Action<LineSeries>? Configure);
}
