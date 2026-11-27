"""
MatPlotLibNet matplotlib reference image generator.

Generates fixture PNGs for the fidelity test suite using pinned matplotlib 3.11.x.
Each fixture uses:
  - fixed RNG seed 42
  - figsize (8, 6) inches at DPI 100 → 800×600 px output
  - style: 'classic' (matplotlib pre-2.0 look matching Theme.MatplotlibClassic)

Usage:
    pip install -r requirements.txt
    python generate.py --all
    python generate.py --chart line
    python generate.py --chart line scatter bar

Output goes to: ../../Tst/MatPlotLibNet.Fidelity/Fixtures/Matplotlib/{classic|v2}/*.png, or to the folder --out names
(the same layout beneath it). Meta JSON is written alongside each PNG.

A chart whose numbers come from a random generator also writes the arrays it draws to data/<chart>.json beside the two style
folders. The fidelity checks read that file, so the check and the reference draw the same data (numpy's default_rng and
System.Random do not agree, and a score between two different point clouds measures the data, not the renderer).

A package a chart needs that is not installed is an error, never a substitute picture: the fixture is a reference, and a
reference drawn by another library than the one the chart names says nothing about the one under test.
"""

import argparse
import json
import os
import random
import sys
from pathlib import Path

import numpy as np
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.dates import date2num
from matplotlib.sankey import Sankey
from datetime import datetime, timedelta

# Packages used by a subset of the fixtures, loaded by the chart that needs them (see require): a missing package fails that
# chart, and a run for the charts that do not need it is not held up by it.
import importlib

from mpl_toolkits.mplot3d import Axes3D  # noqa: F401  (side-effect: registers 3d projection)

FIGSIZE = (8, 6)
DPI = 100
SEED = 42

# STYLE is mutated by main() per --style flag.
# Allowed values: "classic" (matplotlib pre-2.0) or "default" (modern matplotlib v2+).
# Each fig_* function reads this module-level constant via plt.style.context(STYLE).
STYLE = "classic"

# Output subdir name per style (matches Tst/.../Fixtures/Matplotlib/{classic|v2}/).
STYLE_DIR = {"classic": "classic", "default": "v2"}

FIXTURES_ROOT = Path(__file__).parent.parent.parent / "Tst" / "MatPlotLibNet.Fidelity" / "Fixtures" / "Matplotlib"


def require(module: str, pip_name: str | None = None):
    """Import a package a chart is built from, or stop the run. There is no fallback picture: a reference that stands in for
    another library's drawing would be scored against the wrong thing (the treemap and the seaborn references of an earlier
    run were a bar chart, a histogram and bars)."""
    try:
        return importlib.import_module(module)
    except ImportError as exc:
        raise SystemExit(
            f"ERROR: this chart is drawn by {module}, which is not installed: pip install {pip_name or module} "
            f"(tools/mpl_reference/requirements.txt lists the pinned versions). No substitute picture is written.") from exc


def out_dir() -> Path:
    d = FIXTURES_ROOT / STYLE_DIR[STYLE]
    d.mkdir(parents=True, exist_ok=True)
    return d


# The arrays the chart being generated draws, by name, written to data/<chart>.json by save() (see plotted).
_PLOTTED: dict[str, object] = {}


def plotted(**arrays) -> None:
    """Name the arrays this chart draws, so the fidelity check draws the same numbers: a chart whose numbers come from
    numpy's default_rng calls this once with every array it plots, after it has made them."""
    for name, values in arrays.items():
        _PLOTTED[name] = np.asarray(values).tolist()


def save_plotted(name: str) -> None:
    """Write what plotted() collected for the chart to data/<name>.json (the numbers do not depend on the style)."""
    if not _PLOTTED:
        return
    d = FIXTURES_ROOT / "data"
    d.mkdir(parents=True, exist_ok=True)
    # One line per array: json.dumps with an indent would put every number on its own line.
    lines = [json.dumps(key) + ":" + json.dumps(values, separators=(",", ":")) for key, values in _PLOTTED.items()]
    text = "{\n" + ",\n".join(lines) + "\n}\n"
    (d / f"{name}.json").write_text(text, encoding="utf-8", newline="\n")
    _PLOTTED.clear()


def meta(name: str) -> dict:
    return {
        "chart": name,
        "mpl_version": matplotlib.__version__,
        "figsize": list(FIGSIZE),
        "dpi": DPI,
        "style": STYLE,
        "seed": SEED,
    }


def save(fig: plt.Figure, name: str) -> None:
    """Write the chart's picture with its style still in force, as a script that calls plt.style.use and then savefig does.
    Some settings are read only when the figure is drawn, which savefig does: where an axis that scales itself stops
    (axes.autolimit_mode, 'round_numbers' in the classic style, so a classic axis ends on a tick), the text hinting, the
    offset of a tick formatter. Saved after the style's with-block had ended, a classic picture wore the default style's.
    The figure keeps its own colour (facecolor='auto', the style's figure.facecolor), as the library's theme draws it: the
    classic style's savefig.facecolor would fill a saved picture white where its window is grey."""
    od = out_dir()
    path = od / f"{name}.png"
    with plt.style.context(STYLE):
        fig.savefig(path, dpi=DPI, facecolor="auto", edgecolor="auto")
    plt.close(fig)
    (od / f"{name}.json").write_text(json.dumps(meta(name), indent=2))
    print(f"  wrote {path}")


def make(name: str):
    """Factory — returns chart function by name."""
    return CHARTS[name]


# ──────────────────────────────────────────────────────────────────────────────
# Chart fixture functions
# ──────────────────────────────────────────────────────────────────────────────

def fig_line_legend():
    x = np.linspace(0, 10, 50)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(x, np.sin(x),        label="sin(x)")
        ax.plot(x, np.cos(x),        label="cos(x)")
        ax.plot(x, np.sin(x + 1.5), label="sin(x+1.5)")
        ax.set_title("Line chart with legend")
        ax.set_xlabel("x")
        ax.set_ylabel("y")
        ax.legend()
    return fig


def fig_line_grid():
    """The line chart with legend, gridded: the one picture whose grid lines stand on the pixel grid as Agg puts a Line2D there."""
    fig = fig_line_legend()
    fig.axes[0].grid(True)
    return fig


