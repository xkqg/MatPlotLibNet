// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Well-known string keys for <see cref="RcParams"/> entries.</summary>
public static class RcParamKeys
{
    // Font
    /// <summary>rc parameter key for the default font family (e.g. "sans-serif").</summary>
    public const string FontFamily        = "font.family";
    /// <summary>rc parameter key for the default font size in points.</summary>
    public const string FontSize          = "font.size";
    /// <summary>rc parameter key for the default font weight (e.g. "normal", "bold").</summary>
    public const string FontWeight        = "font.weight";

    // Lines
    /// <summary>rc parameter key for the default line width in pixels.</summary>
    public const string LinesLineWidth    = "lines.linewidth";
    /// <summary>rc parameter key for the default line style (e.g. "solid", "dashed").</summary>
    public const string LinesLineStyle    = "lines.linestyle";

    // Axes
    /// <summary>rc parameter key for the axes background fill color.</summary>
    public const string AxesFaceColor     = "axes.facecolor";
    /// <summary>rc parameter key for whether grid lines are shown by default.</summary>
    public const string AxesGrid          = "axes.grid";

    // Grid
    /// <summary>rc parameter key for the grid line color.</summary>
    public const string GridColor         = "grid.color";
    /// <summary>rc parameter key for the grid line width in pixels.</summary>
    public const string GridLineWidth     = "grid.linewidth";
    /// <summary>rc parameter key for the grid line opacity (0.0–1.0).</summary>
    public const string GridAlpha         = "grid.alpha";

    // Figure
    /// <summary>rc parameter key for the default figure width in inches.</summary>
    public const string FigureFigSizeWidth  = "figure.figsize.width";
    /// <summary>rc parameter key for the default figure height in inches.</summary>
    public const string FigureFigSizeHeight = "figure.figsize.height";
    /// <summary>rc parameter key for the figure dots-per-inch resolution.</summary>
    public const string FigureDpi           = "figure.dpi";
    /// <summary>rc parameter key for the figure background fill color.</summary>
    public const string FigureFaceColor     = "figure.facecolor";

    // Text
    /// <summary>rc parameter key for the default text color.</summary>
    public const string TextColor         = "text.color";

    // Scatter / markers
    /// <summary>rc parameter key for the default scatter plot marker style.</summary>
    public const string ScatterMarker     = "scatter.marker";

    // Image
    /// <summary>rc parameter key for the default colormap used by image and heatmap series.</summary>
    public const string ImageCmap         = "image.cmap";
}
