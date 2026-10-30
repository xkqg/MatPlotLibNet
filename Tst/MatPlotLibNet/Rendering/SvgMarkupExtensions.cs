// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>One start tag of an SVG document as a test reads it: the element name and its attributes, in document order.</summary>
/// <param name="Name">The element name (<c>rect</c>, <c>ellipse</c>, ...).</param>
/// <param name="Attributes">The attribute values by name, as written.</param>
public readonly record struct SvgElement(string Name, IReadOnlyDictionary<string, string> Attributes)
{
    /// <summary>The value of <paramref name="attribute"/>, or <see langword="null"/> when the tag does not carry it.</summary>
    public string? this[string attribute] => Attributes.TryGetValue(attribute, out string? value) ? value : null;

    /// <summary>The value of <paramref name="attribute"/> as an invariant-culture number.</summary>
    public double Number(string attribute) => double.Parse(Attributes[attribute], CultureInfo.InvariantCulture);
}

/// <summary>The one way the tests read the markup a figure draws: the start tags of an element, the ids it defines and the
/// ids it refers to. Tests used to keep a regular expression each for the same few questions.</summary>
public static partial class SvgMarkupExtensions
{
    [GeneratedRegex(@"([\w:-]+)=""([^""]*)""")]
    private static partial Regex AttributePattern();

    [GeneratedRegex(@"\bid=""([^""]*)""")]
    private static partial Regex IdPattern();

    [GeneratedRegex("""url\(#([^)]*)\)""")]
    private static partial Regex UrlPattern();

    extension(string svg)
    {
        /// <summary>Every start tag of the element called <paramref name="name"/>, in document order.</summary>
        public IReadOnlyList<SvgElement> Elements(string name) =>
        [
            .. Regex.Matches(svg, $@"<{Regex.Escape(name)}(?=[\s/>])([^>]*)>").Select(tag => new SvgElement(name,
                AttributePattern().Matches(tag.Groups[1].Value).ToDictionary(attribute => attribute.Groups[1].Value, attribute => attribute.Groups[2].Value))),
        ];

        /// <summary>Every <c>id</c> the document defines, in document order.</summary>
        public IReadOnlyList<string> Ids() => [.. IdPattern().Matches(svg).Select(id => id.Groups[1].Value)];

        /// <summary>Every id a <c>url(#id)</c> reference points at, in document order.</summary>
        public IReadOnlyList<string> UrlReferences() => [.. UrlPattern().Matches(svg).Select(reference => reference.Groups[1].Value)];
    }
}
