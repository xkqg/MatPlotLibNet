// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

public class SvgMarkupExtensionsTests
{
    private const string Markup =
        """
        <svg width="40" height="30"><defs><clipPath id="clip-0"><rect x="1" y="2" width="3.5" height="4"/></clipPath></defs>
        <g clip-path="url(#clip-0)"><rect x="10" y="20" width="30" height="40" fill="#ff0000"/>
        <ellipse cx="5" cy="6" rx="7" ry="8"></ellipse><gradient id="g1"/><text x="1" y="2">a &gt; b</text></g></svg>
        """;

    [Fact]
    public void TheElementsOfAName_AreItsStartTags_InDocumentOrder()
    {
        var rects = Markup.Elements("rect");

        Assert.Equal(2, rects.Count);
        Assert.Equal("1", rects[0]["x"]);
        Assert.Equal("#ff0000", rects[1]["fill"]);
    }

    [Fact]
    public void ANumber_IsReadWithTheInvariantCulture()
    {
        Assert.Equal(3.5, Markup.Elements("rect")[0].Number("width"));
    }

    [Fact]
    public void AnAttributeTheTagDoesNotCarry_IsNull()
    {
        Assert.Null(Markup.Elements("ellipse")[0]["fill"]);
    }

    [Fact]
    public void ANameIsNotAPrefixOfAnotherName()
    {
        // "g" must not find <gradient ...>, and "text" finds its tag though the content carries an escaped ">".
        Assert.Single(Markup.Elements("g"));
        Assert.Single(Markup.Elements("gradient"));
        Assert.Single(Markup.Elements("text"));
    }

    [Fact]
    public void AClosingTag_IsNotAStartTag()
    {
        Assert.Single(Markup.Elements("ellipse"));
    }

    [Fact]
    public void TheIds_AreEveryIdTheDocumentDefines()
    {
        Assert.Equal(["clip-0", "g1"], Markup.Ids());
    }

    [Fact]
    public void TheUrlReferences_AreEveryIdAReferencePointsAt()
    {
        Assert.Equal(["clip-0"], Markup.UrlReferences());
    }

