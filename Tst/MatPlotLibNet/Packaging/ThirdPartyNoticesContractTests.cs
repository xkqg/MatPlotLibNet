// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A package that carries someone else's files inside its assembly hands the recipient that author's licence and
/// copyright notice with it (MIT and the Bitstream Vera licence ask for the notice in every copy). Every resource a package
/// embeds is either the library's own or is named here with where it comes from, and a package that embeds third-party files
/// packs a <c>THIRD-PARTY-NOTICES.md</c> that carries their licence text. A new embedded file that is in neither list turns
/// this red, so it cannot reach a package without its notice. The tool package, which bundles whole assemblies, is held by
/// the MCP suite's own notices test.</summary>
public class ThirdPartyNoticesContractTests
{
    private const string NoticesFile = "THIRD-PARTY-NOTICES.md";

    /// <summary>One group of embedded files that is not the library's own: the project that embeds it, the csproj
    /// <c>EmbeddedResource</c> include, the words the package's notices must hold, and (when the origin ships a licence
    /// file) the repository file whose whole text the notices must carry.</summary>
    private readonly record struct ThirdPartyResource(string Project, string Include, string[] NoticeNames, string? LicenseFile);

    private static readonly ThirdPartyResource[] ThirdParty =
    [
        new("MatPlotLibNet.Skia", @"Fonts\DejaVuSans.ttf", ["DejaVuSans.ttf", "Bitstream Vera"], "Src/MatPlotLibNet.Skia/Fonts/LICENSE_DEJAVU"),
        new("MatPlotLibNet.Skia", @"Fonts\DejaVuSans-Bold.ttf", ["DejaVuSans-Bold.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\DejaVuSans-Oblique.ttf", ["DejaVuSans-Oblique.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\DejaVuSans-BoldOblique.ttf", ["DejaVuSans-BoldOblique.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\DejaVuSansDisplay.ttf", ["DejaVuSansDisplay.ttf"], null),
        // The Computer Modern faces of matplotlib's math font set 'cm', from matplotlib's mpl-data/fonts/ttf (BaKoMa Fonts Collection).
        new("MatPlotLibNet.Skia", @"Fonts\cmr10.ttf", ["cmr10.ttf", "BaKoMa", "Basil K. Malyshev", "matplotlib"], "Src/MatPlotLibNet.Skia/Fonts/LICENSE_BAKOMA"),
        new("MatPlotLibNet.Skia", @"Fonts\cmmi10.ttf", ["cmmi10.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\cmti10.ttf", ["cmti10.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\cmb10.ttf", ["cmb10.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\cmsy10.ttf", ["cmsy10.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\cmex10.ttf", ["cmex10.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\cmtt10.ttf", ["cmtt10.ttf"], null),
        // The STIX faces matplotlib's math font sets 'cm' and 'dejavusans' take what they have no glyph for from (StixFonts, StixSansFonts), as
        // matplotlib ships them in mpl-data/fonts/ttf (the STIX Fonts, SIL Open Font License).
        new("MatPlotLibNet.Skia", @"Fonts\STIXGeneral.ttf", ["STIXGeneral.ttf", "STIX Font License", "SIL OPEN FONT LICENSE", "matplotlib"], "Src/MatPlotLibNet.Skia/Fonts/LICENSE_STIX"),
        new("MatPlotLibNet.Skia", @"Fonts\STIXGeneralItalic.ttf", ["STIXGeneralItalic.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXGeneralBol.ttf", ["STIXGeneralBol.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXNonUni.ttf", ["STIXNonUni.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXNonUniIta.ttf", ["STIXNonUniIta.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXSizOneSymReg.ttf", ["STIXSizOneSymReg.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXSizTwoSymReg.ttf", ["STIXSizTwoSymReg.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXSizThreeSymReg.ttf", ["STIXSizThreeSymReg.ttf"], null),
        new("MatPlotLibNet.Skia", @"Fonts\STIXSizFourSymReg.ttf", ["STIXSizFourSymReg.ttf"], null),
        new("MatPlotLibNet.Interactive", @"Resources\signalr.min.js",
            ["signalr.min.js", "@microsoft/signalr", "8.0.7", "Copyright (c) .NET Foundation and Contributors", "Permission is hereby granted"], null),
    ];

    /// <summary>Embedded files that carry no third-party licence of their own: the Natural Earth data is public domain, and
    /// the Geo README says so where the package page shows it.</summary>
    private static readonly string[] PublicDomain = [@"Data\*.geojson"];

    private static readonly string Root = RepositoryRoot.Folder;

    private static string Normalised(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static XDocument Csproj(string project) =>
        XDocument.Load(Path.Combine(Root, "Src", project, project + ".csproj"));

    private static string[] Embedded(string project) =>
        [.. Csproj(project).Descendants("EmbeddedResource").Select(e => (string)e.Attribute("Include")!)];

    [Fact]
    public void EveryEmbeddedResourceOfAPackage_IsTheLibrarysOwnOrNamedWithItsOrigin()
    {
        var unnamed = new List<string>();
        foreach (var csproj in RepositoryTree.Files("Src", "*.csproj"))
        {
            string project = Path.GetFileNameWithoutExtension(csproj.Path);
            foreach (string include in Embedded(project))
            {
                bool named = ThirdParty.Any(t => t.Project == project && t.Include == include) || PublicDomain.Contains(include);
                if (!named) unnamed.Add($"{project}: {include}");
            }
        }

        Assert.True(unnamed.Count == 0,
            "an embedded file with no declared origin (add it to ThirdParty with the notice it needs, or to PublicDomain): " + string.Join("; ", unnamed));
    }

    [Fact]
    public void ThePublicDomainData_IsSaidToBeSoInTheGeoReadme()
    {
        string readme = File.ReadAllText(Path.Combine(Root, "Src", "MatPlotLibNet.Geo", "README.md"));

        Assert.Contains("Natural Earth", readme);
        Assert.Contains("public domain", readme);
    }

    public static IEnumerable<object[]> ProjectsWithThirdPartyFiles() => ThirdParty.Select(t => t.Project).Distinct().Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(ProjectsWithThirdPartyFiles))]
    public void APackageThatEmbedsThirdPartyFiles_PacksItsNotices(string project)
    {
        Assert.True(File.Exists(Path.Combine(Root, "Src", project, NoticesFile)), $"{project} has no {NoticesFile}");
        var packed = Csproj(project).Descendants("None")
            .Where(n => (string?)n.Attribute("Include") == NoticesFile)
            .ToArray();

        Assert.Single(packed);
        Assert.Equal("true", (string?)packed[0].Attribute("Pack"));
        Assert.Equal("\\", (string?)packed[0].Attribute("PackagePath"));
    }

    public static IEnumerable<object?[]> ThirdPartyResources() =>
        ThirdParty.Select(t => new object?[] { t.Project, string.Join("\u0001", t.NoticeNames), t.LicenseFile });

    [Theory]
    [MemberData(nameof(ThirdPartyResources))]
    public void TheNotices_NameEachThirdPartyFileAndCarryItsLicenceText(string project, string names, string? licenseFile)
    {
        string notices = Normalised(File.ReadAllText(Path.Combine(Root, "Src", project, NoticesFile)));

        foreach (string name in names.Split('\u0001'))
            Assert.Contains(Normalised(name), notices);
        if (licenseFile is not null)
            Assert.Contains(Normalised(File.ReadAllText(Path.Combine(Root, licenseFile))), notices);
    }
}
