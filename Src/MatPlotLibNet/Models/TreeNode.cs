// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a node in a hierarchical tree structure, used by Treemap and Sunburst series.</summary>
public sealed record TreeNode
{
    public string Label { get; init; } = "";

    public double Value { get; init; }

    public Color? Color { get; init; }

    /// <summary>A SECOND value the node's colour is read from, through the series' normalizer and colour map —
    /// so a rect can carry two variables: its area from <see cref="Value"/> and its colour from this (the
    /// host-map encoding: area = size, colour = load). Null keeps the sibling-index ramp; an explicit
    /// <see cref="Color"/> always wins.</summary>
    public double? ColorValue { get; init; }

    /// <summary>The node's MEASURE, drawn big: under the label in a leaf cell, at the right end of the header
    /// strip in an interior one. A cell that reports a number reads like a stat tile — the name small on top,
    /// the number underneath in a size you can see from across a room (Grafana's Stat panel puts the value
    /// under the name for exactly this reason); one line carrying both at one size is a line you must read.
    /// Null draws nothing, and a headline that does not fit its cell is dropped before the name is.</summary>
    public string? Headline { get; init; }

    /// <summary>Where the node LEADS, or null. The rect, its label and everything nested inside it are wrapped
    /// in an SVG <c>&lt;a href&gt;</c>, so drilling into a subtree is a URL and needs no script.
    /// <para>The library also ships a self-contained click script (<c>FigureBuilder.WithTreemapDrilldown</c>),
    /// and it works in a saved SVG — but a page that injects the SVG as MARKUP never runs it: a
    /// <c>&lt;script&gt;</c> inserted through <c>innerHTML</c> does not execute (HTML spec). A link works
    /// everywhere, and the URL is state a server-rendered page can hold and a reader can paste.</para></summary>
    public string? Url { get; init; }

    /// <summary>Whether the subtree this node leads to is currently shown (<c>aria-expanded</c>); null for a
    /// link that discloses nothing.</summary>
    public bool? Expanded { get; init; }

    /// <summary>A fill pattern over the node's rect, or <see cref="HatchPattern.None"/>. This is how a rect says
    /// <i>no information</i> — the source went silent — a pattern and not a colour, exactly as a stat tile does:
    /// "I can no longer see you" is a different fault from "you are hot", and a wall that paints them the same
    /// lies when it matters most.</summary>
    public HatchPattern Hatch { get; init; } = HatchPattern.None;

    /// <summary>The hatch strokes' colour, or null to contrast automatically.</summary>
    public Color? HatchColor { get; init; }

    public IReadOnlyList<TreeNode> Children { get; init; } = Array.Empty<TreeNode>();

    /// <summary>Computes the total value: own value if leaf, sum of children if branch.</summary>
    public double TotalValue => Children.Count > 0
        ? Children.Sum(c => c.TotalValue)
        : Value;
}