    [Fact]
    public void ANumberTheTagLacks_IsRefusedByName()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => Markup.Elements("ellipse")[0].Number("fill"));

        Assert.Contains("fill", ex.Message, StringComparison.Ordinal);
    }

    private const string Picture =
        """
        <svg width="800" height="600" viewBox="0 0 800 600">
        <rect x="0" y="0" width="800" height="600" fill="#FFFFFF" />
        <rect x="100" y="72" width="620" height="462" fill="#FFFFFF" />
        <text x="90.5" y="300" font-size="13.89" text-anchor="end" fill="#112233">1.00</text>
        <text x="410" y="557.5" font-size="13.89" text-anchor="middle" fill="#445566">2.00</text>
        <text x="410" y="40" font-size="20" text-anchor="middle" fill="#778899">Title</text>
        <g class="series" data-series-index="0">
        <defs><clipPath id="clip-0"><rect x="100" y="72" width="620" height="462" /></clipPath></defs>
        <g clip-path="url(#clip-0)">
        <polyline points="1,2 3,4" fill="none" stroke="#1F77B4" />
        </g>
        <text x="5" y="6" fill="#AA0000">end label</text>
        </g>
        <g class="series" data-series-index="1">
        <polyline points="5,6 7,8" fill="none" stroke="#FF7F0E" />
        </g>
        <g class="legend"><rect x="1" y="2" width="3" height="4" /></g>
        </svg>
        """;

    [Fact]
    public void TheXmlOfAPicture_IsItsParsedDocument_AndEachFindsADescendantByItsLocalName()
    {
        Assert.Equal("svg", Picture.Xml().Root!.Name.LocalName);
        Assert.Equal(["1.00", "2.00", "Title", "end label"], Picture.Each("text").Select(e => e.Value));
        Assert.Equal(["1.00", "2.00", "Title", "end label"], Picture.Xml().Each("text").Select(e => e.Value));
        Assert.Equal(4, Picture.Each("rect").Count());
    }

    [Fact]
    public void TheDirectChildrenOfThePicture_AreTheShapesDrawnIntoTheFigureItself()
    {
        Assert.Equal(["1.00", "2.00", "Title"], Picture.Direct("text").Select(e => e.Value));
        Assert.Equal(2, Picture.Direct("rect").Count());
    }

    [Fact]
    public void TheChildrenOfAnElement_AreItsOwnElementsOfALocalName_NotTheOnesBelowThem()
    {
        var group = Picture.Each("g").First(g => (string?)g.Attribute("class") == "series");

        Assert.Equal(["end label"], group.Children("text").Select(e => e.Value));
        Assert.Single(group.Children("g"));
        Assert.Single(group.Each("polyline"));
    }

    [Fact]
    public void AnElementIs_WhenItsLocalNameIsTheOneAsked()
    {
        var group = Picture.Each("g").First();

        Assert.True(group.Is("g"));
        Assert.False(group.Is("rect"));
    }

    [Fact]
    public void ANumberOfAnXmlElement_IsReadWithTheInvariantCulture_AndAStringIsRead()
    {
        Assert.Equal(90.5, Picture.Each("text").First().Number("x"));
        Assert.Equal(13.89, "13.89".Number());
    }

    [Fact]
    public void CountingAPattern_CountsEachNonOverlappingOccurrence()
    {
        Assert.Equal(2, "aaaa".CountOf("aa"));
        Assert.Equal(2, Picture.CountOf("<polyline"));
        Assert.Equal(0, Picture.CountOf("<nothing"));
    }

    [Fact]
    public void ASeriesGroup_RunsToItsOwnClosingTag_NotToTheFirstClosingTagInside()
    {
        string first = Picture.SeriesGroup(0);

        Assert.StartsWith("<g class=\"series\" data-series-index=\"0\">", first, StringComparison.Ordinal);
        Assert.EndsWith("</g>", first, StringComparison.Ordinal);
        Assert.Contains("end label", first, StringComparison.Ordinal);
        Assert.Contains("#1F77B4", first, StringComparison.Ordinal);
        Assert.DoesNotContain("#FF7F0E", first, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"legend\"", Picture.SeriesGroup(1), StringComparison.Ordinal);
        Assert.Contains("#FF7F0E", Picture.SeriesGroup(1), StringComparison.Ordinal);
    }

    [Fact]
    public void ASeriesGroupThePictureLacks_IsRefusedByItsIndex()
    {
        var ex = Assert.ThrowsAny<Exception>(() => Picture.SeriesGroup(7));

        Assert.Contains("7", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASeriesGroupThatNeverCloses_IsRefused()
    {
        var ex = Assert.ThrowsAny<Exception>(() => "<svg><g class=\"series\" data-series-index=\"0\"><g></g></svg>".SeriesGroup(0));

        Assert.Contains("close", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSeriesGroups_AreTheGroupsOfClassSeries_InDocumentOrder()
    {
        Assert.Equal(["0", "1"], Picture.SeriesGroups().Select(g => (string)g.Attribute("data-series-index")!));
    }

    [Fact]
    public void ThePlotArea_IsTheSecondRectangleDrawnIntoTheFigure_AfterItsOwnBackground()
    {
        Assert.Equal(new Rect(100, 72, 620, 462), Picture.PlotArea());
    }

    [Fact]
    public void TheFillOfAText_IsTheFillOfTheOneTextThatReadsIt()
    {
        Assert.Equal("#778899", Picture.FillOf("Title"));
        Assert.Throws<Xunit.Sdk.TrueException>(() => Picture.FillOf("nothing like this"));
    }

    [Fact]
    public void TheTextYs_AreTheSortedBaselines_OfTheTextsOfAFontSize()
    {
        Assert.Equal([6.0, 40, 300, 557.5], Picture.TextYs());
        Assert.Equal([300.0, 557.5], Picture.TextYs("13.89"));
    }

    [Fact]
    public void TheTextsAnchored_AreTheTickLabelsOfOneAnchor_WithWhereTheyStand()
    {
        var labels = Picture.TextsAnchored("middle");

        Assert.Equal([new DrawnText("2.00", 410, 557.5), new DrawnText("Title", 410, 40)], labels);
        Assert.Equal([new DrawnText("1.00", 90.5, 300)], Picture.TextsAnchored("end"));
    }

    [Fact]
    public void ThePointsOfASeries_AreThoseOfTheLinesOfItsLabelledGroup_RunAfterRun()
    {
        string svg = "<svg><g aria-label=\"a\" data-series-index=\"0\"><polyline points=\"1,2 3,4\"/><polyline points=\"5,6\"/></g>"
            + "<g aria-label=\"b\" data-series-index=\"1\"><polyline points=\"9,9\"/></g></svg>";

        Assert.Equal([new Point(1, 2), new Point(3, 4), new Point(5, 6)], svg.PointsOf("a"));
        Assert.Equal([new Point(9, 9)], svg.PointsOf("b"));
        Assert.ThrowsAny<InvalidOperationException>(() => svg.PointsOf("c"));
    }

    [Fact]
    public void ThePolylinePoints_AreEveryPointOfEveryPolyline_AndAnUnreadableCoordinateIsNaN()
    {
        var points = "<polyline points=\"1,2 3,x\"/><polyline points=\"5,6\"/>".PolylinePoints();

        Assert.Equal(3, points.Count);
        Assert.Equal(new Point(1, 2), points[0]);
        Assert.True(double.IsNaN(points[1].Y));
        Assert.Equal(new Point(5, 6), points[2]);
    }

    [Fact]
    public void TheTextOfAnElement_IsWhatLiesBetweenItsStartTagAndTheNextTag_AsWritten()
    {
        Assert.Equal("a &gt; b", Markup.Elements("text")[0].Text);
        Assert.Equal(["1.00", "2.00", "Title", "end label"], Picture.Elements("text").Select(text => text.Text));
        Assert.Equal(string.Empty, Markup.Elements("rect")[1].Text);
        Assert.Equal(string.Empty, Markup.Elements("ellipse")[0].Text);
    }

    [Fact]
    public void ThePointsOfAnElement_AreItsPointsAttribute_PairByPair()
    {
        Assert.Equal([new Point(1, 2), new Point(3, 4)], Picture.Elements("polyline")[0].Points());
        Assert.Equal([new Point(5, 6), new Point(7, 8)], Picture.Elements("polyline")[1].Points());
    }

    [Fact]
    public void TheLegendEntries_AreTheGroupsThatCarryALegendIndex_WithTheirLabels()
    {
        const string legend = "<g class=\"legend\"><g data-legend-index=\"0\" class=\"entry\" aria-label=\"a\"><rect/></g>"
            + "<g data-legend-index=\"2\" aria-label=\"c &amp; d\"/><g class=\"series\" data-series-index=\"2\"/></g>";

        Assert.Equal([new DrawnLegendEntry(0, "a"), new DrawnLegendEntry(2, "c &amp; d")], legend.LegendEntries());
        Assert.Empty(Picture.LegendEntries());
    }

    [Fact]
    public void TheFirstColour_IsTheFirstFillOrStrokeThatIsAHexColour()
    {
        Assert.Equal("#1F77B4", Picture.SeriesGroup(0).FirstColour());
        Assert.Equal("#FF7F0E", Picture.SeriesGroup(1).FirstColour());
        Assert.Equal("#00ff00", "<rect fill=\"none\" stroke=\"#00ff00\" fill=\"#112233\"/>".FirstColour());
        Assert.Equal(string.Empty, "<rect fill=\"none\"/>".FirstColour());
    }

    [Fact]
    public void TheValuesOfAnAttribute_AreEveryOneTheDocumentWrites_InOrder()
    {
        Assert.Equal(["0", "1"], Picture.AttributeValues("data-series-index"));
        Assert.Equal(["#1F77B4", "#FF7F0E"], Picture.AttributeValues("stroke"));
        Assert.Empty(Picture.AttributeValues("data-nothing"));
    }

    [Fact]
    public void ATextThatReadsAString_IsFoundAsItIsWritten_AndAMissingOneIsRefused()
    {
        Assert.Equal("90.5", Picture.TextReading("1.00")["x"]);
        Assert.Equal(300, Picture.TextReading("1.00").Number("y"));
        Assert.Equal(6, Picture.TextReading("end label").Number("y"));
        Assert.Throws<Xunit.Sdk.TrueException>(() => Picture.TextReading("nothing like this"));
    }

    [Fact]
    public void EveryTag_IsReadWhateverItsElement_InDocumentOrder()
    {
        var all = Picture.AllElements();

        Assert.Equal(["svg", "rect", "rect", "text", "text", "text", "g", "defs", "clipPath", "rect", "g", "polyline", "text", "g", "polyline", "g", "rect"],
            all.Select(element => element.Name).Take(17));
        Assert.Equal("series", all.First(element => element.Name == "g")["class"]);
        Assert.Equal("Title", all.First(element => element.Name == "text" && element["y"] == "40").Text);
    }

    [Fact]
    public void TheRotationOfAnElement_IsTheAngleOfItsRotateTransform_AsWritten()
    {
        var turned = "<text transform=\"rotate(-30,12.5,40)\" x=\"1\">a</text><text transform=\"translate(1,2)\">b</text><text>c</text>".Elements("text");

        Assert.Equal(-30, turned[0].Rotation());
        Assert.Null(turned[1].Rotation());
        Assert.Null(turned[2].Rotation());
    }

    [Fact]
    public void TheRotationCentre_IsThePointTheRotateTransformTurnsAbout_WhenItNamesOne()
    {
        var turned = "<text transform=\"rotate(-90,12.5,40)\">a</text><text transform=\"rotate(30 7 8)\">b</text><text transform=\"rotate(30)\">c</text><text>d</text>".Elements("text");

        Assert.Equal(new Point(12.5, 40), turned[0].RotationCentre());
        Assert.Equal(new Point(7, 8), turned[1].RotationCentre());
        Assert.Null(turned[2].RotationCentre());
        Assert.Null(turned[3].RotationCentre());
    }

    [Fact]
    public void TheRectsFilled_AreTheRectanglesOfOneFill_AsWritten()
    {
        const string rects = "<rect x=\"1\" y=\"2\" width=\"3\" height=\"4\" fill=\"#112233\"/><rect x=\"5\" y=\"6\" width=\"7\" height=\"8\" fill=\"none\"/>"
            + "<rect x=\"9\" y=\"10\" width=\"11\" height=\"12\" fill=\"#112233\" fill-opacity=\"0.5\"/>";

        Assert.Equal([new Rect(1, 2, 3, 4), new Rect(9, 10, 11, 12)], rects.RectsFilled("#112233"));
        Assert.Equal([new Rect(5, 6, 7, 8)], rects.RectsFilled("none"));
        Assert.Empty(rects.RectsFilled("#000000"));
    }

    [Fact]
    public void TheVerticalLineXs_AreTheXOfTheLongLinesOfOneStrokeThatStandUpright()
    {
        const string lines = "<line x1=\"10\" y1=\"0\" x2=\"10\" y2=\"200\" stroke=\"#FFFFFF\"/><line x1=\"20\" y1=\"0\" x2=\"20\" y2=\"5\" stroke=\"#FFFFFF\"/>"
            + "<line x1=\"30\" y1=\"0\" x2=\"40\" y2=\"200\" stroke=\"#FFFFFF\"/><line x1=\"50\" y1=\"300\" x2=\"50\" y2=\"0\" stroke=\"#FFFFFF\"/>"
            + "<line x1=\"60\" y1=\"0\" x2=\"60\" y2=\"200\" stroke=\"#000000\"/>";

        Assert.Equal([10, 50], lines.VerticalLineXs("#FFFFFF", 100));
        Assert.Equal([60], lines.VerticalLineXs("#000000", 100));
    }

    [Fact]
    public void TheMarkupWithoutDeclarations_LeavesNoXmlDeclarationAndNoDefaultNamespace_SoSeveralPicturesFitUnderOneRoot()
    {
        string bare = ("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"4\"><rect x=\"1\"/></svg>"
            + "<?xml version=\"1.0\"?><svg  xmlns = \"http://www.w3.org/2000/svg\" width=\"5\"/>").WithoutDeclarations();

        Assert.DoesNotContain("<?xml", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("xmlns", bare, StringComparison.Ordinal);
        Assert.Equal(["4", "5"], $"<html>{bare}</html>".Elements("svg").Select(svg => svg["width"]));
        Assert.Equal("1", $"<html>{bare}</html>".Elements("rect")[0]["x"]);
    }

    [Fact]
    public void TheVerticesOfAnElement_AreTheTriplesOfItsVertexData_InTheOrderWritten()
    {
        var line = "<line data-v3d=\"-0.5,0.25,1 0.5,-0.25,2\" stroke=\"#112233\"/>".Elements("line")[0];

        Assert.Equal([[-0.5, 0.25, 1], [0.5, -0.25, 2]], line.Vertices());
    }

    [Fact]
    public void TheScripts_AreTheBodiesOfTheScriptElements_AndWithoutScriptsLeavesTheRest()
    {
        const string page = "<svg><g/><script type=\"text/javascript\">if (a < b) { x(); }</script><rect x=\"1\"/><script>second()</script></svg>";

        Assert.Equal(["if (a < b) { x(); }", "second()"], page.Scripts());
        Assert.Equal("<svg><g/><rect x=\"1\"/></svg>", page.WithoutScripts());
        Assert.Empty("<svg/>".Scripts());
    }

    [Fact]
    public void ThePathPoints_AreTheMoveToAndLineToVerticesOfTheData_AndTheSubpathsSplitAtEachZ()
    {
        var path = "<path d=\"M 1 2 L 3.5 -4 L 5e-1 6 Z M 7 8 L 9 10 C 1 1 2 2 3 3 Z \" fill=\"#112233\"/>".Elements("path")[0];

        Assert.Equal([new Point(1, 2), new Point(3.5, -4), new Point(0.5, 6), new Point(7, 8), new Point(9, 10)], path.PathPoints());
        Assert.Equal([[new Point(1, 2), new Point(3.5, -4), new Point(0.5, 6)], [new Point(7, 8), new Point(9, 10)]], path.Subpaths());
    }

    [Fact]
    public void ThePathNumbers_AreEveryNumberOfTheData_CurvesIncluded()
    {
        var path = "<path d=\"M 1 2.5 C -3 4 5 6 7 8 Z\"/>".Elements("path")[0];

        Assert.Equal([1, 2.5, -3, 4, 5, 6, 7, 8], path.PathNumbers());
    }

    [Fact]
    public void WithoutClipPaths_LeavesTheMarkupThatIsDrawn()
    {
        const string page = "<svg><defs><clipPath id=\"c\"><path d=\"M 0 0\"/></clipPath></defs><path d=\"M 1 1\"/><clipPath id=\"d\"><rect/></clipPath></svg>";

        Assert.Equal("<svg><defs></defs><path d=\"M 1 1\"/></svg>", page.WithoutClipPaths());
    }

    [Fact]
    public void AnElementOfTheParsedTree_IsReadLikeAStartTag_ByItsAttributesAndItsText()
    {
        var parsed = "<svg><text x=\"3\" y=\"4\">hello</text></svg>".Each("text").Single();

        var element = SvgElement.Of(parsed);

        Assert.Equal("text", element.Name);
        Assert.Equal(3, element.Number("x"));
        Assert.Equal("hello", element.Text);
        Assert.Null(element["fill"]);
    }

    [Fact]
    public void AGroupOfAClass_RunsToItsOwnClosingTag_AndAMissingOneIsRefused()
    {
        const string page = "<svg><g class=\"legend\"><g class=\"inner\"><rect x=\"1\"/></g><text>kept</text></g><g class=\"later\"/></svg>";

        Assert.Equal("<g class=\"legend\"><g class=\"inner\"><rect x=\"1\"/></g><text>kept</text></g>", page.GroupOf("legend"));
        Assert.Equal("<g class=\"inner\"><rect x=\"1\"/></g>", page.GroupOf("inner"));
        Assert.Throws<Xunit.Sdk.TrueException>(() => page.GroupOf("absent"));
        Assert.Equal(["kept"], page.GroupOf("legend").Elements("text").Select(text => text.Text));
    }

    [Fact]
    public void APointOutsideTheCanvas_IsRefused_AndAGapIsNot()
    {
        SvgMarkupExtensions.AssertPointsInCanvas([new Point(1, 1), new Point(double.NaN, 2)], 0, 10, 0, 10, "ok");

        var ex = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            SvgMarkupExtensions.AssertPointsInCanvas([new Point(11, 1)], 0, 10, 0, 10, "wide"));

        Assert.Contains("wide: point (11, 1) is outside canvas", ex.Message, StringComparison.Ordinal);
    }
}