def fig_scatter_markers():
    rng = np.random.default_rng(SEED)
    n = 80
    circles_x, circles_y = rng.normal(0, 1, n), rng.normal(0, 1, n)
    squares_x, squares_y = rng.normal(2, 1, n), rng.normal(2, 1, n)
    plotted(circles_x=circles_x, circles_y=circles_y, squares_x=squares_x, squares_y=squares_y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(circles_x, circles_y, marker="o", label="circles")
        ax.scatter(squares_x, squares_y, marker="s", label="squares")
        ax.set_title("Scatter — two marker types")
        ax.legend()
    return fig


def fig_bar_grouped():
    categories = ["A", "B", "C"]
    series1 = [3.2, 5.1, 2.8]
    series2 = [2.1, 4.3, 3.9]
    x = np.arange(len(categories))
    width = 0.35
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.bar(x - width / 2, series1, width, label="Series 1")
        ax.bar(x + width / 2, series2, width, label="Series 2")
        ax.set_xticks(x)
        ax.set_xticklabels(categories)
        ax.set_title("Grouped bar chart")
        ax.legend()
    return fig


def fig_hist_normal():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 1000)
    plotted(data=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.hist(data, bins=30)
        ax.set_title("Histogram — 1000 normal samples, 30 bins")
        ax.set_xlabel("value")
        ax.set_ylabel("count")
    return fig


def fig_pie_autopct():
    sizes = [35, 25, 20, 15, 5]
    labels = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.pie(sizes, labels=labels, autopct="%1.1f%%")
        ax.set_title("Pie chart")
    return fig


def fig_box_basic():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 1.0, 100) for loc in [0, 1, 2, 3]]
    plotted(groups=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.boxplot(data)
        ax.set_title("Box plot — 4 groups")
        ax.set_xlabel("group")
        ax.set_ylabel("value")
    return fig


def fig_violin_basic():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 1.0, 200) for loc in [0, 2, 4]]
    plotted(groups=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.violinplot(data, positions=[1, 2, 3])
        ax.set_title("Violin plot — 3 groups")
        ax.set_xlabel("group")
        ax.set_ylabel("value")
    return fig


def fig_imshow_viridis():
    rng = np.random.default_rng(SEED)
    data = rng.uniform(0, 1, (10, 10))
    plotted(data=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        # nearest: the cells stay cells. The classic style smooths an image (image.interpolation is bilinear), the library's heatmap
        # draws each cell as one flat colour, and a smoothed reference would score the smoothing, not the renderer.
        im = ax.imshow(data, cmap="viridis", aspect="auto", interpolation="nearest")
        fig.colorbar(im, ax=ax)
        ax.set_title("Heatmap 10×10 (viridis)")
    return fig


def fig_contour_peaks():
    x = np.linspace(-3, 3, 100)
    y = np.linspace(-3, 3, 100)
    X, Y = np.meshgrid(x, y)
    Z = (1 - X / 2 + X**5 + Y**3) * np.exp(-X**2 - Y**2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        cs = ax.contourf(X, Y, Z, levels=10)
        fig.colorbar(cs, ax=ax)
        ax.set_title("Filled contour — peaks function")
    return fig


def fig_polar_rose():
    theta = np.linspace(0, 2 * np.pi, 300)
    r = np.abs(np.cos(3 * theta))
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE, subplot_kw={"projection": "polar"})
        ax.plot(theta, r)
        ax.set_title("Polar — 3-petal rose")
    return fig


def fig_candlestick():
    """20 synthetic OHLC bars using mpl_finance-style manual patches."""
    rng = np.random.default_rng(SEED)
    n = 20
    close = np.cumsum(rng.normal(0, 1, n)) + 100
    high  = close + rng.uniform(0.5, 2, n)
    low   = close - rng.uniform(0.5, 2, n)
    open_ = close + rng.normal(0, 0.5, n)
    plotted(open=open_, high=high, low=low, close=close)

    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        for i in range(n):
            color = "green" if close[i] >= open_[i] else "red"
            ax.plot([i, i], [low[i], high[i]], color="black", linewidth=1)
            ax.bar(i, abs(close[i] - open_[i]),
                   bottom=min(close[i], open_[i]),
                   color=color, width=0.6, linewidth=0)
        ax.set_title("Candlestick — 20 bars")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
    return fig


def fig_errorbar():
    rng = np.random.default_rng(SEED)
    x = np.arange(10)
    y = np.sin(x) + rng.normal(0, 0.1, 10)
    yerr = rng.uniform(0.05, 0.3, 10)
    plotted(x=x, y=y, yerr=yerr)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.errorbar(x, y, yerr=yerr, fmt="o-", capsize=4, label="data ± error")
        ax.set_title("Errorbar chart")
        ax.set_xlabel("x")
        ax.set_ylabel("y")
        ax.legend()
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — XY family (10)
# ──────────────────────────────────────────────────────────────────────────────

def fig_area():
    x = np.linspace(0, 10, 100)
    y = np.sin(x) + 1.2
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.fill_between(x, 0, y)
        ax.set_title("Area — fill_between")
        ax.set_xlabel("x")
        ax.set_ylabel("y")
    return fig


def fig_stacked_area():
    x = np.linspace(0, 10, 50)
    y1 = np.sin(x) + 2
    y2 = np.cos(x) + 2
    y3 = np.sin(x + 1) + 2
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.stackplot(x, y1, y2, y3, labels=["A", "B", "C"])
        ax.set_title("Stacked area")
        ax.legend(loc="upper right")
    return fig


def fig_step():
    x = np.arange(20)
    y = np.cumsum(np.ones(20))
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.step(x, y, where="mid")
        ax.set_title("Step function")
        ax.set_xlabel("x")
        ax.set_ylabel("cumulative")
    return fig


def fig_bubble():
    rng = np.random.default_rng(SEED)
    n = 40
    x = rng.uniform(0, 10, n)
    y = rng.uniform(0, 10, n)
    sizes = rng.uniform(50, 500, n)
    plotted(x=x, y=y, sizes=sizes)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(x, y, s=sizes, alpha=0.5)
        ax.set_title("Bubble chart")
    return fig


def fig_regression():
    rng = np.random.default_rng(SEED)
    x = np.linspace(0, 10, 30)
    y = 2 * x + 1 + rng.normal(0, 1.5, 30)
    plotted(x=x, y=y)
    coef = np.polyfit(x, y, 1)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(x, y, label="data")
        ax.plot(x, np.polyval(coef, x), label="linear fit")
        ax.set_title("Linear regression")
        ax.legend()
    return fig


def fig_residual():
    rng = np.random.default_rng(SEED)
    x = np.linspace(0, 10, 30)
    residuals = rng.normal(0, 1, 30)
    plotted(x=x, residuals=residuals)
    sns = require("seaborn")
    # The check draws the library's Residplot, which fits a line and plots what is left, as seaborn's residplot does: the reference
    # is seaborn's own, so both draw one chart (its dots, its ':' zero line and its range).
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.residplot(x=x, y=residuals, ax=ax)
        ax.set_title("Residual plot")
        ax.set_xlabel("x")
        ax.set_ylabel("residual")
    return fig


def fig_ecdf():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 100)
    sorted_data = np.sort(data)
    y = np.arange(1, len(sorted_data) + 1) / len(sorted_data)
    plotted(data=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(sorted_data, y, drawstyle="steps-post")
        ax.set_title("ECDF")
        ax.set_xlabel("value")
        ax.set_ylabel("cumulative probability")
    return fig


def fig_signal():
    sample_rate = 100.0
    t = np.arange(0, 2, 1 / sample_rate)
    y = np.sin(2 * np.pi * 3 * t) + 0.3 * np.sin(2 * np.pi * 10 * t)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(t, y)
        ax.set_title("Signal — 3 Hz + 10 Hz")
        ax.set_xlabel("time (s)")
        ax.set_ylabel("amplitude")
    return fig


def fig_signalxy():
    rng = np.random.default_rng(SEED)
    x = np.sort(rng.uniform(0, 10, 60))
    y = np.sin(x) + 0.1 * rng.normal(size=60)
    plotted(x=x, y=y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(x, y)
        ax.set_title("Signal — irregular X")
        ax.set_xlabel("x")
        ax.set_ylabel("y")
    return fig


def fig_sparkline():
    rng = np.random.default_rng(SEED)
    y = np.cumsum(rng.normal(0, 1, 50))
    plotted(y=y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(y)
        # A sparkline is a line with no axes that fills its plot over the extent of its data: the library's SparklineSeries draws the
        # data's x from the first value to the last and its y from the lowest to the highest, edge to edge, with no axis, ticks or frame.
        ax.margins(0)
        ax.set_axis_off()
        ax.set_title("Sparkline")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Grid family (8)
# ──────────────────────────────────────────────────────────────────────────────

def fig_contour_lines():
    x = np.linspace(-3, 3, 100)
    y = np.linspace(-3, 3, 100)
    X, Y = np.meshgrid(x, y)
    Z = (1 - X / 2 + X**5 + Y**3) * np.exp(-X**2 - Y**2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        cs = ax.contour(X, Y, Z, levels=10)
        ax.clabel(cs, inline=True, fontsize=8)
        ax.set_title("Contour lines — peaks function")
    return fig


def fig_hexbin():
    rng = np.random.default_rng(SEED)
    x = rng.normal(0, 1, 2000)
    y = rng.normal(0, 1, 2000)
    plotted(x=x, y=y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        hb = ax.hexbin(x, y, gridsize=20, cmap="viridis")
        fig.colorbar(hb, ax=ax)
        ax.set_title("Hexbin — 2000 samples")
    return fig


def fig_hist2d():
    rng = np.random.default_rng(SEED)
    x = rng.normal(0, 1, 5000)
    y = rng.normal(0, 1, 5000)
    plotted(x=x, y=y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        h = ax.hist2d(x, y, bins=30, cmap="viridis")
        fig.colorbar(h[3], ax=ax)
        ax.set_title("2D histogram")
    return fig


def fig_pcolormesh():
    x = np.linspace(0, 10, 30)
    y = np.linspace(0, 10, 30)
    X, Y = np.meshgrid(x, y)
    Z = np.sin(X) * np.cos(Y)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        pc = ax.pcolormesh(X, Y, Z, cmap="viridis")
        fig.colorbar(pc, ax=ax)
        ax.set_title("Pcolormesh — sin(x)·cos(y)")
    return fig


def fig_image():
    # One number per pixel through a colour map: the library's image takes a matrix of values, not three colour channels, so the
    # reference draws what the library can (an RGB array of noise would give the two pictures different data).
    rng = np.random.default_rng(SEED)
    data = rng.uniform(0, 1, (30, 40))
    plotted(data=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.imshow(data, cmap="viridis", aspect="auto", interpolation="nearest")   # nearest in both styles, as the heatmap
        ax.set_title("Image 30×40 (viridis)")
    return fig


def fig_image_style():
    # A smooth wave, 170 rows by 220 columns, shown larger than its size but less than three times it and not at a whole multiple,
    # with no interpolation named: the style's image.interpolation scales it ('auto' under the default style, which is Hanning at this
    # size; 'bilinear' under the classic one). imshow centres each cell on its numbers, the first row on top.
    rows, cols = np.mgrid[0:170, 0:220]
    data = np.sin(cols / 14.0) * np.cos(rows / 11.0)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.imshow(data, cmap="viridis", aspect="auto")
        ax.set_title("Image 170×220, the style's interpolation")
    return fig


def fig_spectrogram():
    rng = np.random.default_rng(SEED)
    fs = 1000.0
    t = np.arange(0, 2, 1 / fs)
    signal = np.sin(2 * np.pi * 50 * t) + 0.5 * rng.normal(size=t.shape)
    plotted(signal=signal)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.specgram(signal, NFFT=256, Fs=fs, cmap="viridis")
        ax.set_title("Spectrogram — 50 Hz + noise")
    return fig


def fig_tricontour():
    rng = np.random.default_rng(SEED)
    n = 200
    x = rng.uniform(-3, 3, n)
    y = rng.uniform(-3, 3, n)
    z = np.exp(-(x**2 + y**2) / 2)
    plotted(x=x, y=y, z=z)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.tricontour(x, y, z, levels=10)
        ax.set_title("Tricontour — gaussian bump")
    return fig


def fig_tripcolor():
    rng = np.random.default_rng(SEED)
    n = 200
    x = rng.uniform(-3, 3, n)
    y = rng.uniform(-3, 3, n)
    z = np.exp(-(x**2 + y**2) / 2)
    plotted(x=x, y=y, z=z)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        tp = ax.tripcolor(x, y, z, cmap="viridis")
        fig.colorbar(tp, ax=ax)
        ax.set_title("Tripcolor — gaussian bump")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Field family (4)
# ──────────────────────────────────────────────────────────────────────────────

def fig_quiver():
    x = np.linspace(-2, 2, 10)
    y = np.linspace(-2, 2, 10)
    X, Y = np.meshgrid(x, y)
    U = -Y
    V = X
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        q = ax.quiver(X, Y, U, V)
        ax.quiverkey(q, 0.9, 0.9, 1, "1 unit", labelpos="E")
        ax.set_title("Quiver — rotational field")
    return fig


def fig_streamplot():
    x = np.linspace(-3, 3, 40)
    y = np.linspace(-3, 3, 40)
    X, Y = np.meshgrid(x, y)
    U = -1 - X**2 + Y
    V = 1 + X - Y**2
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.streamplot(X, Y, U, V)
        ax.set_title("Streamplot")
    return fig


def fig_barbs():
    x = np.linspace(-2, 2, 8)
    y = np.linspace(-2, 2, 8)
    X, Y = np.meshgrid(x, y)
    U = np.sin(X) * 20
    V = np.cos(Y) * 20
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.barbs(X, Y, U, V)
        ax.set_title("Barbs — wind field")
    return fig


def fig_stem():
    x = np.linspace(0, 2 * np.pi, 20)
    y = np.sin(x)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.stem(x, y)
        ax.set_title("Stem — sin(x)")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Polar family (3)
# ──────────────────────────────────────────────────────────────────────────────

def fig_polar_scatter():
    rng = np.random.default_rng(SEED)
    theta = rng.uniform(0, 2 * np.pi, 60)
    r = rng.uniform(0, 1, 60)
    plotted(theta=theta, r=r)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE, subplot_kw={"projection": "polar"})
        ax.scatter(theta, r)
        ax.set_title("Polar scatter")
    return fig


def fig_polar_bar():
    theta = np.linspace(0, 2 * np.pi, 12, endpoint=False)
    r = np.array([3, 5, 2, 6, 4, 7, 3, 5, 4, 6, 3, 5])
    width = 2 * np.pi / 12
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE, subplot_kw={"projection": "polar"})
        ax.bar(theta, r, width=width)
        ax.set_title("Polar bar")
    return fig


def fig_polar_heatmap():
    rng = np.random.default_rng(SEED)
    nr, ntheta = 10, 24
    theta = np.linspace(0, 2 * np.pi, ntheta + 1)
    r = np.linspace(0, 1, nr + 1)
    T, R = np.meshgrid(theta, r)
    Z = rng.uniform(0, 1, (nr, ntheta))
    plotted(data=Z)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE, subplot_kw={"projection": "polar"})
        ax.pcolormesh(T, R, Z, cmap="viridis")
        ax.set_title("Polar heatmap")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Categorical family (4)
# ──────────────────────────────────────────────────────────────────────────────

def fig_broken_bar():
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.broken_barh([(0, 3), (5, 2), (8, 4)], (10, 5))
        ax.broken_barh([(1, 2), (6, 3)], (20, 5))
        ax.set_ylim(5, 30)
        ax.set_xlim(0, 15)
        ax.set_title("Broken bar — broken_barh")
        ax.set_xlabel("time")
        ax.set_yticks([12.5, 22.5])
        ax.set_yticklabels(["task A", "task B"])
    return fig


def fig_eventplot():
    rng = np.random.default_rng(SEED)
    data = [rng.uniform(0, 10, 30) for _ in range(4)]
    plotted(rows=data)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.eventplot(data)
        ax.set_title("Eventplot — 4 rows")
    return fig


def fig_gantt():
    tasks = ["Design", "Build", "Test", "Ship"]
    starts = [0, 3, 8, 11]
    durations = [3, 5, 3, 2]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        for i, (start, dur) in enumerate(zip(starts, durations)):
            ax.barh(i, dur, left=start, height=0.6)
        ax.set_yticks(range(len(tasks)))
        ax.set_yticklabels(tasks)
        ax.set_xlabel("day")
        ax.set_title("Gantt chart")
        ax.invert_yaxis()
    return fig


def fig_waterfall():
    # An opening total of 100, four steps, and a closing total of 140: six bars. The totals stand on zero; each step stands on the
    # running total before it. The colours are written out, not taken from the style's cycle: in the classic style C2 and C3 are
    # red and cyan, which is not what any waterfall means, and the library under test draws increases green, decreases red.
    labels = ["Start", "A", "B", "C", "D", "End"]
    steps = [30, -20, 40, -10]
    running = [100]
    for step in steps:
        running.append(running[-1] + step)          # 100, 130, 110, 150, 140
    increase, decrease, total = "#2ca02c", "#d62728", "#1f77b4"
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.bar(0, running[0], color=total)
        for i, step in enumerate(steps):
            ax.bar(i + 1, step, bottom=running[i], color=increase if step >= 0 else decrease)
        ax.bar(len(labels) - 1, running[-1], color=total)
        ax.set_xticks(range(len(labels)))
        ax.set_xticklabels(labels)
        ax.set_title("Waterfall chart")
    return fig


def fig_fill_betweenx():
    # An area that lies down: at each height y the fill runs from x = 0 to the curve.
    y = np.arange(0, 11)
    x = np.array([2, 4, 3, 6, 5, 8, 6, 9, 7, 10, 8])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.fill_betweenx(y, x)
        ax.set_title("fill_betweenx")
    return fig


def fig_matshow():
    # A matrix shown as a grid of equal cells, the first row on top, with a colour bar. viridis in both styles (classic's
    # default map is jet).
    data = (np.arange(54).reshape(6, 9)) % 7
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        image = ax.matshow(data, cmap="viridis")
        fig.colorbar(image, ax=ax)
    return fig


def fig_set_bounds():
    # The left and bottom lines span only part of their axis (a range frame), the other two lines are gone, the ticks stay on the
    # left and the bottom.
    x = np.arange(0, 11)
    y = np.array([2, 3, 5, 4, 6, 8, 7, 9, 8, 10, 9])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(x, y)
        ax.spines["bottom"].set_bounds(2, 8)
        ax.spines["left"].set_bounds(3, 9)
        ax.spines["top"].set_visible(False)
        ax.spines["right"].set_visible(False)
        ax.xaxis.set_ticks_position("bottom")
        ax.yaxis.set_ticks_position("left")
        ax.set_title("set_bounds")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Distribution family (6, seaborn)
# ──────────────────────────────────────────────────────────────────────────────

def fig_kde():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 500)
    plotted(data=data)
    sns = require("seaborn")
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.kdeplot(data, ax=ax)
        ax.set_title("KDE — 500 normal samples")
    return fig


def fig_rugplot():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 100)
    plotted(data=data)
    sns = require("seaborn")
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.rugplot(data, ax=ax, height=0.3)
        ax.set_ylim(0, 1)
        ax.set_title("Rugplot")
    return fig


def fig_stripplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 0.5, 30) for loc in [0, 1, 2]]
    plotted(groups=data)
    sns = require("seaborn")
    # seaborn jitters a strip with numpy's global random state (uniform within 0.1 of the category), numbers no other library can
    # reproduce; the reference stands the dots on their categories (jitter=False) and the check draws the strip with Jitter = 0, so
    # the score compares the dots' size, colour and the axes, never two random point clouds.
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.stripplot(data=data, ax=ax, jitter=False)
        ax.set_title("Stripplot — 3 groups")
    return fig


def fig_swarmplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 0.5, 30) for loc in [0, 1, 2]]
    plotted(groups=data)
    sns = require("seaborn")
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.swarmplot(data=data, ax=ax)
        ax.set_title("Swarmplot — 3 groups")
    return fig


