// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The marker of a part of a chart's SVG sent in parts: every drawn part (<c>background</c>, <c>axes-0</c>, …,
/// <c>overlay</c>) stands in a group of its own, named by an attribute, so a viewer that keeps the whole document can put a part
/// that changed in place of the one of the same name. The library writes the groups by it, and the Blazor client and the
/// Interactive page find them by it; a client of your own finds them the same way.</summary>
public static class SvgPartMarker
{
    /// <summary>The attribute that names a part.</summary>
    public const string Attribute = "data-mpl-part";

    /// <summary>What the group of a part begins with, up to its name: the markup of every part starts with it.</summary>
    public const string Opening = "<g " + Attribute + "=\"";

    /// <summary>What closes the group of a part, with its line break.</summary>
    public const string Closing = "</g>" + LineEndExtensions.LineEnd;

    /// <summary>The opening tag of the group of the part named <paramref name="name"/>, without its line break.</summary>
    /// <param name="name">The part's name.</param>
    /// <returns>The tag.</returns>
    public static string Start(string name) => Opening + name + "\">";
}
