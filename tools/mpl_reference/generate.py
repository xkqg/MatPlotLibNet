"""
MatPlotLibNet matplotlib reference image generator.

Generates fixture PNGs for the fidelity test suite using pinned matplotlib 3.9.x.
Each fixture uses:
  - fixed RNG seed 42
  - figsize (8, 6) inches at DPI 100 → 800×600 px output
  - style: 'classic' (matplotlib pre-2.0 look matching Theme.MatplotlibClassic)

Usage:
    pip install -r requirements.txt
    python generate.py --all
    python generate.py --chart line
    python generate.py --chart line scatter bar

Output goes to: ../../Tst/MatPlotLibNet.Fidelity/Fixtures/Matplotlib/*.png
Meta JSON written alongside each PNG.
"""

import argparse
import json
import os
import sys
from pathlib import Path

import numpy as np
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.dates import date2num
from matplotlib.sankey import Sankey
from datetime import datetime, timedelta

# Optional deps — used only by a subset of fixtures. Each import is guarded so
# the generator still runs for the subset the developer cares about.
try:
    import seaborn as sns
except ImportError:
    sns = None

try:
    import squarify
except ImportError:
    squarify = None

try:
    import pandas as pd
    import pandas_ta as pta
except ImportError:
    pd = None
    pta = None

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


def out_dir() -> Path:
    d = FIXTURES_ROOT / STYLE_DIR[STYLE]
    d.mkdir(parents=True, exist_ok=True)
    return d


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
    od = out_dir()
    path = od / f"{name}.png"
    fig.savefig(path, dpi=DPI)
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
    rng = np.random.default_rng(SEED)
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


def fig_scatter_markers():
    rng = np.random.default_rng(SEED)
    n = 80
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(rng.normal(0, 1, n), rng.normal(0, 1, n), marker="o", label="circles")
        ax.scatter(rng.normal(2, 1, n), rng.normal(2, 1, n), marker="s", label="squares")
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
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        im = ax.imshow(data, cmap="viridis", aspect="auto")
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
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(x, y, s=sizes, alpha=0.5)
        ax.set_title("Bubble chart")
    return fig


def fig_regression():
    rng = np.random.default_rng(SEED)
    x = np.linspace(0, 10, 30)
    y = 2 * x + 1 + rng.normal(0, 1.5, 30)
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
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.scatter(x, residuals)
        ax.axhline(0, color="black", linewidth=0.8)
        ax.set_title("Residual plot")
        ax.set_xlabel("x")
        ax.set_ylabel("residual")
    return fig


def fig_ecdf():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 100)
    sorted_data = np.sort(data)
    y = np.arange(1, len(sorted_data) + 1) / len(sorted_data)
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
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(y)
        ax.set_title("Sparkline")
        for spine in ("top", "right"):
            ax.spines[spine].set_visible(False)
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
    rng = np.random.default_rng(SEED)
    data = rng.uniform(0, 1, (30, 40, 3))
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.imshow(data, aspect="auto")
        ax.set_title("RGB image")
    return fig


def fig_spectrogram():
    rng = np.random.default_rng(SEED)
    fs = 1000.0
    t = np.arange(0, 2, 1 / fs)
    signal = np.sin(2 * np.pi * 50 * t) + 0.5 * rng.normal(size=t.shape)
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
    labels = ["Start", "A", "B", "C", "D", "End"]
    values = [100, 30, -20, 40, -10]
    cumulative = np.concatenate([[0], np.cumsum(values)])
    totals = [100, *(100 + np.cumsum(values))]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        # Start bar
        ax.bar(0, totals[0], color="C0")
        # Delta bars
        for i, v in enumerate(values):
            color = "C2" if v >= 0 else "C3"
            ax.bar(i + 1, v, bottom=totals[i], color=color)
        # End bar
        ax.bar(len(labels) - 1, totals[-1], color="C0")
        ax.set_xticks(range(len(labels)))
        ax.set_xticklabels(labels)
        ax.set_title("Waterfall chart")
    return fig


