// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests;

/// <summary>What <c>Figure.Save(path, options)</c> does with each setting of <see cref="SaveOptions"/> in the formats
/// core writes itself: SVG uses the indentation, the number of decimals, the title and the author; JSON is a data
/// document and uses none of them. Every setting a format cannot use is a documented no-op, and a test below says so.</summary>
public class SaveOptionsExportTests
{
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    private static Figure Chart(bool interactive = false)
    {
        var builder = Plt.Create().WithSize(500, 350).WithTitle("Revenue")
            .Plot([0.0, 0.123456, 1.2345678, 2.5], [1.0, 3.14159265, 2.71828183, 0.5]);
        if (interactive) builder.WithZoomPan().WithRichTooltips();
        return builder.Build();
    }

    private static string SavedText(Figure figure, string fileName, SaveOptions? options)
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}_{fileName}");
        try
        {
            figure.Save(path, options);
            return File.ReadAllText(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── nothing asked, nothing changed ───────────────────────────────────────────────────────────────

    [Fact]
    public void Svg_WithoutSvgSettings_IsTheFileNoOptionsWrite()
    {
        var figure = Chart();

        string none = SavedText(figure, "a.svg", null);

        Assert.Equal(none, SavedText(figure, "a.svg", new SaveOptions()));
        Assert.Equal(none, SavedText(figure, "a.svg", new SaveOptions { Dpi = 300 }));
        Assert.Equal(none, SavedText(figure, "a.svg", new SaveOptions { Title = "", Author = "" }));
        Assert.Equal(figure.ToSvg(), none);
    }

    [Fact]
    public void Json_IsADataDocument_AndUsesNoSetting()
    {
        var figure = Chart();
        var everything = new SaveOptions
        {
            Dpi = 300, PrettifySvg = true, SvgDecimalPrecision = 1, Title = "T", Author = "A",
        };

        Assert.Equal(figure.ToJson(), SavedText(figure, "a.json", everything));
        Assert.Equal(figure.ToJson(), SavedText(figure, "a.json", null));
    }

    // ── PrettifySvg ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PrettifySvg_IndentsTheNestedElements()
    {
        string svg = SavedText(Chart(), "a.svg", new SaveOptions { PrettifySvg = true });

        string[] lines = SourceFile.LinesOf(svg);
        Assert.True(lines.Length > 10, "an indented document has a line per element");
        Assert.StartsWith("<svg", lines[0]);
        Assert.Contains(lines, l => l.StartsWith("  <") && !l.StartsWith("   "));
        Assert.Contains(lines, l => l.StartsWith("    <") && !l.StartsWith("     "));
    }

    [Fact]
    public void PrettifySvg_IsTheSameDocument_OnlyIndented()
    {
        var figure = Chart(interactive: true);

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument pretty = SavedText(figure, "a.svg", new SaveOptions { PrettifySvg = true }).Xml();

        Assert.True(XNode.DeepEquals(plain.Root, pretty.Root), "indenting changed the document, not only its layout");
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("PrettifySvg_AddsNoSpaceInsideText", () => SavedText(Chart(), "a.svg", new SaveOptions { PrettifySvg = true }), ">Revenue<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void PrettifySvg_LeavesAnInlineScriptWhole()
    {
        var figure = Chart(interactive: true);
        string Script(string svg) => svg.Xml().Each("script").First().Value;

        string plain = Script(SavedText(figure, "a.svg", null));
        string pretty = Script(SavedText(figure, "a.svg", new SaveOptions { PrettifySvg = true }));

        Assert.NotEmpty(plain);
        Assert.Equal(plain, pretty);
    }

    // ── SvgDecimalPrecision ──────────────────────────────────────────────────────────────────────────

    private static readonly string[] Coordinates =
        ["x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "width", "height", "d", "points", "transform", "viewBox"];

    private static readonly string[] Styling = ["stroke-width", "stroke-dasharray", "opacity", "fill-opacity", "stop-opacity", "offset", "font-size"];

    private static IEnumerable<string> ValuesOf(XDocument svg, IEnumerable<string> attributes) =>
        svg.Descendants().SelectMany(e => e.Attributes()).Where(a => attributes.Contains(a.Name.LocalName)).Select(a => a.Value);

    private static int MostDecimals(IEnumerable<string> values) =>
        values.SelectMany(v => Regex.Matches(v, @"\d+\.(\d+)").Select(m => m.Groups[1].Length)).DefaultIfEmpty(0).Max();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SvgDecimalPrecision_LimitsTheDecimalsOfEveryCoordinate(int decimals)
    {
        var figure = Chart();

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument rounded = SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = decimals }).Xml();

        Assert.True(MostDecimals(ValuesOf(plain, Coordinates)) > decimals, "the fixture has no coordinate worth rounding");
        Assert.True(MostDecimals(ValuesOf(rounded, Coordinates)) <= decimals);
    }

    /// <summary>The drawing is written to two decimals of a pixel, so a precision of two or more has nothing left to round:
    /// every coordinate is the one written without it.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(15)]
    public void SvgDecimalPrecision_OfTwoOrMore_LeavesTheDrawingAsWritten(int decimals)
    {
        var figure = Chart();

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument rounded = SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = decimals }).Xml();

        Assert.Equal(2, MostDecimals(ValuesOf(plain, Coordinates)));
        Assert.Equal(ValuesOf(plain, Coordinates), ValuesOf(rounded, Coordinates));
    }

    [Fact]
    public void SvgDecimalPrecision_LeavesWidthsOpacitiesAndSizesOfTextAsWritten()
    {
        // A stroke of 1.5 rounded to a whole number would be 2 (or, for 0.4, 0): a look, not a position.
        var figure = Chart();

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument rounded = SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = 0 }).Xml();

        Assert.NotEmpty(ValuesOf(plain, Styling));
        Assert.Equal(ValuesOf(plain, Styling), ValuesOf(rounded, Styling));
    }

    [Fact]
    public void SvgDecimalPrecision_LeavesTextAndDataAttributesAsWritten()
    {
        var figure = Chart(interactive: true);

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument rounded = SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = 0 }).Xml();

        static IEnumerable<string> Data(XDocument svg) => svg.Descendants().SelectMany(e => e.Attributes())
            .Where(a => a.Name.LocalName.StartsWith("data-", StringComparison.Ordinal)).Select(a => $"{a.Name.LocalName}={a.Value}");
        static IEnumerable<string> Texts(XDocument svg) => svg.Each("text").Select(e => e.Value);

        Assert.NotEmpty(Data(plain));
        Assert.Equal(Data(plain), Data(rounded));
        Assert.NotEmpty(Texts(plain));
        Assert.Equal(Texts(plain), Texts(rounded));
        Assert.Equal(
            plain.Each("script").First().Value,
            rounded.Each("script").First().Value);
    }

    [Fact]
    public void SvgDecimalPrecision_KeepsTheStructureOfTheDocument()
    {
        var figure = Chart();

        XDocument plain = SavedText(figure, "a.svg", null).Xml();
        XDocument rounded = SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = 1 }).Xml();

        Assert.Equal(
            plain.Descendants().Select(e => e.Name.LocalName),
            rounded.Descendants().Select(e => e.Name.LocalName));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void SvgDecimalPrecision_OutsideZeroToFifteen_IsRefused(int decimals)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => SavedText(Chart(), "a.svg", new SaveOptions { SvgDecimalPrecision = decimals }));

        Assert.Equal("SvgDecimalPrecision", refusal.ParamName);
    }

    [Fact]
    public void SvgDecimalPrecision_OfFifteen_IsAccepted()
    {
        var figure = Chart();

        Assert.Equal(
            SavedText(figure, "a.svg", null).Xml().Root!.Descendants().Count(),
            SavedText(figure, "a.svg", new SaveOptions { SvgDecimalPrecision = 15 }).Xml().Root!.Descendants().Count());
    }

    // ── Title and Author ─────────────────────────────────────────────────────────────────────────────

    private static XElement? Metadata(XDocument svg) => svg.Root!.Children("metadata").FirstOrDefault();

    [Fact]
    public void TitleAndAuthor_AreWrittenAsDublinCoreMetadata()
    {
        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }).Xml();

        var description = Metadata(svg)!.Element(Rdf + "RDF")!.Element(Rdf + "Description")!;
        Assert.Equal("Annual Revenue", description.Element(Dc + "title")!.Value);
        Assert.Equal("Finance Team", description.Element(Dc + "creator")!.Value);
    }

    [Fact]
    public void ATitleAlone_WritesNoCreator()
    {
        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Title = "Annual Revenue" }).Xml();

        var description = Metadata(svg)!.Element(Rdf + "RDF")!.Element(Rdf + "Description")!;
        Assert.NotNull(description.Element(Dc + "title"));
        Assert.Null(description.Element(Dc + "creator"));
    }

    [Fact]
    public void AnAuthorAlone_WritesNoTitle()
    {
        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Author = "Finance Team" }).Xml();

        var description = Metadata(svg)!.Element(Rdf + "RDF")!.Element(Rdf + "Description")!;
        Assert.Null(description.Element(Dc + "title"));
        Assert.NotNull(description.Element(Dc + "creator"));
    }

    [Fact]
    public void MetadataText_IsEscaped_AndReadsBackAsWritten()
    {
        const string title = "R&D <2026> \"plan\" — 東京";

        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Title = title }).Xml();

        Assert.Equal(title, Metadata(svg)!.Descendants(Dc + "title").Single().Value);
    }

    [Fact]
    public void TheAccessibleName_IsNotReplacedByTheDocumentTitle()
    {
        // <title> is what a screen reader announces for the picture; the document title is metadata about the file.
        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Title = "Annual Revenue" }).Xml();

        Assert.Equal("Revenue", svg.Root!.Children("title").First().Value);
    }

    [Fact]
    public void Metadata_StandsBetweenTheTitleAndTheDrawing()
    {
        XDocument svg = SavedText(Chart(), "a.svg", new SaveOptions { Title = "Annual Revenue" }).Xml();

        var names = svg.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal("title", names[0]);
        Assert.Equal("metadata", names[1]);
    }

    [Fact]
    public void Metadata_StandsAfterTheDescription_WhenThereIsOne()
    {
        var figure = Plt.Create().WithSize(200, 100).WithDescription("A line").Plot([0.0, 1.0], [0.0, 1.0]).Build();

        XDocument svg = SavedText(figure, "a.svg", new SaveOptions { Author = "A" }).Xml();

        var names = svg.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "title", "desc", "metadata" }, names.Take(3));
    }

    // ── where the SVG goes ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AFileWithoutExtension_IsAnSvg_AndTakesTheSettings()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}");
        try
        {
            Chart().Save(path, new SaveOptions { PrettifySvg = true, Title = "T" });

            string svg = File.ReadAllText(path + ".svg");
            Assert.Contains("\n  <", svg);
            Assert.Contains("<dc:title>T</dc:title>", svg);
        }
        finally
        {
            if (File.Exists(path + ".svg")) File.Delete(path + ".svg");
        }
    }

    [Fact]
    public void AnUnknownExtension_IsAnSvg_AndTakesTheSettings()
    {
        string svg = SavedText(Chart(), "a.chart", new SaveOptions { PrettifySvg = true, Title = "T" });

        Assert.StartsWith("<svg", svg);
        Assert.Contains("\n  <", svg);
        Assert.Contains("<dc:title>T</dc:title>", svg);
    }

    [Fact]
    public void AnUnknownExtension_WithoutOptions_IsTheFiguresOwnSvg()
    {
        var figure = Chart();

        Assert.Equal(figure.ToSvg(), SavedText(figure, "a.chart", null));
    }

    [Fact]
    public void AFileWithoutExtension_WithoutOptions_IsTheFiguresOwnSvg()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}");
        try
        {
            var figure = Chart();
            figure.Save(path);

            Assert.Equal(figure.ToSvg(), File.ReadAllText(path + ".svg"));
        }
        finally
        {
            if (File.Exists(path + ".svg")) File.Delete(path + ".svg");
        }
    }

    [Fact]
    public void SaveSvg_IsUnchanged()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}.svg");
        try
        {
            var figure = Chart();
            figure.SaveSvg(path);

            Assert.Equal(figure.ToSvg(), File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── a transform written by someone else ──────────────────────────────────────────────────────────

    /// <summary>A transform that reads the options.</summary>
    private sealed class OptionsTransform() : RecordingFigureTransform("no-options"), IFigureTransform
    {
        public SaveOptions? Seen { get; private set; }

        void IFigureTransform.Transform(Figure figure, Stream output, SaveOptions options)
        {
            Seen = options;
            output.Write("with-options"u8);
        }
    }

    [Fact]
    public void ATransformThatKnowsNoOptions_StillWritesItsFile_WhenOptionsAreGiven()
    {
        var transform = new RecordingFigureTransform("plain");
        FigureExtensions.RegisterTransform(".s9plain", transform);

        string text = SavedText(Chart(), "a.s9plain", new SaveOptions { Dpi = 300, Title = "T" });

        Assert.Equal("plain", text);
        Assert.Equal(1, transform.Calls);
    }

    [Fact]
    public void ATransformThatReadsOptions_ReceivesTheOnesGiven()
    {
        var transform = new OptionsTransform();
        FigureExtensions.RegisterTransform(".s9options", transform);
        var options = new SaveOptions { Dpi = 300 };

        string text = SavedText(Chart(), "a.s9options", options);

        Assert.Equal("with-options", text);
        Assert.Same(options, transform.Seen);
    }

    [Fact]
    public void ATransformThatReadsOptions_IsCalledWithoutThem_WhenNoneAreGiven()
    {
        var transform = new OptionsTransform();
        FigureExtensions.RegisterTransform(".s9options2", transform);

        string text = SavedText(Chart(), "a.s9options2", null);

        Assert.Equal("no-options", text);
        Assert.Null(transform.Seen);
    }

    [Fact]
    public void AnExportThatFails_LeavesTheFileThatWasThere()
    {
        FigureExtensions.RegisterTransform(".s9failing", new RecordingFigureTransform("half a file", new InvalidOperationException("the export failed half way")));
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}.s9failing");
        try
        {
            File.WriteAllText(path, "the report from last week");

            Assert.Throws<InvalidOperationException>(() => Chart().Save(path));

            Assert.Equal("the report from last week", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
