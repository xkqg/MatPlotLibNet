// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Well-known string keys for <see cref="RcParams"/> entries.</summary>
public static class RcParamKeys
{
    // Font
    public const string FontFamily        = "font.family";
    public const string FontSize          = "font.size";
    public const string FontWeight        = "font.weight";

    // Lines
    public const string LinesLineWidth    = "lines.linewidth";
    public const string LinesLineStyle    = "lines.linestyle";

    // Axes
    public const string AxesFaceColor     = "axes.facecolor";
    public const string AxesGrid          = "axes.grid";

    // Grid
    public const string GridColor         = "grid.color";
    public const string GridLineWidth     = "grid.linewidth";
    public const string GridAlpha         = "grid.alpha";

    // Figure
    public const string FigureFigSizeWidth  = "figure.figsize.width";
    public const string FigureFigSizeHeight = "figure.figsize.height";
    public const string FigureDpi           = "figure.dpi";
    public const string FigureFaceColor     = "figure.facecolor";

    // Text
    public const string TextColor         = "text.color";

    // Scatter / markers
    public const string ScatterMarker     = "scatter.marker";

    // Image
    public const string ImageCmap         = "image.cmap";
}