# ──────────────────────────────────────────────────────────────────────────────
# Phase 5 — Distribution family (6, seaborn)
# ──────────────────────────────────────────────────────────────────────────────

def fig_kde():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 500)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.kdeplot(data, ax=ax)
        else:
            ax.hist(data, bins=30, density=True)
        ax.set_title("KDE — 500 normal samples")
    return fig


def fig_rugplot():
    rng = np.random.default_rng(SEED)
    data = rng.normal(0, 1, 100)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.rugplot(data, ax=ax, height=0.3)
        else:
            ax.scatter(data, np.zeros_like(data), marker="|", s=200)
        ax.set_ylim(0, 1)
        ax.set_title("Rugplot")
    return fig


def fig_stripplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 0.5, 30) for loc in [0, 1, 2]]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.stripplot(data=data, ax=ax)
        else:
            for i, d in enumerate(data):
                ax.scatter(np.full_like(d, i), d)
        ax.set_title("Stripplot — 3 groups")
    return fig


def fig_swarmplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 0.5, 30) for loc in [0, 1, 2]]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.swarmplot(data=data, ax=ax)
        else:
            for i, d in enumerate(data):
                ax.scatter(np.full_like(d, i), d)
        ax.set_title("Swarmplot — 3 groups")
    return fig


def fig_pointplot():
    rng = np.random.default_rng(SEED)
    data = [rng.normal(loc, 1.0, 50) for loc in [0, 1, 2, 3]]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.pointplot(data=data, ax=ax)
        else:
            means = [np.mean(d) for d in data]
            stds = [np.std(d) for d in data]
            ax.errorbar(range(len(means)), means, yerr=stds, fmt="o-")
        ax.set_title("Pointplot — 4 groups")
    return fig


def fig_countplot():
    rng = np.random.default_rng(SEED)
    categories = rng.choice(["A", "B", "C", "D"], 200, p=[0.4, 0.3, 0.2, 0.1])
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if sns is not None:
            sns.countplot(x=categories, ax=ax)
        else:
            unique, counts = np.unique(categories, return_counts=True)
            ax.bar(unique, counts)
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
        ax.axis("tight")
        ax.axis("off")
        ax.table(cellText=cell_text, rowLabels=rows, colLabels=columns, loc="center")
        ax.set_title("Loss table")
    return fig


def fig_treemap():
    values = [500, 300, 200, 150, 100, 80, 60]
    labels = ["A", "B", "C", "D", "E", "F", "G"]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        if squarify is not None:
            squarify.plot(sizes=values, label=labels, ax=ax, alpha=0.8)
        else:
            # Fallback: simple horizontal bars
            ax.barh(labels, values)
        ax.axis("off")
        ax.set_title("Treemap")
    return fig


def fig_radar():
    categories = ["Speed", "Power", "Range", "Accuracy", "Cost"]
    values = [0.8, 0.6, 0.7, 0.9, 0.5]
    n = len(categories)
    angles = np.linspace(0, 2 * np.pi, n, endpoint=False).tolist()
    values_closed = values + values[:1]
    angles_closed = angles + angles[:1]
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE, subplot_kw={"projection": "polar"})
        ax.plot(angles_closed, values_closed, linewidth=2)
        ax.fill(angles_closed, values_closed, alpha=0.25)
        ax.set_xticks(angles)
        ax.set_xticklabels(categories)
        ax.set_title("Radar — 5 axes")
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
    df = pd.DataFrame({"open": open_, "high": high, "low": low, "close": close, "volume": volume})
    return df