def fig_pointplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 1.0, 50) for loc in [0, 1, 2, 3]]
    plotted(groups=data)
    sns = require("seaborn")
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.pointplot(data=data, ax=ax, seed=SEED)   # the bars are a bootstrap: seeded, so a run draws the same bars
        ax.set_title("Pointplot — 4 groups")
    return fig


def fig_countplot():
    rng = np.random.default_rng(SEED)
    categories = rng.choice(["A", "B", "C", "D"], 200, p=[0.4, 0.3, 0.2, 0.1])
    plotted(categories=categories)
    sns = require("seaborn")
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        sns.countplot(x=categories, ax=ax)
        ax.set_title("Countplot")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — 3D family (5, mpl_toolkits.mplot3d)
# ──────────────────────────────────────────────────────────────────────────────

def fig_scatter3d():
    rng = np.random.default_rng(SEED)
    n = 60
    x = rng.normal(0, 1, n)
    y = rng.normal(0, 1, n)
    z = rng.normal(0, 1, n)
    plotted(x=x, y=y, z=z)
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, projection="3d")
        ax.scatter(x, y, z)
        ax.view_init(elev=30, azim=-60)
        ax.set_title("Scatter 3D")
    return fig


def fig_bar3d():
    xs = np.arange(4)
    ys = np.arange(4)
    x_mesh, y_mesh = np.meshgrid(xs, ys)
    x_flat = x_mesh.flatten()
    y_flat = y_mesh.flatten()
    z_flat = np.zeros_like(x_flat)
    dz = np.arange(16) + 1
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, projection="3d")
        ax.bar3d(x_flat, y_flat, z_flat, 0.8, 0.8, dz)
        ax.view_init(elev=30, azim=-60)
        ax.set_title("Bar 3D")
    return fig


