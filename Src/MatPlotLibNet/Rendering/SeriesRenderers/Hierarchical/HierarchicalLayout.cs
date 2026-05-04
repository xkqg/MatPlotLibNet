// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Layout constants shared by every renderer in the
/// <see cref="MatPlotLibNet.Models.Series.HierarchicalSeries"/> family
/// (Treemap, Sunburst, Dendrogram, …). Centralising the magic-numbered offsets,
/// padding strips, and stroke thicknesses ensures a 4th renderer added later (e.g.
/// <c>ClustermapSeries</c>) discovers the existing visual contract instead of
/// inventing its own constant under a different name.</summary>
/// <remarks>Each nested type owns the constants for ONE renderer; constants are
/// renderer-specific by design — sunburst's ring inset is not a treemap's header
/// height. The grouping under one file is for discoverability, not for forced
/// sharing.</remarks>
internal static class HierarchicalLayout
{
    /// <summary>Minimum pixel width or height for a renderer sub-panel below which
    /// the renderer suppresses the panel to avoid sub-pixel noise. Shared by
    /// every composite renderer that subdivides plot bounds — <c>ClustermapSeriesRenderer</c>
    /// (dendrogram margin panels) and <c>PairGridSeriesRenderer</c> (per-cell sub-panels).</summary>
    public const double MinPanelPx = 4.0;

    /// <summary>Constants used by <c>DendrogramSeriesRenderer</c>.</summary>
    public static class Dendrogram
    {
        /// <summary>Pixel gap between the leaf-axis baseline and the leaf label.</summary>
        public const double LabelOffsetPx = 6.0;

        /// <summary>Stroke thickness of dendrogram U-shape segments.</summary>
        public const double LineThickness = 1.25;

        /// <summary>Stroke thickness of the dashed cut-height reference line.</summary>
        public const double CutLineThickness = 1.0;
    }

    /// <summary>Constants used by <c>TreemapSeriesRenderer</c>.</summary>
    public static class Treemap
    {
        /// <summary>Pixel height reserved at the top of an interior rectangle for its label.</summary>
        public const double HeaderHeightPx = 18.0;

        /// <summary>Pixel padding between sibling rectangles inside an interior frame.</summary>
        public const double SidePaddingPx = 2.0;
    }

    /// <summary>Constants used by <c>SunburstSeriesRenderer</c>.</summary>
    public static class Sunburst
    {
        /// <summary>Pixel inset subtracted from the plot's half-extent before computing
        /// the outermost ring radius — leaves visible clearance for outer labels.</summary>
        public const double OuterRingInsetPx = 10.0;
    }

    /// <summary>Constants used by <c>ClustermapSeriesRenderer</c>.</summary>
    public static class Clustermap
    {
        /// <summary>Minimum pixel width/height for a dendrogram panel below which the
        /// panel is suppressed to avoid sub-pixel noise. Aliases the shared
        /// <see cref="HierarchicalLayout.MinPanelPx"/> so a single source-of-truth
        /// applies across every composite renderer.</summary>
        public const double MinPanelPx = HierarchicalLayout.MinPanelPx;
    }
}
