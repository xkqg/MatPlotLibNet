// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Transforms;

/// <summary>The file extensions the library knows a format for. Core owns the strings: <c>Figure.Save</c> routes on them,
/// <c>MatPlotLibNet.Skia</c> registers its exporters under them, and the MCP server maps them to its chart formats, so no
/// project spells <c>".png"</c> a second time.</summary>
internal static class ExportExtension
{
    internal const string Svg = ".svg";
    internal const string Json = ".json";
    internal const string Png = ".png";
    internal const string Pdf = ".pdf";
    internal const string Gif = ".gif";
}