def fig_surface():
    x = np.linspace(-3, 3, 40)
    y = np.linspace(-3, 3, 40)
    X, Y = np.meshgrid(x, y)
    Z = np.sin(np.sqrt(X**2 + Y**2))
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, projection="3d")
        ax.plot_surface(X, Y, Z, cmap="viridis")
        ax.view_init(elev=30, azim=-60)
        ax.set_title("Surface — sin(r)")
    return fig


def fig_wireframe():
    x = np.linspace(-3, 3, 20)
    y = np.linspace(-3, 3, 20)
    X, Y = np.meshgrid(x, y)
    Z = np.sin(np.sqrt(X**2 + Y**2))
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, projection="3d")
        ax.plot_wireframe(X, Y, Z)
        ax.view_init(elev=30, azim=-60)
        ax.set_title("Wireframe — sin(r)")
    return fig


def fig_stem3d():
    theta = np.linspace(0, 2 * np.pi, 20)
    x = np.cos(theta)
    y = np.sin(theta)
    z = theta
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, projection="3d")
        ax.stem(x, y, z)
        ax.view_init(elev=30, azim=-60)
        ax.set_title("Stem 3D — spiral")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Financial family (1)
# ──────────────────────────────────────────────────────────────────────────────

def fig_ohlc_bar():
    """OHLC bar chart — vertical high/low line + left tick for open, right tick for close."""
    rng = np.random.default_rng(SEED)
    n = 20
    close = np.cumsum(rng.normal(0, 1, n)) + 100
    high = close + rng.uniform(0.5, 2, n)
    low = close - rng.uniform(0.5, 2, n)
    open_ = close + rng.normal(0, 0.5, n)
    plotted(open=open_, high=high, low=low, close=close)

    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        for i in range(n):
            color = "green" if close[i] >= open_[i] else "red"
            ax.vlines(i, low[i], high[i], color=color, linewidth=1)
            ax.hlines(open_[i], i - 0.2, i, color=color, linewidth=1)
            ax.hlines(close[i], i, i + 0.2, color=color, linewidth=1)
        ax.set_title("OHLC bars — 20 bars")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Special family (4)
# ──────────────────────────────────────────────────────────────────────────────

def fig_sankey():
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(111, xticks=[], yticks=[], title="Sankey diagram")
        sankey = Sankey(ax=ax, scale=0.01, offset=0.2, head_angle=120,
                        format="%.0f", unit="%")
        sankey.add(flows=[25, 0, 60, -10, -20, -5, -15, -35],
                   labels=["", "", "", "First", "Second", "Third", "Fourth", "Fifth"],
                   orientations=[-1, 1, 0, 1, 1, 1, -1, -1])
        sankey.finish()
    return fig


def fig_table():
    columns = ["Freeze", "Wind", "Flood", "Quake", "Hail"]
    rows = ["%d year" % x for x in (100, 50, 20, 10, 5)]
    cell_text = [
        ["30", "12", "4", "2", "1"],
        ["80", "28", "11", "5", "2"],
        ["150", "45", "22", "12", "5"],
        ["210", "90", "50", "22", "11"],
        ["280", "120", "80", "45", "20"],
    ]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        # ax.table where it stands by default, loc="bottom": below the axes, its data columns sharing the axes' width, its row labels in a
        # column hung left of the axes and fitted to their text, no cell at the corner above them, the axes above drawn as any axes is.
        # Its six rows (Table.FONTSIZE * 1.2 pixels each at the figure's dpi, 16.67 at 100) need more than the style's bottom margin, so
        # the subplot parameters keep room for them below the axes and for the row labels left of it, before the table is made: a row's
        # height is a share of the axes' height when the table is made, which a layout run afterwards would scale.
        fig.subplots_adjust(left=0.2, bottom=0.3)
        ax.table(cellText=cell_text, rowLabels=rows, colLabels=columns)
        ax.set_title("Loss table")
    return fig


def fig_treemap():
    values = [500, 300, 200, 150, 100, 80, 60]
    labels = ["A", "B", "C", "D", "E", "F", "G"]
    squarify = require("squarify")
    # squarify.plot, given no colour, colours tile i with plt.get_cmap()(random.random()): the style's image.cmap at a draw of the
    # module-level random generator. The draws are made here, in the same order, and handed over, so the picture is the one
    # squarify draws; they do not depend on the style, so the check reads them and colours each tile through its theme's map.
    random.seed(SEED)
    draws = [random.random() for _ in values]
    plotted(draws=draws)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        cmap = plt.get_cmap()
        # squarify lays its tiles out in a norm_x by norm_y rectangle (100 by 100 unless told) and ax.bar stretches that to the axes, so the
        # tiles are squares in a square and oblongs on the page; the library squarifies in the pixels of its plot area, so the layout
        # rectangle is the axes' own size in pixels. squarify lays its tiles out from y = 0 in a Y that runs up, so the largest tile stands at
        # the bottom left, as the library's treemap does under the matplotlib themes. The tiles are not named: squarify centres a name, the
        # library writes it in a corner, so the names would be the only thing the two pictures do not share.
        width, height = ax.get_window_extent().size
        squarify.plot(sizes=values, norm_x=float(width), norm_y=float(height), ax=ax, alpha=0.8, color=[cmap(d) for d in draws])
        ax.axis("off")
        ax.set_title("Treemap")
    return fig


