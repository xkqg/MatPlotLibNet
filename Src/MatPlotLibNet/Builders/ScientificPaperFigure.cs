// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The layout of <see cref="FigureTemplates.ScientificPaper(int, int, Action{AxesBuilder}[], string, double, double)"/>:
/// a grid of open-framed panels at 150 dpi with a tight layout, one callback per panel, row by row.</summary>
internal sealed class ScientificPaperFigure : FacetedFigure
{
    /// <summary>The resolution a paper figure is drawn at.</summary>
    private const double PaperDpi = 150;

    private readonly int _rows, _cols;
    private readonly Action<AxesBuilder>[] _configures;

    /// <summary>Makes the paper figure of <paramref name="rows"/> by <paramref name="cols"/> panels.</summary>
    /// <param name="rows">Number of subplot rows.</param>
    /// <param name="cols">Number of subplot columns.</param>
    /// <param name="configures">One callback per subplot, in row-major order.</param>
    /// <exception cref="ArgumentException"><paramref name="configures"/> does not hold one callback per subplot.</exception>
    internal ScientificPaperFigure(int rows, int cols, Action<AxesBuilder>[] configures)
    {
        configures.Length.ThrowIfMisaligned(rows * cols, nameof(configures));

        _rows = rows;
        _cols = cols;
        _configures = configures;
    }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        fb.WithDpi(PaperDpi)
          .TightLayout();

        for (int i = 0; i < _configures.Length; i++)
        {
            var configure = _configures[i];
            fb.AddSubPlot(_rows, _cols, i + 1, ax =>
            {
                ax.OpenFrame();
                configure(ax);
            });
        }
    }
}
