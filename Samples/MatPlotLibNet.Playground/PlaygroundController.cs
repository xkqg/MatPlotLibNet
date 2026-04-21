// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Playground;

/// <summary>
/// Pure-C# selection and build logic extracted from the Blazor page so it can be unit-tested.
/// The Razor page delegates its <c>OnThemeChanged</c>, <c>OnColorMapChanged</c>, and
/// <c>Rebuild</c> logic here; the controller has no Blazor dependencies.
/// </summary>
public static class PlaygroundController
{
    /// <summary>
    /// Resolves a raw HTML <c>select</c> value (an integer index as a string) to the
    /// corresponding <see cref="Theme"/>.  Returns <c>null</c> when <paramref name="rawValue"/>
    /// is not a valid integer or the index is out of range.
    /// </summary>
    public static Theme? SelectThemeByIndex(string? rawValue, (Theme Theme, string Label)[] choices)
    {
        if (!int.TryParse(rawValue, out int idx)) return null;
        if (idx < 0 || idx >= choices.Length) return null;
        return choices[idx].Theme;
    }

    /// <summary>
    /// Resolves a raw HTML <c>select</c> value to the corresponding <see cref="IColorMap"/>.
    /// Returns <c>null</c> when the value is not a valid integer or the index is out of range.
    /// </summary>
    public static IColorMap? SelectColorMapByIndex(string? rawValue, IColorMap[] choices)
    {
        if (!int.TryParse(rawValue, out int idx)) return null;
        if (idx < 0 || idx >= choices.Length) return null;
        return choices[idx];
    }

    /// <summary>
    /// Builds an SVG and code snippet for <paramref name="example"/>.
    /// Returns <c>true</c> on success; <c>false</c> on failure with <paramref name="error"/>
    /// set to the exception message (HTML-safe inline error markup) and svg/code set to "".
    /// </summary>
    public static bool TryBuild(
        PlaygroundExample example,
        PlaygroundOptions options,
        out string svg,
        out string code,
        out string error)
    {
        try
        {
            var (figure, snippet) = PlaygroundExamples.Build(example, options);
            svg   = figure.ToSvg();
            code  = snippet;
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            svg   = "<p style='color:red;padding:20px'>" + ex.Message + "</p>";
            code  = "";
            error = ex.Message;
            return false;
        }
    }
}