# Where the library puts the web of a radar in this 800 x 600 figure, in pixels of the picture: the centre, the radius of the outer
# ring and the baseline of the title. The library draws the web at three quarters of half the shorter side of its plot area, which
# is a different square under each style, so the reference stands its axes on the same pixels: a score is only worth reading
# when the check and the reference draw the same thing.
RADAR_WEB = {
    "classic": {"cx": 410.0, "cy": 300.0, "r": 180.0, "title_y": 53.06},
    "default": {"cx": 410.0, "cy": 303.0, "r": 173.25, "title_y": 63.67},
}


def _radar_axes(num_vars):
    """matplotlib has no radar axes. Its gallery's recipe (radar_chart.py with frame='polygon') is a polar axes whose patch and
    spine are a regular polygon and whose rings are drawn as polygons, with theta zero at north; the library's radar runs its
    categories clockwise, so the direction is turned too. The projection is registered once per number of spokes."""
    from matplotlib.patches import RegularPolygon
    from matplotlib.path import Path as MplPath
    from matplotlib.projections import register_projection
    from matplotlib.projections.polar import PolarAxes
    from matplotlib.spines import Spine
    from matplotlib.transforms import Affine2D

    class RadarTransform(PolarAxes.PolarTransform):
        def transform_path_non_affine(self, path):
            # A path with more than one interpolation step is a grid line: force the interpolation, which defeats
            # PolarTransform's conversion of a ring to a circular arc.
            if path._interpolation_steps > 1:
                path = path.interpolated(num_vars)
            return MplPath(self.transform(path.vertices), path.codes)

    class RadarAxes(PolarAxes):
        name = f"radar{num_vars}"
        PolarTransform = RadarTransform

        def __init__(self, *args, **kwargs):
            super().__init__(*args, **kwargs)
            self.set_theta_zero_location("N")
            self.set_theta_direction(-1)

        def _gen_axes_patch(self):
            return RegularPolygon((0.5, 0.5), num_vars, radius=0.5, edgecolor="k")

        def _gen_axes_spines(self):
            spine = Spine(axes=self, spine_type="circle", path=MplPath.unit_regular_polygon(num_vars))
            spine.set_transform(Affine2D().scale(0.5).translate(0.5, 0.5) + self.transAxes)
            return {"polar": spine}

    register_projection(RadarAxes)
    return RadarAxes.name


def fig_radar():
    categories = ["Speed", "Power", "Range", "Accuracy", "Cost"]
    values = [0.8, 0.6, 0.7, 0.9, 0.5]
    n = len(categories)
    angles = np.linspace(0, 2 * np.pi, n, endpoint=False).tolist()
    values_closed = values + values[:1]
    angles_closed = angles + angles[:1]
    web = RADAR_WEB[STYLE]
    width_px, height_px = FIGSIZE[0] * DPI, FIGSIZE[1] * DPI
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_axes(
            [(web["cx"] - web["r"]) / width_px, 1 - (web["cy"] + web["r"]) / height_px, 2 * web["r"] / width_px, 2 * web["r"] / height_px],
            projection=_radar_axes(n))
        # The outer ring is the largest value, as the library's MaxValue is.
        ax.set_ylim(0, max(values))
        # The library's radar outline is 2 pixels wide (RadarSeries.LineWidth is in pixels); matplotlib's linewidth is in points.
        ax.plot(angles_closed, values_closed, linewidth=2 * 72 / DPI)
        ax.fill(angles_closed, values_closed, alpha=0.25)
        ax.set_xticks(angles)
        ax.set_xticklabels(categories)
        # set_title puts the title at the axes' top and the pad above it: the pad that reaches the library's title baseline.
        ax.set_title("Radar — 5 axes", pad=((web["cy"] - web["r"]) - web["title_y"]) * 72 / DPI)
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 6 — Financial indicators (15, pandas_ta references)
# ──────────────────────────────────────────────────────────────────────────────

def _synthetic_ohlc(n=60, seed=SEED):
    """Closed-form OHLC + volume — NO RNG.  Must produce byte-identical values in C#.
    Formula: close = 100 + 5*sin(2πi/25) + 3*sin(2πi/7); high/low/open/volume derived.
    A closed-form approach (vs seeded RNG) is the only way to get exact cross-language
    correlation — Python PCG64 ≠ C# System.Random."""
    i = np.arange(n, dtype=float)
    close  = 100.0 + 5.0 * np.sin(2 * np.pi * i / 25) + 3.0 * np.sin(2 * np.pi * i / 7)
    high   = close + 1.0 + 0.5 * np.abs(np.sin(i * 0.5))
    low    = close - 1.0 - 0.5 * np.abs(np.cos(i * 0.5))
    open_  = close + 0.3 * np.sin(i * 0.7)
    volume = 2500.0 + 1500.0 * np.sin(i * 0.3)
    df = require("pandas").DataFrame({"open": open_, "high": high, "low": low, "close": close, "volume": volume})
    return df


def _pandas_ta():
    """pandas_ta, the library the indicator references are named after, computing with its own functions. pandas_ta switches to
    TA-Lib's functions wherever TA-Lib can be imported (one Imports dict that every indicator reads), and TA-Lib is installed for
    the ind_psar reference: the RSI, MACD, ATR, ADX, OBV, stochastic, Bollinger and Keltner references would then be TA-Lib's,
    whose first values and smoothing differ, and would change with what is installed."""
    ta = require("pandas_ta")
    ta.Imports["talib"] = False
    return ta


def fig_ind_sma():
    df = _synthetic_ohlc()
    sma = _pandas_ta().sma(df["close"], length=20)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, sma, label="SMA(20)")
        ax.set_title("SMA(20) overlay")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_ema():
    df = _synthetic_ohlc()
    ema = _pandas_ta().ema(df["close"], length=20)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, ema, label="EMA(20)")
        ax.set_title("EMA(20) overlay")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def _band(ax, index, lower, middle, upper, label):
    """The library's band verb as matplotlib draws it: the middle line, which names no colour and so takes the next colour of the
    cycle, and the channel between the upper and the lower line filled in that colour at alpha 0.15 with no edge. The legend names
    the middle line only, by the verb's label."""
    mid, = ax.plot(index, middle, label=label)
    ax.fill_between(index, upper, lower, color=mid.get_color(), alpha=0.15, linewidth=0)


def fig_ind_bbands():
    df = _synthetic_ohlc()
    bb = _pandas_ta().bbands(df["close"], length=20, std=2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        _band(ax, df.index, bb["BBL_20_2.0_2.0"], bb["BBM_20_2.0_2.0"], bb["BBU_20_2.0_2.0"], "BB(20,2)")
        ax.set_title("Bollinger Bands(20, 2)")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_macd():
    df = _synthetic_ohlc()
    macd = _pandas_ta().macd(df["close"], fast=12, slow=26, signal=9)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, macd.iloc[:, 0], label="MACD")
        ax.plot(df.index, macd.iloc[:, 2], label="Signal")
        ax.bar(df.index, macd.iloc[:, 1], width=0.6, label="Histogram")
        ax.set_title("MACD(12, 26, 9)")
        ax.set_xlabel("bar")
        ax.axhline(0, color="black", linewidth=0.5)
        ax.legend()
    return fig


def fig_ind_rsi():
    df = _synthetic_ohlc()
    rsi = _pandas_ta().rsi(df["close"], length=14)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, rsi, label="RSI(14)")
        ax.axhline(70, color="red", linestyle="--", linewidth=0.8)
        ax.axhline(30, color="green", linestyle="--", linewidth=0.8)
        ax.set_title("RSI(14)")
        ax.set_xlabel("bar")
        ax.set_ylabel("RSI")
        ax.set_ylim(0, 100)
        ax.legend()
    return fig