def fig_ind_sma():
    df = _synthetic_ohlc()
    sma = pta.sma(df["close"], length=20)
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
    ema = pta.ema(df["close"], length=20)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, ema, label="EMA(20)")
        ax.set_title("EMA(20) overlay")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_bbands():
    df = _synthetic_ohlc()
    bb = pta.bbands(df["close"], length=20, std=2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, bb.iloc[:, 0], label="lower")
        ax.plot(df.index, bb.iloc[:, 1], label="middle")
        ax.plot(df.index, bb.iloc[:, 2], label="upper")
        ax.set_title("Bollinger Bands(20, 2)")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_macd():
    df = _synthetic_ohlc()
    macd = pta.macd(df["close"], fast=12, slow=26, signal=9)
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
    rsi = pta.rsi(df["close"], length=14)
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
    stoch = pta.stoch(df["high"], df["low"], df["close"], k=14, d=3)
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
    atr = pta.atr(df["high"], df["low"], df["close"], length=14)
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
    adx = pta.adx(df["high"], df["low"], df["close"], length=14)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, adx.iloc[:, 0], label="ADX")
        ax.plot(df.index, adx.iloc[:, 1], label="+DI")
        ax.plot(df.index, adx.iloc[:, 2], label="-DI")
        ax.set_title("ADX(14)")
        ax.set_xlabel("bar")
        ax.legend()
    return fig


def fig_ind_cci():
    df = _synthetic_ohlc()
    cci = pta.cci(df["high"], df["low"], df["close"], length=20)
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
    wr = pta.willr(df["high"], df["low"], df["close"], length=14)
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
    obv = pta.obv(df["close"], df["volume"])
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
    kc = pta.kc(df["high"], df["low"], df["close"], length=20, scalar=2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close")
        ax.plot(df.index, kc.iloc[:, 0], label="lower")
        ax.plot(df.index, kc.iloc[:, 1], label="middle")
        ax.plot(df.index, kc.iloc[:, 2], label="upper")
        ax.set_title("Keltner Channels(20, 2)")
        ax.set_xlabel("bar")
        ax.set_ylabel("price")
        ax.legend()
    return fig


def fig_ind_ichimoku():
    df = _synthetic_ohlc(n=80)  # ichimoku needs more history
    ich, _ = pta.ichimoku(df["high"], df["low"], df["close"])
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


def fig_ind_psar():
    df = _synthetic_ohlc()
    psar = pta.psar(df["high"], df["low"], df["close"], af=0.02, max_af=0.2)
    with plt.style.context(STYLE):
        fig, ax = plt.subplots(figsize=FIGSIZE)
        ax.plot(df.index, df["close"], label="close", linewidth=0.8)
        # psar returns 4 cols: long, short, af, reversal — plot long+short scatter
        ax.scatter(df.index, psar.iloc[:, 0], s=10, label="PSAR long")
        ax.scatter(df.index, psar.iloc[:, 1], s=10, label="PSAR short")
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
# Registry
# ──────────────────────────────────────────────────────────────────────────────

CHARTS: dict[str, callable] = {
    # Core 12 (Phase 3)
    "line":          fig_line_legend,
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
}


# ──────────────────────────────────────────────────────────────────────────────
# CLI
# ──────────────────────────────────────────────────────────────────────────────

def main():
    global STYLE
    parser = argparse.ArgumentParser(description="Generate matplotlib reference PNGs for MatPlotLibNet fidelity tests")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--all", action="store_true", help="Generate all fixtures")
    group.add_argument("--chart", nargs="+", metavar="NAME",
                       choices=list(CHARTS.keys()),
                       help=f"Generate specific chart(s). Choices: {', '.join(CHARTS)}")
    parser.add_argument("--style", choices=["classic", "v2", "both"], default="both",
                        help="matplotlib style: 'classic' (pre-2.0), 'v2' (modern default), or 'both'")
    args = parser.parse_args()

    charts = list(CHARTS.keys()) if args.all else args.chart
    styles = ["classic", "default"] if args.style == "both" else \
             (["classic"] if args.style == "classic" else ["default"])

    for style in styles:
        STYLE = style
        print(f"matplotlib {matplotlib.__version__} [{STYLE_DIR[style]}], generating {len(charts)} fixture(s) -> {out_dir()}")
        for name in charts:
            try:
                fig = CHARTS[name]()
                save(fig, name)
            except Exception as exc:
                print(f"  ERROR generating {name} [{style}]: {exc}", file=sys.stderr)
                sys.exit(1)
    print("Done.")


if __name__ == "__main__":
    main()
