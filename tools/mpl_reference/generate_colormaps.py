"""
MatPlotLibNet matplotlib colour map table generator.

Writes the two files that hold matplotlib's colour maps for the library:

  Src/MatPlotLibNet/Styling/ColorMaps/MatplotlibColorTables.Data.cs
      the data of every map the library ships under a name matplotlib has, in the form matplotlib holds it:
        - a map matplotlib lists (ListedColormap: viridis, plasma, inferno, magma, cividis, turbo, twilight, ...) as its
          entries, each channel as the renderers write it (colormaps[name](i, bytes=True), a byte truncated);
        - a map matplotlib makes from segment data (LinearSegmentedColormap: the ColorBrewer maps, coolwarm, ...) whose segments
          meet at equal distances as the colours where they meet, which a linear blend between them reproduces;
        - any other map matplotlib makes from segment data (jet, hot, bone, ...) or from functions (afmhot, ocean, rainbow,
          cubehelix) as the 256 entries of its table.

  Tst/MatPlotLibNet/Styling/ColorMaps/matplotlib-colormaps.json
      what matplotlib's own lookup answers at a set of values of every such map, for the test that holds the library's maps
      to it. It is read from the lookup (cmap(v, bytes=True)), not from the data above, so a mistake in the data is seen.

Usage (from anywhere; needs matplotlib 3.11.2 and numpy):
    python generate_colormaps.py

The library's name of a map is matplotlib's name in lower case; the C# files write the library's.
"""

import json
import sys
from pathlib import Path

import numpy as np
import matplotlib

matplotlib.use("Agg")
from matplotlib import colormaps  # noqa: E402

ROOT = Path(__file__).parent.parent.parent
DATA_CS = ROOT / "Src" / "MatPlotLibNet" / "Styling" / "ColorMaps" / "MatplotlibColorTables.Data.cs"
REFERENCE_JSON = ROOT / "Tst" / "MatPlotLibNet" / "Styling" / "ColorMaps" / "matplotlib-colormaps.json"

# The maps the library ships that matplotlib has, by matplotlib's name. The qualitative maps (tab10, Set1, ...) are listed
# colours the library holds as ListedColorMap and are only checked, not generated.
SHIPPED = [
    "viridis", "plasma", "inferno", "magma", "coolwarm", "Blues", "Reds", "turbo", "jet",
    "cividis", "Greens", "Oranges", "Purples", "Greys", "YlOrBr", "YlOrRd", "OrRd", "PuBu", "YlGn", "BuGn", "hot", "copper",
    "bone", "BuPu", "GnBu", "PuRd", "RdPu", "YlGnBu", "PuBuGn", "cubehelix",
    "RdBu", "RdYlGn", "RdYlBu", "BrBG", "PiYG", "Spectral", "PuOr", "seismic", "bwr",
    "twilight", "twilight_shifted", "hsv",
    "gray", "spring", "summer", "autumn", "winter", "cool", "afmhot", "PRGn", "RdGy", "rainbow", "ocean", "terrain", "CMRmap",
]
# okabe_ito is not one of them: matplotlib lists black first, and the library lists it last (the order of the colour-blind-safe theme's cycle).
QUALITATIVE = ["tab10", "tab20", "Set1", "Set2", "Set3", "Pastel1", "Pastel2", "Dark2", "Accent", "Paired"]

# The values at which the test asks matplotlib: every fifth entry of the table, and the last.
def sample_indices(n):
    return sorted(set(range(0, n, 5)) | {n - 1})


def entries(cm):
    """The table of the map as bytes, one row of (r, g, b) per entry: matplotlib's own lookup of every entry."""
    return colormaps[cm.name](np.arange(cm.N), bytes=True)[:, :3].astype(int)


def segment_stops(cm):
    """The positions where the segments of a LinearSegmentedColormap meet and the byte colour at each, or None when a channel
    is a function or a segment jumps (a value at a position that differs on its two sides)."""
    data = cm._segmentdata
    if any(callable(data[c]) for c in ("red", "green", "blue")):
        return None
    xs = set()
    for channel in ("red", "green", "blue"):
        rows = np.asarray(data[channel], dtype=float)
        if not np.allclose(rows[:, 1], rows[:, 2]):
            return None
        xs.update(rows[:, 0].tolist())
    positions = np.array(sorted(xs))
    rgb = np.empty((len(positions), 3), dtype=int)
    for k, channel in enumerate(("red", "green", "blue")):
        rows = np.asarray(data[channel], dtype=float)
        rgb[:, k] = np.rint(np.interp(positions, rows[:, 0], rows[:, 1]) * 255).astype(int)
    return positions, rgb


def blend_at(positions, rgb, v):
    """What the library does between stops: a linear blend of the bytes, rounded."""
    return [int(np.floor(np.interp(v, positions, rgb[:, k]) + 0.5)) for k in range(3)]