def fig_ind_stoch():
    df = _synthetic_ohlc()
    stoch = _pandas_ta().stoch(df["high"], df["low"], df["close"], k=14, d=3)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, stoch.iloc[:, 0], label="%K")
        ax.plot(df.index, stoch.iloc[:, 1], label="%D")
        ax.axhline(80, color="red", linestyle="--", linewidth=0.8)
        ax.axhline(20, color="green", linestyle="--", linewidth=0.8)
        ax.set_title("Stochastic(14, 3)")
        ax.set_xlabel("bar")
        ax.set_ylim(0, 100)
        ax.legend()
    return fig


def fig_ind_atr():
    df = _synthetic_ohlc()
    atr = _pandas_ta().atr(df["high"], df["low"], df["close"], length=14)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, atr, label="ATR(14)")
        ax.set_title("Average True Range(14)")
        ax.set_xlabel("bar")
        ax.set_ylabel("ATR")
        ax.legend()
    return fig


def fig_ind_adx():
    df = _synthetic_ohlc()
    adx = _pandas_ta().adx(df["high"], df["low"], df["close"], length=14)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        # By column name: pandas_ta returns ADX_14, ADXR_14_2, DMP_14, DMN_14, so the second and third columns are not the directional lines.
        ax.plot(df.index, adx["ADX_14"], label="ADX")
        ax.plot(df.index, adx["DMP_14"], label="+DI")
        ax.plot(df.index, adx["DMN_14"], label="-DI")
        ax.set_title("ADX(14)")
        ax.set_xlabel("bar")
        ax.legend()
    return fig


def _standard_cci(df, length):
    """The commodity channel index as Lambert defined it and TA-Lib computes it, (tp - sma(tp)) / (0.015 * mean deviation), from
    pandas_ta's own typical price, moving average and mean absolute deviation. pandas_ta 0.4.71b0's own cci (the path without TA-Lib)
    writes `typical_price - mean_typical_price / (c * mad_typical_price)`, a missing pair of brackets: on these bars it runs from
    -2374 to -1499 where the formula gives -171 to 208, so it is not drawn here."""
    ta = _pandas_ta()
    from pandas_ta.statistics import mad
    typical = ta.hlc3(df["high"], df["low"], df["close"])
    return (typical - ta.sma(typical, length=length)) / (0.015 * mad(typical, length=length))


def fig_ind_cci():
    df = _synthetic_ohlc()
    cci = _standard_cci(df, 20)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, cci, label="CCI(20)")
        ax.axhline(100, color="red", linestyle="--", linewidth=0.8)
        ax.axhline(-100, color="green", linestyle="--", linewidth=0.8)
        ax.set_title("Commodity Channel Index(20)")
        ax.set_xlabel("bar")
        ax.legend()
    return fig


def fig_ind_williamsr():
    df = _synthetic_ohlc()
    wr = _pandas_ta().willr(df["high"], df["low"], df["close"], length=14)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, wr, label="Williams %R(14)")
        ax.axhline(-20, color="red", linestyle="--", linewidth=0.8)
        ax.axhline(-80, color="green", linestyle="--", linewidth=0.8)
        ax.set_title("Williams %R(14)")
        ax.set_xlabel("bar")
        ax.set_ylim(-100, 0)
        ax.legend()
    return fig


def fig_ind_obv():
    df = _synthetic_ohlc()
    obv = _pandas_ta().obv(df["close"], df["volume"])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, obv, label="OBV")
        ax.set_title("On-Balance Volume")
        ax.set_xlabel("bar")
        ax.set_ylabel("OBV")
        ax.legend()
    return fig


def fig_ind_vwap():
    df = _synthetic_ohlc()
    # pandas_ta.vwap requires DatetimeIndex; compute manually to match our implementation.
    typical = (df["high"] + df["low"] + df["close"]) / 3
    vwap_vals = (typical * df["volume"]).cumsum() / df["volume"].cumsum()
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, vwap_vals, label="VWAP")
        ax.set_title("VWAP")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_keltner():
    df = _synthetic_ohlc()
    kc = _pandas_ta().kc(df["high"], df["low"], df["close"], length=20, scalar=2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        _band(ax, df.index, kc["KCLe_20_2"], kc["KCBe_20_2"], kc["KCUe_20_2"], "KC(20,2)")
        ax.set_title("Keltner Channels(20, 2)")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_ichimoku():
    df = _synthetic_ohlc(n=80)  # ichimoku needs more history
    ich, _ = _pandas_ta().ichimoku(df["high"], df["low"], df["close"])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close", color="black", linewidth=0.8)
        for i, name in enumerate(["SpanA", "SpanB", "Tenkan", "Kijun", "Chikou"]):
            if i < ich.shape[1]:
                ax.plot(df.index, ich.iloc[:, i], label=name, linewidth=0.8)
        ax.set_title("Ichimoku Cloud")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend(fontsize=8)
    return fig


def _wilder_sar(df):
    """Wilder's parabolic SAR as TA-Lib computes it (talib.SAR), split into the bars where it stands below the close (long) and above
    it (short); the first bar has none. pandas_ta 0.4.71b0's psar is another reading of Wilder: it holds the SAR out of the range of
    the bar before only, where Wilder and TA-Lib hold it out of the two bars before, and it seeds the first bar from the close, so on
    these bars it stands up to 0.93 away from TA-Lib's, in the first bars of each trend."""
    talib = require("talib", "TA-Lib")
    sar = talib.SAR(df["high"].to_numpy(), df["low"].to_numpy(), acceleration=0.02, maximum=0.2)
    below = sar < df["close"].to_numpy()
    pd = require("pandas")
    return pd.Series(np.where(below, sar, np.nan)), pd.Series(np.where(~below & ~np.isnan(sar), sar, np.nan))


def fig_ind_psar():
    df = _synthetic_ohlc()
    long, short = _wilder_sar(df)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        # The library's verb as matplotlib draws it: the long and the short dots as two scatters that name no colour, each 4 points
        # squared, under the verb's labels.
        ax.scatter(df.index, long, s=4, label="SAR (Long)")
        ax.scatter(df.index, short, s=4, label="SAR (Short)")
        ax.set_title("Parabolic SAR")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Composition fixtures (multi-subplot, suptitle, mathtext)
# ──────────────────────────────────────────────────────────────────────────────

def fig_comp_mathtext_two_subplots():
    """Two side-by-side subplots with a figure-level suptitle, mathtext labels, and mathtext legend.
    Regression guard for v1.1.3 — exercises legend mathtext, interior y-label gutter, and suptitle clearance."""
    t_ms = np.linspace(0, 50, 500)
    decay = np.exp(-t_ms * 0.08) * np.cos(t_ms * 0.4)
    noise = np.sin(t_ms * 1.3) * 0.3
    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE, dpi=DPI)
        fig.suptitle(r"$\alpha$ decay and $\beta$ noise — $\omega = 0.4$ rad/ms", fontsize=14, fontweight="bold")
        ax1 = fig.add_subplot(1, 2, 1)
        ax1.set_title(r"R$^{2}$ = 0.97")
        ax1.set_xlabel(r"$\Delta t$ (ms)")
        ax1.set_ylabel(r"$\sigma$ (normalised)")
        ax1.plot(t_ms, decay, color="#1f77b4", label=r"$\alpha$ decay")
        ax1.legend(loc="upper right")
        ax2 = fig.add_subplot(1, 2, 2)
        ax2.set_title(r"Noise — $\mu \pm 2\sigma$")
        ax2.set_xlabel(r"$\Delta t$ (ms)")
        ax2.set_ylabel(r"Amplitude")
        ax2.plot(t_ms, noise, color="#ff7f0e", label=r"$\beta$ noise")
        ax2.legend(loc="upper right")
        fig.tight_layout(rect=[0, 0, 1, 0.93])
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# v1.7.0 additions — broken-axis, symlog, geographic
# ──────────────────────────────────────────────────────────────────────────────

