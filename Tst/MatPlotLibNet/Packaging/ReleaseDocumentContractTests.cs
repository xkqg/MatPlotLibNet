// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static MatPlotLibNet.Tests.Packaging.ReleaseFiles;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// The documents and pages a release publishes (the cookbook, the home page, the search-engine and accessibility pages, the READMEs, the
/// matplotlib translation page, the API pages) say what the library is and what it ships, and what they count is what the assembly carries.
/// </summary>
public class ReleaseDocumentContractTests
{
    // ---- what a search engine reads ---------------------------------------------------------------------------

    [Fact]
    public void EveryCookbookPage_TellsASearchEngineWhatItIsAndWhatItAnswers()
    {
        // A cookbook page is the page a stranger can actually land on: they search for the thing they want to
        // draw, not for this library's name. Two lines of YAML decide what they see in the result. Without a
        // title docfx falls back to the heading, which is written for a reader who is already here — "Line
        // Charts" — instead of the words that were typed to find it.
        var missing = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Select(file => (Name: Path.GetFileName(file), Head: SourceFile.FrontMatterOf(File.ReadAllText(file))))
            .Where(p => !p.Head.Any(line => line.StartsWith("title:", StringComparison.Ordinal))
                     || !p.Head.Any(line => line.StartsWith("description:", StringComparison.Ordinal)))
            .Select(p => p.Name)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"These cookbook pages carry no title or no description of their own: {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheDocumentationHomePage_SaysWhatThisIsInTheFormATSearchEngineReads()
    {
        // Measured over the built site: 0 of 772 pages carried structured data of any kind, so a search engine
        // had nothing but prose to decide what this project even is — and the name reads as "matplotlib" plus
        // ".net", which is two other projects' names. One JSON-LD block on the page that matters states it.
        // It is raw HTML in the Markdown, which markdig passes straight through; no forked docfx template.
        string home = Read("docs", "index.md");
        var block = Regex.Match(home, @"<script type=""application/ld\+json"">\s*(?<json>\{.*?\})\s*</script>", RegexOptions.Singleline);

        Assert.True(block.Success, "docs/index.md carries no JSON-LD block");

        using var data = JsonDocument.Parse(block.Groups["json"].Value);
        var root = data.RootElement;

        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("SoftwareApplication", root.GetProperty("@type").GetString());
        Assert.Equal("MatPlotLibNet", root.GetProperty("name").GetString());
        Assert.Equal("https://xkqg.github.io/MatPlotLibNet/", root.GetProperty("url").GetString());
        Assert.Equal("https://github.com/xkqg/MatPlotLibNet", root.GetProperty("codeRepository").GetString());
        Assert.Equal("https://www.nuget.org/packages/MatPlotLibNet", root.GetProperty("downloadUrl").GetString());
    }

    [Fact]
    public void TheIndexNowKey_IsPublishedWithTheSiteAndSaysItsOwnName()
    {
        // IndexNow is how a site tells Bing and Yandex that a page changed without holding an account anywhere:
        // the proof of ownership is a file, served from the site itself, whose CONTENT is its own name. This
        // site lives under a project path and the host root answers 404, so the key is submitted with an
        // explicit keyLocation — which means the file has to be reachable at exactly that address. A key docfx
        // does not copy into the site answers 404 there, and every submission made with it is refused.
        var keys = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*.txt")
            .Where(file => Path.GetFileNameWithoutExtension(file) is { Length: 32 } name
                        && name.All(Uri.IsHexDigit))
            .ToArray();

        string key = Assert.Single(keys);
        Assert.Equal(Path.GetFileNameWithoutExtension(key), File.ReadAllText(key).Trim());

        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        var resources = config.RootElement.GetProperty("build").GetProperty("resource")[0].GetProperty("files")
            .EnumerateArray().Select(f => f.GetString()).ToArray();

        Assert.True(resources.Contains("*.txt", StringComparer.Ordinal)
                 || resources.Contains(Path.GetFileName(key), StringComparer.Ordinal),
            "docs/docfx.json does not copy the IndexNow key into the built site");
    }

    [Fact]
    public void TheDocumentationSite_CarriesItsAnalyticsTag()
    {
        // The tag is one key in globalMetadata and the modern template renders it into every page's head, so
        // there is nothing to maintain — but there is something to lose: a key removed or misspelled produces a
        // site that builds, deploys and reports nothing, and an empty report reads exactly like no visitors.
        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        string? tag = config.RootElement.GetProperty("build").GetProperty("globalMetadata")
            .GetProperty("_googleAnalyticsTagId").GetString();

        Assert.Matches("^G-[A-Z0-9]{10}$", tag ?? string.Empty);
    }

    [Fact]
    public void EveryCookbookPage_SaysWhichPackageItNeeds()
    {
        // A cookbook page is where a stranger lands from a search — "candlestick chart in C#" — and every one of
        // the 34 arrived without naming a package or an install line. They are the pages that have to answer
        // "and how do I get this", because the reader has no reason to walk back to the home page for it.
        var silent = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Where(file => !File.ReadAllText(file).Contains("dotnet add package", StringComparison.Ordinal)
                        && !File.ReadAllText(file).Contains("dotnet tool install", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(silent.Length == 0,
            $"These cookbook pages never say what to install: {string.Join(", ", silent)}");
    }

    [Fact]
    public void TheDocumentationHomePage_LinksEveryPackageToWhereItIsInstalledFrom()
    {
        // Someone who arrives at the documentation from a search is there to USE the library, and the package
        // table listed all fourteen names as plain text — the reader had to retype one into nuget.org. Every
        // name is a link now, and a package added later has to be one too, or the table quietly stops being
        // the way out of the site.
        string home = Read("docs", "index.md");

        var missing = PackableProjects()
            .Select(p => p.Xml.Descendants("PackageId").First().Value.Trim())
            .Where(id => !home.Contains($"[`{id}`](https://www.nuget.org/packages/{id})", StringComparison.Ordinal))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"The package table does not link these to nuget.org: {string.Join(", ", missing)}");
    }

    [Fact]
    public void ThePagesWorkflow_StripsTheByteOrderMarkFromTheSitemap()
    {
        // docfx writes the sitemap as UTF-8 WITH a byte-order mark, three bytes in front of the XML
        // declaration, and there is no docfx setting for it — so the workflow that publishes the site takes
        // them off again. Nothing else about the file was wrong when Search Console refused it: 766 urls,
        // every one inside the property, no duplicates, valid priorities and lastmods, served as
        // application/xml over a 200 to a Googlebot user agent.
        string workflow = Read(".github", "workflows", "pages.yml");
        int build = workflow.IndexOf("docfx build", StringComparison.Ordinal);
        int strip = workflow.IndexOf("_site/sitemap.xml", StringComparison.Ordinal);

        Assert.True(strip > build && build >= 0,
            "pages.yml no longer strips the byte-order mark from docs/_site/sitemap.xml after the docfx build");
    }

    [Fact]
    public void TheSearchConsoleVerification_IsPublishedWithTheSiteAndNamesItself()
    {
        // Google verifies ownership of a site by fetching one file it named, whose single line repeats that
        // file's own name. It is fetched again from time to time, not only once: delete the file, or stop
        // copying it into the built site, and the property silently falls back to unverified — with it goes
        // every crawl statistic and the ability to submit a sitemap.
        var files = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "google*.html").ToArray();

        string verification = Assert.Single(files);
        Assert.Equal($"google-site-verification: {Path.GetFileName(verification)}", File.ReadAllText(verification).Trim());

        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        var resources = config.RootElement.GetProperty("build").GetProperty("resource")[0].GetProperty("files")
            .EnumerateArray().Select(f => f.GetString()).ToArray();

        Assert.True(resources.Contains("google*.html", StringComparer.Ordinal)
                 || resources.Contains(Path.GetFileName(verification), StringComparer.Ordinal),
            "docs/docfx.json does not copy the Search Console verification file into the built site");
    }

    // ---- the numbers a package page prints --------------------------------------------------------------------

    /// <summary>Every counted claim a <c>&lt;Description&gt;</c> may carry, beside the code that measures it.</summary>
    private static (string Pattern, int Measured, string What)[] PublishedCounts() =>
    [
        (@"(\d+) colormaps", Styling.ColorMapRegistryTests.DeclaredColorMaps().Length * 2, "colormaps"),
        (@"(\d+) base (?:maps|colormaps)", Styling.ColorMapRegistryTests.DeclaredColorMaps().Length, "base maps"),
        (@"(\d+) (?:series types|chart types)", Models.SeriesCountContractTests.ConcreteSeries().Length, "concrete series"),
        (@"(\d+) themes", ThemeCount(), "themes"),
        // The indicator count is NOT here: it is package-specific. The core assembly ships 58 indicator types and
        // the DataFrame package exposes its own extension surface over them, so one number checked against one
        // assembly would be wrong for the other package. DataFrame pins its own, in its own test project.
    ];

    private static int ThemeCount() =>
        typeof(global::MatPlotLibNet.Styling.Theme)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Count(p => p.PropertyType == typeof(global::MatPlotLibNet.Styling.Theme));

    [Fact]
    public void EveryPackageDescription_CountsWhatTheLibraryActuallyShips()
    {
        // The colormap count is pinned across every *.md in the repository — and the number that reaches the most
        // readers is in no *.md at all. It is <Description>: the text nuget.org prints on the package page and the
        // package manager prints in its search row. The core package advertised 142 colormaps there while the
        // registry held 148, and nothing was red. The same hole as the tags — product text no test reads.
        foreach (var (path, xml) in PackableProjects())
        {
            string description = xml.Descendants("Description").FirstOrDefault()?.Value ?? string.Empty;

            foreach (var (pattern, measured, what) in PublishedCounts())
            {
                foreach (Match m in Regex.Matches(description, pattern))
                {
                    Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == measured,
                        $"{path} says \"{m.Value}\"; the library ships {measured} {what}.");
                }
            }
        }
    }

