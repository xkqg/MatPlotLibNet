// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Samples.Console;

internal static class FigureBuilderSampleExtensions
{
    /// <summary>
    /// Saves the figure to the given SVG path and to the matching .png path — used for
    /// samples that also appear in the GitHub wiki (which only embeds PNG).
    /// </summary>
    public static void SaveSvgAndPng(this FigureBuilder b, string svgPath)
    {
        b.Save(svgPath);
        b.Save(Path.ChangeExtension(svgPath, ".png"));
    }
}
