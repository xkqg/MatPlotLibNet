// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The settings of <see cref="SaveOptions"/> an SVG can use, applied to a finished SVG document: its
/// indentation, the decimals of its coordinates and the title and author of the file. They act on the document text, so
/// they hold for whichever <see cref="ISvgRenderer"/> wrote it.</summary>
internal static class SvgDocumentExtensions
{
    private const int MostDecimals = 15;

    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace DublinCore = "http://purl.org/dc/elements/1.1/";

    /// <summary>The attributes that place or size a shape. Their numbers are rounded; every other attribute is styling
    /// (a stroke width, an opacity, a dash length, a font size), data or text, and is left as written.</summary>
    private static readonly HashSet<string> CoordinateAttributes = new(StringComparer.Ordinal)
    {
        "x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "dx", "dy",
        "width", "height", "d", "points", "transform", "viewBox",
    };

    /// <summary>A number as the SVG writer emits it: optional sign, digits with an optional fraction, optional exponent.</summary>
    private static readonly Regex Number = new(@"-?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);

    /// <summary>The document with the SVG settings of <paramref name="options"/> applied. Options that ask for none of
    /// them return <paramref name="svg"/> itself, byte for byte.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="SaveOptions.SvgDecimalPrecision"/> is outside 0 to 15.</exception>
    public static string WithSaveOptions(this string svg, SaveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SvgDecimalPrecision is < 0 or > MostDecimals)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SaveOptions.SvgDecimalPrecision), options.SvgDecimalPrecision,
                $"The number of decimals of an SVG must be between 0 and {MostDecimals}.");
        }

        string? title = options.TitleOrNull();
        string? author = options.AuthorOrNull();
        bool hasMetadata = title is not null || author is not null;
        if (!options.PrettifySvg && options.SvgDecimalPrecision is null && !hasMetadata)
        {
            return svg;
        }

        // The whitespace between the elements is layout, not content: it is dropped when the document is to be indented
        // (the writer lays it out again) and kept otherwise, so a document that is not indented keeps its lines.
        XDocument document = XDocument.Parse(svg, options.PrettifySvg ? LoadOptions.None : LoadOptions.PreserveWhitespace);
        XElement root = document.Root!;

        if (hasMetadata)
        {
            AddMetadata(root, title, author);
        }

        if (options.SvgDecimalPrecision is { } decimals)
        {
            RoundCoordinates(root, decimals);
        }

        return Write(document, options.PrettifySvg);
    }

    /// <summary>Writes the Dublin Core title and creator after the <c>title</c> and the <c>desc</c> that name the chart.</summary>
    private static void AddMetadata(XElement root, string? title, string? author)
    {
        var description = new XElement(Rdf + "Description");
        if (title is not null) description.Add(new XElement(DublinCore + "title", title));
        if (author is not null) description.Add(new XElement(DublinCore + "creator", author));

        var metadata = new XElement(root.Name.Namespace + "metadata",
            new XElement(Rdf + "RDF",
                new XAttribute(XNamespace.Xmlns + "rdf", Rdf),
                new XAttribute(XNamespace.Xmlns + "dc", DublinCore),
                description));

        // Where the file keeps one element to a line, the new element takes its own line too.
        XElement? anchor = root.Elements().LastOrDefault(e => e.Name.LocalName is "title" or "desc");
        if (anchor is not null)
        {
            if (anchor.NextNode is XText { Value: var gap } && string.IsNullOrWhiteSpace(gap))
            {
                anchor.AddAfterSelf(new XText(gap), metadata);
            }
            else
            {
                anchor.AddAfterSelf(metadata);
            }
        }
        else if (root.FirstNode is XText { Value: var lead } first && string.IsNullOrWhiteSpace(lead))
        {
            first.AddAfterSelf(metadata, new XText(lead));
        }
        else
        {
            root.AddFirst(metadata);
        }
    }

    private static void RoundCoordinates(XElement root, int decimals)
    {
        foreach (XElement element in root.DescendantsAndSelf())
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (CoordinateAttributes.Contains(attribute.Name.LocalName))
                {
                    attribute.Value = Number.Replace(attribute.Value, match => Round(match.Value, decimals));
                }
            }
        }
    }

    /// <summary>One number rounded half away from zero. A number that already has no more decimals is returned as it
    /// was written, so only digits that are asked to go are touched.</summary>
    private static string Round(string token, int decimals)
    {
        int point = token.IndexOf('.');
        bool exponent = token.AsSpan().IndexOfAny('e', 'E') >= 0;
        if (!exponent && (point < 0 || token.Length - point - 1 <= decimals))
        {
            return token;
        }

        // The decimal type rounds the digits that were written ("0.15" is "0.2"); a double would round the nearest
        // binary fraction, which is a hair under. Only an exponent form, a number that is tiny or huge, takes a double.
        if (!exponent)
        {
            if (!decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal exact))
            {
                return token;
            }

            decimal rounded = Math.Round(exact, decimals, MidpointRounding.AwayFromZero);
            return rounded == 0 ? "0" : rounded.ToString("0." + new string('#', MostDecimals), CultureInfo.InvariantCulture);
        }

        // Every match of Number is a valid float, so this parse cannot fail; one past the range of a double is infinite.
        double value = double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (!double.IsFinite(value))
        {
            return token;
        }

        double roundedValue = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        return roundedValue == 0 ? "0" : roundedValue.ToString("G", CultureInfo.InvariantCulture);
    }

    private static string Write(XDocument document, bool indent)
    {
        var text = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = indent,
            IndentChars = "  ",
        };
        using (var writer = XmlWriter.Create(text, settings))
        {
            document.Save(writer);
        }

        return text.ToString();
    }
}
