// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A server-interactive SVG carries its interaction script inside it, and a browser never runs a
/// <c>&lt;script&gt;</c> that arrives through <c>innerHTML</c> (measured in Edge: on the ASP.NET Core sample page, mounted
/// that way, the script never ran on load or after an update, and no gesture reached the hub). Every page and every
/// document that shows how to put a pushed SVG on a page (it registers a handler for <c>UpdateChartSvg</c>) therefore mounts it with
/// <c>mplMountSvg</c>, which re-creates the script elements, and assigns <c>innerHTML</c> nowhere else.</summary>
public class SvgMountRecipeContractTests
{
    private static readonly string Root = RepoRoot();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static readonly string[] Extensions = [".html", ".razor", ".cshtml", ".js", ".md"];

    /// <summary>Every sample page and every document the repository ships a reader: the samples, the root README, the
    /// package READMEs, the cookbook and articles. Build output and the generated API pages are not authored text.</summary>
    private static IEnumerable<string> AuthoredFiles()
    {
        IEnumerable<string> Under(string relative) => Directory.Exists(Path.Combine(Root, relative))
            ? Directory.EnumerateFiles(Path.Combine(Root, relative), "*", SearchOption.AllDirectories)
            : [];
        return Under("Samples").Concat(Under("docs")).Concat(Under("articles"))
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "Src"), "README.md", SearchOption.AllDirectories))
            .Append(Path.Combine(Root, "README.md"))
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !Regex.IsMatch(f, @"[\\/](bin|obj|node_modules|_site|api)[\\/]"));
    }

    /// <summary>A page that receives a pushed SVG registers a JavaScript handler for it: <c>.on('UpdateChartSvg', ...)</c>.</summary>
    private static readonly Regex HandlerOfAPushedSvg = new(@"\.on\(\s*['""]UpdateChartSvg['""]");

    private static string[] PageFiles() => AuthoredFiles()
        .Where(f => HandlerOfAPushedSvg.IsMatch(File.ReadAllText(f)))
        .Select(f => Path.GetRelativePath(Root, f).Replace('\\', '/'))
        .Order(StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<string> PagesThatMountAPushedSvg() => new(PageFiles());

    [Fact]
    public void TheSamplesAndThePackageReadmes_AreAmongThePagesFound()
    {
        var found = PageFiles();

        Assert.Contains("Samples/MatPlotLibNet.Samples.AspNetCore/wwwroot/index.html", found);
        Assert.Contains("Src/MatPlotLibNet.AspNetCore/README.md", found);
        Assert.Contains("Src/MatPlotLibNet.Blazor/README.md", found);
    }

    [Theory]
    [MemberData(nameof(PagesThatMountAPushedSvg))]
    public void APushedSvg_IsMountedSoThatItsScriptRuns(string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(Root, relativePath));

        int definitions = Regex.Matches(text, @"function\s+mplMountSvg\s*\(").Count;
        int mounts = Regex.Matches(text, @"(?<!function\s)mplMountSvg\s*\(").Count;
        int innerHtmlWrites = Regex.Matches(text, @"\.innerHTML\s*=[^=]").Count;

        Assert.True(definitions == 1, $"{relativePath}: defines mplMountSvg {definitions} times; it shows it once");
        Assert.True(mounts >= 1, $"{relativePath}: never calls mplMountSvg");
        Assert.True(innerHtmlWrites == 1,
            $"{relativePath}: assigns innerHTML {innerHtmlWrites} times; only mplMountSvg may, because a script that arrives through innerHTML never runs");
    }

    /// <summary>A redrawn picture replaces the SVG, and with it the drag or the band the viewer is in the middle of. The chart
    /// says when it is busy (<c>data-mpl-busy</c>) and when it is idle again (<c>mpl:idle</c>), and the recipe waits for it.</summary>
    [Theory]
    [MemberData(nameof(PagesThatMountAPushedSvg))]
    public void APushedSvg_IsNotMountedOverAGestureInProgress(string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(Root, relativePath));

        Assert.Contains("data-mpl-busy", text);
        Assert.Contains("mpl:idle", text);
    }

    /// <summary>The Blazor sample shows the way that needs no recipe: the live chart component mounts the picture, runs its
    /// script, holds a redraw back during a gesture and gives the script the connection its client holds, so the page sets no
    /// connection of its own.</summary>
    [Fact]
    public void TheBlazorSample_UsesTheLiveChartComponent_AndSetsNoConnectionOfItsOwn()
    {
        string text = File.ReadAllText(Path.Combine(Root, "Samples", "MatPlotLibNet.Samples.Blazor", "Components", "Pages", "Interactive.razor"));

        Assert.Contains("<MplLiveChart", text);
        Assert.DoesNotContain("__mpl_signalr_connection", text);
        Assert.DoesNotContain("UpdateChartSvg", text);
    }
}