def fig_broken_y():
    """Y-axis break: data with low values 0..18 and high values 100..118, shown from 0 to 125 with 25..85 cut out.
    Two stacked subplots that share the same X but cover disjoint Y ranges, with diagonal break markers between them:
    matplotlib's broken-axis recipe (gallery subplots_axes_and_figures/broken_axis). The parts are stacked in the
    proportion of the spans they show (40 and 25), so one data unit is as long in both, as the library's break keeps
    one scale on either side; the legend stands at the upper left of the whole, the top part's.
    """
    x = np.arange(0, 20)
    y = np.where(x < 10, x * 2, x * 2 + 80)

    with plt.style.context(STYLE):
        fig, (ax_top, ax_bot) = plt.subplots(2, 1, sharex=True, figsize=FIGSIZE,
                                             gridspec_kw={"height_ratios": [40, 25]})
        ax_top.plot(x, y, label="Data")
        ax_bot.plot(x, y, label="Data")
        ax_top.set_ylim(85, 125)   # high range
        ax_bot.set_ylim(0, 25)     # low range
        ax_top.spines.bottom.set_visible(False)
        ax_bot.spines.top.set_visible(False)
        ax_top.tick_params(labeltop=False, bottom=False)
        ax_bot.xaxis.tick_bottom()
        ax_top.set_title("Broken Y-Axis")
        ax_bot.set_xlabel("x")
        ax_top.legend(loc="upper left")
        # Diagonal break markers
        d = .015
        kwargs = dict(transform=ax_top.transAxes, color='k', clip_on=False)
        ax_top.plot((-d, +d), (-d, +d), **kwargs)
        ax_top.plot((1 - d, 1 + d), (-d, +d), **kwargs)
        kwargs.update(transform=ax_bot.transAxes)
        ax_bot.plot((-d, +d), (1 - d, 1 + d), **kwargs)
        ax_bot.plot((1 - d, 1 + d), (1 - d, 1 + d), **kwargs)
    return fig


def fig_symlog():
    """Symmetric log Y-axis with linthresh=100 — for cubic data x ∈ [-50, 50]
    spanning ±125000. Validates that ticks are spaced (no overlap)."""
    x = np.arange(-50, 51, 1.0)
    y = x ** 3

    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(x, y, label="x³")
        ax.set_yscale("symlog", linthresh=100)
        ax.set_title("Symlog Y-Axis — x³")
        ax.set_xlabel("x")
        ax.set_ylabel("x³ (symlog, linthresh=100)")
        ax.legend(loc="upper left")
    return fig


def fig_geo_robinson():
    """World map in Robinson projection with land + ocean + coastlines + borders, drawn by cartopy (its Natural Earth 110m
    features, which cartopy downloads on first use)."""
    ccrs = require("cartopy.crs", "cartopy")
    cfeature = require("cartopy.feature", "cartopy")

    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(1, 1, 1, projection=ccrs.Robinson())
        ax.add_feature(cfeature.OCEAN, facecolor="#1a3a5c")
        ax.add_feature(cfeature.LAND,  facecolor="#2d5a27")
        ax.add_feature(cfeature.COASTLINE, edgecolor="white", linewidth=0.5)
        ax.add_feature(cfeature.BORDERS,   edgecolor="#888888", linewidth=0.2)
        ax.set_title("World Map — Robinson")
    return fig


def fig_ternary_scatter():
    """Ten compositions in a ternary triangle, drawn by mpltern: a corner label on each side, the grid and the ticks at
    fifths, and a title."""
    require("mpltern")  # side-effect: registers the 'ternary' projection
    from matplotlib.ticker import MultipleLocator

    top = np.array([0.80, 0.55, 0.30, 0.15, 0.10, 0.40, 0.25, 0.60, 0.35, 0.50])
    left = np.array([0.10, 0.20, 0.50, 0.30, 0.15, 0.20, 0.35, 0.10, 0.45, 0.25])
    right = 1.0 - top - left

    with plt.style.context(STYLE):
        fig = plt.figure(figsize=FIGSIZE)
        ax = fig.add_subplot(1, 1, 1, projection="ternary")
        ax.scatter(top, left, right)
        ax.set_tlabel("Top")
        ax.set_llabel("Left")
        ax.set_rlabel("Right")
        for axis in (ax.taxis, ax.laxis, ax.raxis):
            axis.set_major_locator(MultipleLocator(0.2))
        ax.grid(True)
        ax.set_title("ternary scatter")
    return fig


def fig_annotate_default():
    """Two annotations with no font of their own: matplotlib draws them at the size of the tick labels."""
    x = np.linspace(0, 10, 50)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(x, x)
        ax.annotate("peak", xy=(5, 5))
        ax.annotate("start", xy=(1, 1))
        ax.set_title("Annotation text size")
        ax.set_xlabel("x")
        ax.set_ylabel("y")
    return fig


# MatPlotLibNet stands a bar label 4 px beyond the bar's end; matplotlib's padding is in points.
BAR_LABEL_PADDING_PT = 4 * 72 / DPI


def _bar_label_figure(horizontal: bool, inverted: bool, title: str):
    """Bars 3, 5 and -2 with value labels at the bar's end; the value axis runs forwards or backwards."""
    categories = ["a", "b", "c"]
    values = [3.0, 5.0, -2.0]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        bars = ax.barh(categories, values) if horizontal else ax.bar(categories, values)
        ax.bar_label(bars, padding=BAR_LABEL_PADDING_PT)
        limits = (8, -4) if inverted else (-4, 8)
        if horizontal:
            ax.set_xlim(*limits)
        else:
            ax.set_ylim(*limits)
        ax.set_title(title)
    return fig


def fig_bar_label_signs():
    return _bar_label_figure(False, False, "Bar labels — positive and negative")


def fig_bar_label_inverted():
    return _bar_label_figure(False, True, "Bar labels — backwards Y axis")


def fig_bar_label_horizontal():
    return _bar_label_figure(True, False, "Bar labels — horizontal bars")


def fig_bar_label_horizontal_inverted():
    return _bar_label_figure(True, True, "Bar labels — horizontal bars, backwards axis")


# ──────────────────────────────────────────────────────────────────────────────
# Patches in data space, linked axes
# ──────────────────────────────────────────────────────────────────────────────

def _patch_shapes():
    """The patches both patch pictures draw: a rectangle, a polygon, an arrow, an arc, a circle, an ellipse and a wedge, all at
    positive coordinates so the log axis can place them. The numbers are the fidelity test's (PatchFidelityTests). Every colour is
    written out: the patch defaults of the two styles differ, and the pictures test the shapes, not the defaults."""
    black = "black"
    return [
        mpatches.Rectangle((1.2, 1), 1.0, 2.0, angle=20, facecolor="#1f77b4", edgecolor=black, label="rectangle"),
        mpatches.Polygon([(3, 1), (5, 1.5), (4.5, 4), (3.4, 3)], closed=True, facecolor="#ff7f0e", edgecolor=black),
        mpatches.FancyArrow(6, 1, 3, 2.5, width=0.35, facecolor="#2ca02c", edgecolor=black),
        mpatches.Arc((16, 2.5), 8, 2.4, theta1=0, theta2=270, edgecolor="#d62728", linewidth=2),
        mpatches.Circle((2, 7), 1.2, facecolor="#9467bd", edgecolor=black, alpha=0.6),
        mpatches.Ellipse((6, 7), 4, 2, angle=30, facecolor="#8c564b", edgecolor=black, label="ellipse"),
        mpatches.Wedge((11, 7), 2.0, 30, 300, facecolor="#e377c2", edgecolor=black, label="wedge"),
    ]


def fig_patches_linear():
    """The seven patches on a linear axes with set limits (the library rounds an automatic range to a step of its own, matplotlib to
    one that depends on the axes' size: a difference in every chart, left out so this one scores the drawing), with a legend listing
    the three that carry a label."""
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        for patch in _patch_shapes():
            ax.add_patch(patch)
        ax.set_xlim(0, 21)
        ax.set_ylim(0, 10)
        ax.set_title("Patches in data space")
        ax.legend(loc="upper right")
    return fig


def fig_patches_log():
    """The same seven patches and a ring cut from a wedge on a log X axis with fixed limits: each vertex, a curve's control
    points included, moves through the scale on its own, so the curves bend."""
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        for patch in _patch_shapes():
            ax.add_patch(patch)
        ax.add_patch(mpatches.Wedge((17, 7), 2.5, 40, 320, width=0.8, facecolor="#17becf", edgecolor="black"))
        ax.set_xscale("log")
        ax.set_xlim(0.4, 25)
        ax.set_ylim(0, 10)
        ax.set_title("Patches on a log axis")
    return fig


