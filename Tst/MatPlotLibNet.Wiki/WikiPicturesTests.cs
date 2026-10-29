// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The wiki shows pictures, and a picture that was drawn by an older renderer shows a library that no longer exists. The
/// repository draws every picture the wiki shows (the sample program writes them to its <c>images</c> folder), and the
/// wiki keeps a copy of each: this holds the two folders to one another, so a renderer change that redraws a picture
/// fails here until the wiki carries the new one.
/// </summary>
public sealed partial class WikiPicturesTests
{
    private static string WikiImages => Path.Join(Wiki.BesideTheRepository.Folder, "images");

    private static string RepositoryImages => Path.Join(Repository.Root, "images");

    [GeneratedRegex(@"\(images/(?<name>[^)\s#]+)\)")]
    private static partial Regex ImageLink();

    [Fact]
    public void EveryPictureTheWikiKeeps_IsTheOneTheRepositoryDraws()
    {
        Assert.True(Directory.Exists(WikiImages), $"The wiki has no images folder at {WikiImages}. Clone it beside the repository: {Wiki.CloneCommand}");

        string[] kept = [.. Directory.GetFiles(WikiImages).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal)];
        string[] missing = [.. kept.Where(name => !File.Exists(Path.Join(RepositoryImages, name)))];
        string[] stale = [.. kept.Except(missing).Where(name => !Same(Path.Join(WikiImages, name), Path.Join(RepositoryImages, name)))];

        Assert.True(missing.Length == 0, "The wiki keeps pictures the repository's images folder does not hold: " + string.Join(", ", missing)
            + ". Draw each in the sample program (Samples/MatPlotLibNet.Samples.Console), so a renderer change redraws it.");
        Assert.True(stale.Length == 0, "The wiki keeps an older picture than the repository draws: " + string.Join(", ", stale)
            + ". Copy the repository's picture over it.");
    }

    [Fact]
    public void EveryPictureAPageShows_IsInTheWikisImagesFolder()
    {
        string[] absent =
        [
            .. Wiki.BesideTheRepository.Pages()
                .SelectMany(page => ImageLink().Matches(Wiki.BesideTheRepository.Text(page.Name))
                    .Select(link => link.Groups["name"].Value)
                    .Where(image => !File.Exists(Path.Join(WikiImages, image)))
                    .Select(image => $"{page.Name} -> images/{image}"))
                .Distinct(),
        ];

        Assert.True(absent.Length == 0, "A page shows a picture the wiki's images folder does not hold: " + string.Join("; ", absent));
    }

    // A picture is the same when its bytes are; a vector picture is text, so the line ending a checkout chose does not count.
    private static bool Same(string wiki, string repository) =>
        Path.GetExtension(wiki) is ".svg"
            ? File.ReadAllText(wiki).ReplaceLineEndings("\n") == File.ReadAllText(repository).ReplaceLineEndings("\n")
            : File.ReadAllBytes(wiki).AsSpan().SequenceEqual(File.ReadAllBytes(repository));
}
