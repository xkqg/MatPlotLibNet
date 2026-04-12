// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using Microsoft.DotNet.Interactive.Formatting;

namespace MatPlotLibNet.Notebooks;

/// <summary>Formats a <see cref="Figure"/> as an inline SVG fragment for Polyglot / Jupyter notebook output.</summary>
internal static class FigureFormatter
{
    /// <summary>Registers the formatter with the <see cref="Formatter"/> registry.</summary>
    internal static void Register()
    {
        Formatter.Register<Figure>((figure, writer) =>
        {
            string svg = figure.ToSvg();
            writer.Write($"<div style=\"overflow:auto;max-width:100%;\">{svg}</div>");
        }, mimeType: HtmlFormatter.MimeType);
    }
}
