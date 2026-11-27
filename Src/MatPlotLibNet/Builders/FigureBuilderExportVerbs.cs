// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>How the figure leaves the builder: built and written as SVG, as JSON or to a file, and whether its SVG resizes with the
/// page that holds it.</summary>
public static class FigureBuilderExportVerbs
{
    extension(FigureBuilder figure)
    {
        /// <summary>Controls whether the emitted SVG's root element carries an inline
        /// <c>style="max-width:100%;height:auto"</c> declaration so the chart resizes
        /// fluidly with its container. Default is <see langword="true"/>; set to
        /// <see langword="false"/> to leave the declaration out, for the same bytes the library wrote
        /// before it carried one (e.g. pixel-diff fixtures that pin the SVG string).</summary>
        public FigureBuilder WithResponsiveSvg(bool enabled = true) => figure.OnFigure(f => f.ResponsiveSvg = enabled);

        /// <summary>Builds the figure and renders it as an SVG string.</summary>
        public string ToSvg() => figure.Build().ToSvg();

        /// <summary>Builds the figure and serializes it to JSON.</summary>
        public string ToJson(bool indented = false) => figure.Build().ToJson(indented);

        /// <summary>Builds the figure and saves it to a file. Format auto-detected from extension.</summary>
        public void Save(string path) => figure.Build().Save(path);
    }
}