    /// <summary>Packages the API site cannot document, each with the reason. docfx runs on the Linux Pages
    /// runner, which has no MAUI workload — the metadata step would fail the whole site for one package.</summary>
    private static readonly IReadOnlyDictionary<string, string> NotOnTheApiSite = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Src/MatPlotLibNet.Maui/MatPlotLibNet.Maui.csproj"] = "needs the MAUI workload, which the Pages runner has not got",
    };

    [Fact]
    public void EveryCookbookPage_IsInTheTableOfContents()
    {
        // docfx builds the site from the TOC, so a page the TOC does not list is a 404 behind a link the index
        // already carries. Measured: six pages had reached the index and never the TOC, the newest of them
        // linked from the cookbook's own front page.
        var pages = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Select(Path.GetFileName)
            .Where(name => name != "index.md")
            .OrderBy(name => name, StringComparer.Ordinal);
        string toc = Read("docs", "cookbook", "toc.yml");

        var missing = pages.Where(name => !toc.Contains($"href: {name}", StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0,
            $"These cookbook pages are published but unreachable: {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheMcpReadme_CarriesTheRegistryOwnershipMarker()
    {
        // The official MCP Registry proves ownership by reading the PUBLISHED package's README for this exact
        // line. 1.15.0 shipped without it and the listing was refused until 1.15.1 carried it; nuget.org packages
        // are immutable, so a README edit that loses the line costs a release, not a commit.
        var lines = RepositoryTree.FileAt("Src", "MatPlotLibNet.Mcp", "README.md").Lines.Select(l => l.Trim());

        Assert.Contains("mcp-name: io.github.xkqg/matplotlibnet", lines);
    }

    [Fact]
    public void TheAccessibilityPage_NamesEverySeriesWithNoTabularForm()
    {
        // "Every chart has a data table" is the promise the page makes, and it is not quite true: two series
        // are annotations rather than data. A reader who builds an accessible page on that promise and finds
        // nothing has been misled by a document, not by the library. The exceptions are a hand-kept list in
        // one test file; this pins the page to it, so adding a third one fails here rather than silently
        // widening the gap between what the page says and what the code does.
        string page = Read("docs", "cookbook", "accessibility.md");

        var unnamed = Models.Series.SeriesDataTableTests.WithoutATabularForm
            .Where(name => !page.Contains(name, StringComparison.Ordinal))
            .ToArray();

        Assert.True(unnamed.Length == 0,
            $"The accessibility page promises a table for every chart but never names these exceptions: {string.Join(", ", unnamed)}");
    }

    [Fact]
    public void EveryDocumentThatCountsTheColormaps_CountsWhatTheLibraryActuallyShips()
    {
        // Seven documents printed "142 colormaps" and nothing held them to it: the only check asked for "at least
        // 114", so the number could have been wrong by twenty-eight and stayed green. The registry is the source
        // of the number, and these are the documents that repeat it.
        int registered = Styling.ColorMapRegistryTests.DeclaredColorMaps().Length * 2;
        int baseMaps = registered / 2;

        foreach (var (path, text) in RepositoryTree.CurrentStateDocuments())
        {
            foreach (Match m in Regex.Matches(text, @"(\d+) colormaps"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == registered,
                    $"{path} says \"{m.Value}\"; the library registers {registered}.");
            }

            foreach (Match m in Regex.Matches(text, @"(\d+) base (?:maps|colormaps)"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == baseMaps,
                    $"{path} says \"{m.Value}\"; the library declares {baseMaps}.");
            }

            // "viridis, plasma, turbo, coolwarm and N more" — four named, the rest counted.
            foreach (Match m in Regex.Matches(text, @"coolwarm,? and (\d+) more"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == registered - 4,
                    $"{path} names four colormaps and says \"{m.Value}\"; there are {registered - 4} others.");
            }
        }
    }

    [Fact]
    public void TheApiDocumentation_CoversEveryPackableProject()
    {
        // docfx metadata is a hand-kept file list too: a package absent from it is absent from the API site.
        string docfx = Read("docs", "docfx.json");
        var missing = PackableProjects()
            .Select(p => p.Path)
            .Where(path => !docfx.Contains(path, StringComparison.Ordinal) && !NotOnTheApiSite.ContainsKey(path))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"These packages would be missing from the published API reference: {string.Join(", ", missing)}");
    }

    // ---- what reaches a search engine at all -------------------------------------------------------------------

    /// <summary>The address the site is served at, as the sitemap is built with it. One source, so a tool that
    /// writes addresses into pages cannot disagree with the sitemap that lists them.</summary>
    private static string SiteBaseUrl()
    {
        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        return config.RootElement.GetProperty("build").GetProperty("sitemap").GetProperty("baseUrl").GetString()!;
    }

    /// <summary>A step of the pages workflow is worth nothing unless the workflow names it and the script it runs is in the repository.</summary>
    private static void AssertThePagesWorkflowRuns(string workflow, string script, params string[] needles)
    {
        foreach (var needle in needles) Assert.Contains(needle, workflow, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Root, "tools", "seo", script)), $"the workflow runs tools/seo/{script} and it is not in the repository");
    }

    [Fact]
    public void ThePagesWorkflow_GivesEveryBuiltPageOneAddressAndAPreviewCard()
    {
        // Measured on the live site: 0 of 766 pages carried a canonical address or a link-preview tag, and
        // neither can be switched on from docfx's own configuration — _canonicalUrlPrefix emits nothing in
        // 2.78.5 and an unknown frontmatter key is dropped in silence. So the site is finished after the build
        // instead. The step is worth nothing unless it runs and is checked, so this pins all three: the tool's
        // own cases run first, it is applied, and the result is verified before anything is uploaded.
        string workflow = Read(".github", "workflows", "pages.yml");

        AssertThePagesWorkflowRuns(workflow, "page-metadata.py",
            "tools/seo/page-metadata.py --self-test", "tools/seo/page-metadata.py --site docs/_site", "--apply", "--verify");

        Assert.Contains($"--base-url {SiteBaseUrl()}", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePagesWorkflow_TellsIndexNowWhichPagesChanged()
    {
        // A key file nobody ever submits a URL with tells nobody anything, and that is exactly what was here:
        // the key was published, verified by its own test, and never used. This pins the other half. It has to
        // run after the deployment, because a crawler asked to fetch a page that is not live yet reads the old
        // one, and it has to derive the key from the published file rather than repeat it, because a key in two
        // places drifts and every submission made with the stale one is refused.
        string workflow = Read(".github", "workflows", "pages.yml");

        AssertThePagesWorkflowRuns(workflow, "indexnow.py", "tools/seo/indexnow.py --self-test", "tools/seo/indexnow.py", "--key-dir docs", "--submit");

        int deployed = workflow.IndexOf("actions/deploy-pages", StringComparison.Ordinal);
        int submitted = workflow.IndexOf("tools/seo/indexnow.py --base-url", StringComparison.Ordinal);
        Assert.True(deployed >= 0 && submitted > deployed,
            "IndexNow is told about the pages before they are deployed, so a crawler would read the old ones");

        var key = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*.txt")
            .Select(Path.GetFileNameWithoutExtension)
            .Single(name => name is { Length: 32 } && name.All(Uri.IsHexDigit));

        Assert.DoesNotContain(key!, workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSocialCard_IsTheSizeALinkPreviewAsksFor()
    {
        // Every page points its preview at one image. A link posted in a chat, a forum or an issue is rendered
        // from it, and a preview that is too small is dropped and the link shows as bare text. 1200x630 is the
        // size every renderer accepts as the wide card; anything under 600x315 is refused outright.
        byte[] png = File.ReadAllBytes(Path.Combine(Root, "images", "social-card.png"));

        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], png[..8]);

        int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

        Assert.Equal(1200, width);
        Assert.Equal(630, height);
        Assert.True(png.Length < 5 * 1024 * 1024, $"the card is {png.Length} bytes; a preview is capped at 5 MB");
    }

    [Fact]
    public void TheMatplotlibTranslationPage_TranslatesTwinx_ToTheContainerVerb()
    {
        // The page is where a matplotlib user looks for the C# of what they know, and twinx is the row that once taught
        // the old right-axis verb. The removed names themselves are held out of every document by DeletedNameTests.
        string page = Read("docs", "cookbook", "matplotlib-to-csharp.md");

        var twinx = SourceFile.LinesOf(page).Select(SourceFile.TableCellsOf)
            .Single(cells => cells.Count >= 2 && cells[0].Contains("ax.twinx()", StringComparison.Ordinal));
        Assert.Contains("WithAddRightYAxis", twinx[1], StringComparison.Ordinal);
    }

    [Fact]
    public void TheWpfReadme_SaysWhichWindowsPlatformVersionAProjectMustTarget()
    {
        // MatPlotLibNet.Wpf targets net8.0-windows10.0.19041.0 because SkiaSharp.Views.WPF ships no asset below
        // that platform version. A project on a bare netX.0-windows (platform version 7.0) cannot restore the
        // package (NU1202), and issue 21 had to ask what the floor was and who imposed it. The csproj is where
        // the floor is set; the README is where a consumer looks first, so it must quote the csproj's own value.
        string target = XDocument.Load(Path.Combine(Root, "Src", "MatPlotLibNet.Wpf", "MatPlotLibNet.Wpf.csproj"))
            .Descendants("TargetFramework").Single().Value.Trim();
        string platformVersion = Regex.Match(target, @"windows(?<v>[\d.]+)$").Groups["v"].Value;
        string readme = Read("Src", "MatPlotLibNet.Wpf", "README.md");

        Assert.NotEqual(string.Empty, platformVersion);
        Assert.Contains(platformVersion, readme, StringComparison.Ordinal);
        Assert.Contains("SkiaSharp.Views.WPF", readme, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Dpi")]
    [InlineData("Title")]
    [InlineData("Author")]
    public void TheSkiaReadme_NamesEachSaveSettingThePngAndPdfExportsRead(string setting)
    {
        // PngTransform reads Dpi, Title and Author of the SaveOptions it is handed, PdfTransform reads Title and Author.
        // The README ships inside the package, and it is where a consumer of the PNG and PDF export looks for what a
        // save can set; a setting the export reads and the README does not name is one nobody finds.
        Assert.NotNull(typeof(SaveOptions).GetProperty(setting));
        Assert.Contains($"{setting} = ", Read("Src", "MatPlotLibNet.Skia", "README.md"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheMcpReadme_SaysASpecsDpiSizesThePngInsideTheCanvasCeiling()
    {
        // The spec's dpi sizes the PNG, so the 4,000 by 4,000 pixel ceiling holds for the picture it draws. The README
        // ships inside the package; the tool descriptions say it to a model, this says it to the person who installs it.
        string readme = Read("Src", "MatPlotLibNet.Mcp", "README.md");

        Assert.Contains("`dpi`", readme, StringComparison.Ordinal);
        Assert.Contains("ceiling", readme, StringComparison.Ordinal);
        Assert.Contains("PNG", readme[readme.IndexOf("`dpi`", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }
}