def worst_level(cm, positions, rgb):
    table = entries(cm)
    worst = 0
    for i in range(cm.N):
        got = blend_at(positions, rgb, i / (cm.N - 1))
        worst = max(worst, max(abs(got[k] - table[i][k]) for k in range(3)))
    return worst


def evenly(positions):
    return np.allclose(positions, np.linspace(0, 1, len(positions)), atol=1e-12)


def hex_rows(rgb):
    return [f"0x{int(r):02X}{int(g):02X}{int(b):02X}" for r, g, b in rgb]


def array_lines(values, per_line=8, indent="        "):
    lines = []
    for i in range(0, len(values), per_line):
        lines.append(indent + ", ".join(values[i:i + per_line]) + ",")
    return "\n".join(lines)


def pascal(name):
    return "".join(part[:1].upper() + part[1:] for part in name.lower().split("_"))


def main():
    tables = []   # (library name, kind, positions or None, rgb)
    for name in SHIPPED:
        cm = colormaps[name]
        library = name.lower()
        if type(cm).__name__ == "ListedColormap":
            tables.append((library, "listed", None, entries(cm)))
            continue
        stops = segment_stops(cm)
        if stops is not None:
            positions, rgb = stops
            worst = worst_level(cm, positions, rgb)
            # The library finds the colour of a value in O(1) between colours at equal distances; a lookup between colours at
            # unequal distances is a search (twice the time of a lookup, measured), so a map whose segments meet unevenly is
            # written as its table, which is what matplotlib's own lookup reads.
            if worst <= 1 and evenly(positions):
                tables.append((library, "stops", None, rgb))
                print(f"{library:18s} {len(positions):3d} stops   worst level {worst}", file=sys.stderr)
                continue
            print(f"{library:18s} {len(positions):3d} unevenly spaced or {worst} levels off: written as its table", file=sys.stderr)
        tables.append((library, "table", None, entries(cm)))
        print(f"{library:18s} table of {cm.N}", file=sys.stderr)

    out = []
    out.append("// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.")
    out.append("// Licensed under the MIT License. See LICENSE file in the project root for full license information.")
    out.append("")
    out.append("// Written by tools/mpl_reference/generate_colormaps.py from matplotlib 3.11.2: change the tables by running it again.")
    out.append("")
    out.append("namespace MatPlotLibNet.Styling.ColorMaps;")
    out.append("")
    out.append("internal static partial class MatplotlibColorTables")
    out.append("{")
    out.append("    /// <summary>The library's colour map of this name, made of matplotlib's table for it; <see langword=\"null\"/> for a name")
    out.append("    /// matplotlib has no table for.</summary>")
    out.append("    private static LinearColorMap? MakeFromTable(string name) => name switch")
    out.append("    {")
    for library, kind, positions, rgb in tables:
        prop = pascal(library)
        out.append(f"        \"{library}\" => Evenly(name, {prop}),")
    out.append("        _ => null,")
    out.append("    };")
    for library, kind, positions, rgb in tables:
        prop = pascal(library)
        out.append("")
        if kind == "listed":
            out.append(f"    /// <summary>matplotlib's <c>{library}</c>: the {len(rgb)} colours of its listed colour map, as bytes.</summary>")
        elif kind == "table":
            out.append(f"    /// <summary>matplotlib's <c>{library}</c>: the {len(rgb)} entries of its table, as bytes.</summary>")
        else:
            out.append(f"    /// <summary>matplotlib's <c>{library}</c>: the colour where each of its {len(rgb)} segments meets.</summary>")
        out.append(f"    private static ReadOnlySpan<uint> {prop} =>")
        out.append("    [")
        out.append(array_lines(hex_rows(rgb)))
        out.append("    ];")
    out.append("}")
    out.append("")
    DATA_CS.write_text("\n".join(out), encoding="utf-8", newline="\n")

    reference = {}
    for name in SHIPPED + QUALITATIVE:
        cm = colormaps[name]
        n = cm.N
        rows = []
        for i in sample_indices(n):
            v = i / (n - 1)
            r, g, b, _ = colormaps[name](v, bytes=True)
            rows.append([v, f"{int(r):02X}{int(g):02X}{int(b):02X}"])
        reference[name.lower()] = {"entries": n, "samples": rows}
    lines = [f"{json.dumps(name)}:{json.dumps(entry, separators=(',', ':'))}" for name, entry in reference.items()]
    REFERENCE_JSON.write_text("{" + chr(10) + ("," + chr(10)).join(lines) + chr(10) + "}" + chr(10), encoding="utf-8", newline=chr(10))
    print(f"wrote {DATA_CS} and {REFERENCE_JSON}", file=sys.stderr)


if __name__ == "__main__":
    main()