def fig_linked_yaxis_fahrenheit():
    """secondary_yaxis on the right: Fahrenheit beside a plot in Celsius. The limits are set, so the picture scores the linked axis
    and not the automatic range."""
    hours = np.array([0, 3, 6, 9, 12, 15, 18, 21, 24], dtype=float)
    celsius = np.array([8.0, 6.5, 7.5, 13.0, 19.5, 22.0, 18.0, 12.5, 9.0])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(hours, celsius)
        ax.set_xlim(0, 24)
        ax.set_ylim(5, 25)
        ax.set_title("Celsius and Fahrenheit")
        ax.set_xlabel("hour")
        ax.set_ylabel("temperature (°C)")
        right = ax.secondary_yaxis("right", functions=(lambda c: c * 1.8 + 32, lambda f: (f - 32) / 1.8))
        right.set_ylabel("temperature (°F)")
    return fig


def fig_linked_xaxis_period():
    """secondary_xaxis on the top of a plot over frequency on a log scale: the period, the reciprocal. No title: matplotlib stands
    a title where the top axes' own labels already are. The limits are set, so the picture scores the linked axis and not the
    automatic range."""
    frequency = np.array([1, 2, 5, 10, 20, 50, 100, 200, 500, 1000], dtype=float)
    power = np.array([0.8, 1.6, 3.0, 4.2, 3.1, 1.9, 1.1, 0.6, 0.25, 0.1])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(frequency, power)
        ax.set_xscale("log")
        ax.set_xlim(1, 1000)
        ax.set_ylim(0, 5)
        ax.set_xlabel("frequency (Hz)")
        ax.set_ylabel("power")
        top = ax.secondary_xaxis("top", functions=(lambda f: 1.0 / f, lambda p: 1.0 / p))
        top.set_xlabel("period (s)")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Registry
# ──────────────────────────────────────────────────────────────────────────────

CHARTS: dict[str, callable] = {
    # Core 12 (Phase 3)
    "line":          fig_line_legend,
    "line_grid":     fig_line_grid,
    "scatter":       fig_scatter_markers,
    "bar":           fig_bar_grouped,
    "hist":          fig_hist_normal,
    "pie":           fig_pie_autopct,
    "box":           fig_box_basic,
    "violin":        fig_violin_basic,
    "heatmap":       fig_imshow_viridis,
    "contour":       fig_contour_peaks,
    "polar":         fig_polar_rose,
    "candlestick":   fig_candlestick,
    "errorbar":      fig_errorbar,
    # Phase 5 — XY (10)
    "area":          fig_area,
    "stacked_area":  fig_stacked_area,
    "step":          fig_step,
    "bubble":        fig_bubble,
    "regression":    fig_regression,
    "residual":      fig_residual,
    "ecdf":          fig_ecdf,
    "signal":        fig_signal,
    "signalxy":      fig_signalxy,
    "sparkline":     fig_sparkline,
    # Phase 5 — Grid (8)
    "contour_lines": fig_contour_lines,
    "hexbin":        fig_hexbin,
    "hist2d":        fig_hist2d,
    "pcolormesh":    fig_pcolormesh,
    "image":         fig_image,
    "image_style":   fig_image_style,
    "spectrogram":   fig_spectrogram,
    "tricontour":    fig_tricontour,
    "tripcolor":     fig_tripcolor,
    # Phase 5 — Field (4)
    "quiver":        fig_quiver,
    "streamplot":    fig_streamplot,
    "barbs":         fig_barbs,
    "stem":          fig_stem,
    # Phase 5 — Polar (3)
    "polar_scatter": fig_polar_scatter,
    "polar_bar":     fig_polar_bar,
    "polar_heatmap": fig_polar_heatmap,
    # Phase 5 — Categorical (4)
    "broken_bar":    fig_broken_bar,
    "eventplot":     fig_eventplot,
    "gantt":         fig_gantt,
    "waterfall":     fig_waterfall,
    "fill_betweenx": fig_fill_betweenx,
    "matshow":       fig_matshow,
    "set_bounds":    fig_set_bounds,
    "ternary_scatter": fig_ternary_scatter,
    # Phase 5 — Distribution (6, seaborn)
    "kde":           fig_kde,
    "rugplot":       fig_rugplot,
    "stripplot":     fig_stripplot,
    "swarmplot":     fig_swarmplot,
    "pointplot":     fig_pointplot,
    "countplot":     fig_countplot,
    # Phase 5 — 3D (5)
    "scatter3d":     fig_scatter3d,
    "bar3d":         fig_bar3d,
    "surface":       fig_surface,
    "wireframe":     fig_wireframe,
    "stem3d":        fig_stem3d,
    # Phase 5 — Financial (1)
    "ohlc_bar":      fig_ohlc_bar,
    # Phase 5 — Special (4)
    "sankey":        fig_sankey,
    "table":         fig_table,
    "treemap":       fig_treemap,
    "radar":         fig_radar,
    # Phase 6 — Financial indicators (15, pandas_ta references)
    "ind_sma":       fig_ind_sma,
    "ind_ema":       fig_ind_ema,
    "ind_bbands":    fig_ind_bbands,
    "ind_macd":      fig_ind_macd,
    "ind_rsi":       fig_ind_rsi,
    "ind_stoch":     fig_ind_stoch,
    "ind_atr":       fig_ind_atr,
    "ind_adx":       fig_ind_adx,
    "ind_cci":       fig_ind_cci,
    "ind_williamsr": fig_ind_williamsr,
    "ind_obv":       fig_ind_obv,
    "ind_vwap":      fig_ind_vwap,
    "ind_keltner":   fig_ind_keltner,
    "ind_ichimoku":  fig_ind_ichimoku,
    "ind_psar":      fig_ind_psar,
    # v1.1.3 — Composition (multi-subplot suptitle + mathtext)
    "comp_mathtext_two_subplots": fig_comp_mathtext_two_subplots,
    # v1.7.0 — Axis breaks, symlog, geographic
    "broken_y":      fig_broken_y,
    "symlog":        fig_symlog,
    "geo_robinson":  fig_geo_robinson,
    # Text placement — annotation size, bar label side
    "annotate_default":              fig_annotate_default,
    "bar_label_signs":               fig_bar_label_signs,
    "bar_label_inverted":            fig_bar_label_inverted,
    "bar_label_horizontal":          fig_bar_label_horizontal,
    "bar_label_horizontal_inverted": fig_bar_label_horizontal_inverted,
    # Patches in data space, linked axes (matplotlib 3.11.2, see requirements.txt)
    "patches_linear":                fig_patches_linear,
    "patches_log":                   fig_patches_log,
    "linked_yaxis_fahrenheit":       fig_linked_yaxis_fahrenheit,
    "linked_xaxis_period":           fig_linked_xaxis_period,
}


# ──────────────────────────────────────────────────────────────────────────────
# CLI
# ──────────────────────────────────────────────────────────────────────────────

def main():
    global STYLE, FIXTURES_ROOT
    parser = argparse.ArgumentParser(description="Generate matplotlib reference PNGs for MatPlotLibNet fidelity tests")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--all", action="store_true", help="Generate all fixtures")
    group.add_argument("--chart", nargs="+", metavar="NAME",
                       choices=list(CHARTS.keys()),
                       help=f"Generate specific chart(s). Choices: {', '.join(CHARTS)}")
    parser.add_argument("--style", choices=["classic", "v2", "both"], default="both",
                        help="matplotlib style: 'classic' (pre-2.0), 'v2' (modern default), or 'both'")
    parser.add_argument("--out", metavar="DIR", type=Path, default=None,
                        help="write under DIR (classic/, v2/ and data/ beneath it) instead of the fidelity fixtures, "
                             "to compare a run with what is checked in")
    args = parser.parse_args()
    if args.out is not None:
        FIXTURES_ROOT = args.out

    charts = list(CHARTS.keys()) if args.all else args.chart
    styles = ["classic", "default"] if args.style == "both" else \
             (["classic"] if args.style == "classic" else ["default"])

    for style in styles:
        STYLE = style
        print(f"matplotlib {matplotlib.__version__} [{STYLE_DIR[style]}], generating {len(charts)} fixture(s) -> {out_dir()}")
        for name in charts:
            try:
                _PLOTTED.clear()
                fig = CHARTS[name]()
                save(fig, name)
                save_plotted(name)
            except Exception as exc:
                print(f"  ERROR generating {name} [{style}]: {exc}", file=sys.stderr)
                sys.exit(1)
    print("Done.")


if __name__ == "__main__":
    main()
