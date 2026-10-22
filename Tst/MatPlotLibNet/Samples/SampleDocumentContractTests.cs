// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Playground;
using MatPlotLibNet.Tests.Rendering.Svg;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// The pictures the cookbook shows and the numbers the sample documents print are kept by hand: the console sample
/// writes <c>images/</c>, the documentation site copies that folder, and a page points at a file by name. A page
/// that names a picture nobody generated publishes a broken image, and a count nobody recounts drifts the day a
/// sample is added. These tests hold both to what is in the repository.
/// </summary>
public class SampleDocumentContractTests
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

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    /// <summary>The names in <c>images/</c> that the generator wrote as both an SVG and a PNG.</summary>
    private static string[] SvgPngPairs()
    {
        var files = Directory.EnumerateFiles(Path.Combine(Root, "images")).Select(Path.GetFileName).ToHashSet();
        return [.. files.Where(f => f!.EndsWith(".svg", StringComparison.Ordinal))
            .Select(f => f![..^4])
            .Where(name => files.Contains(name + ".png"))
            .Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void EveryPictureTheCookbookShows_IsInTheImagesFolder()
    {
        // The cookbook pages point at ../images/<name>, which the documentation site fills from images/ when it is
        // built. A name with no file there is a broken picture on the published page, and no build fails on it.
        var missing = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .SelectMany(page => Regex.Matches(File.ReadAllText(page), @"\]\(\.\./images/(?<name>[^)\s]+)\)")
                .Select(m => m.Groups["name"].Value)
                .Where(name => !File.Exists(Path.Combine(Root, "images", name)))
                .Select(name => $"{Path.GetFileName(page)} -> {name}"))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"The cookbook shows {missing.Length} picture(s) that images/ does not hold: {string.Join("; ", missing)}");
    }

    [Fact]
    public void TheSamplesReadme_CountsTheSvgPngPairsInTheImagesFolder()
    {
        var count = Regex.Match(Read("Samples", "README.md"), @"(?<n>\d+) SVG/PNG pairs");

        Assert.True(count.Success, "Samples/README.md no longer says how many SVG/PNG pairs the console sample writes");
        Assert.Equal(SvgPngPairs().Length, int.Parse(count.Groups["n"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>One picture and the number of internal labels its scripts carry.</summary>
    private readonly record struct PictureFinding(string Picture, int Labels);

    [Fact]
    public void NoPictureInTheImagesFolder_CarriesAnInternalLabelInItsScripts()
    {
        // The interactive pictures are the library's own output, scripts included. A comment in one that names the
        // phase of a plan or a version is on show to everyone who opens the file, so the pictures are regenerated
        // when the scripts change; the scripts themselves are held to the same words in EmbeddedScriptLabelTests.
        var offenders = Directory.EnumerateFiles(Path.Combine(Root, "images"), "*.svg")
            .Select(file => new PictureFinding(Path.GetFileName(file), File.ReadAllText(file).ScriptText().InternalLabels().Count))
            .Where(p => p.Labels > 0)
            .Select(p => $"{p.Picture} ({p.Labels})")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"{offenders.Length} picture(s) in images/ carry internal labels in their scripts: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ThePlaygroundPage_CarriesNoInternalLabelInItsScript()
    {
        // wwwroot/index.html is served as written: its script comments are on show in the browser's source view.
        var hits = Read("Samples", "MatPlotLibNet.Playground", "wwwroot", "index.html").InternalLabels();

        Assert.True(hits.Count == 0,
            $"The Playground page carries {hits.Count} internal label(s): {string.Join(" | ", hits.Select(h => h.Text))}");
    }

    [Fact]
    public void ThePlaygroundPage_ShowsNoInternalLabelInTheTextItRenders()
    {
        // The razor comments and the @code block never reach the browser; what is left is the markup a visitor reads.
        string page = Read("Samples", "MatPlotLibNet.Playground", "Pages", "Playground.razor");
        string rendered = Regex.Replace(page, @"@\*.*?\*@", "", RegexOptions.Singleline);
        rendered = rendered[..rendered.IndexOf("@code", StringComparison.Ordinal)];

        var hits = rendered.InternalLabels();

        Assert.True(hits.Count == 0,
            $"The Playground page shows {hits.Count} internal label(s): {string.Join(" | ", hits.Select(h => h.Text))}");
    }

    [Theory]
    [InlineData("Samples", "README.md")]
    [InlineData("articles", "intro.md")]
    public void ADocumentThatCountsThePlaygroundExamples_SaysHowManyThereAre(string folder, string file)
    {
        var count = Regex.Match(Read(folder, file), @"(?<n>\d+) example charts");

        Assert.True(count.Success, $"{folder}/{file} no longer says how many example charts the Playground has");
        Assert.Equal(PlaygroundExamples.Examples.Count, int.Parse(count.Groups["n"].Value, CultureInfo.InvariantCulture));
    }
}
