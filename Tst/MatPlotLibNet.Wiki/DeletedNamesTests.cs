// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>A name the library deleted (<see cref="DeletedName"/>) is named on no page of the wiki: a page that still does describes
/// a mechanism that is gone. The core suite reads the code, the tests and the repository's documents.</summary>
public class DeletedNamesTests
{
    /// <summary>One case for the whole list, which names every mention it finds.</summary>
    [Fact]
    public void NoDeletedName_IsNamedOnAPageOfTheWiki()
    {
        var wiki = Wiki.BesideTheRepository;
        SourceFile[] pages = [.. wiki.Pages().Select(page => new SourceFile(page.Name, wiki.Text(page.Name)))];

        var findings = DeletedName.All.SelectMany(deleted => pages.SelectMany(deleted.MentionsIn)).ToList();

        Assert.True(findings.Count == 0, $"{findings.Count} mentions of a deleted name:\n" + string.Join('\n', findings));
    }

    /// <summary>The package-validation suppression files record what the release took out of the packages the release before published.
    /// A removed name the packages no longer have anywhere is a published deleted name, so every document is held to it: the list of
    /// <see cref="DeletedName"/> cannot miss a removal the build recorded.</summary>
    [Fact]
    public void EveryNameThePackagesRemovedSinceTheReleaseBefore_IsAPublishedDeletedName()
    {
        var published = DeletedName.All.Where(deleted => deleted.Published).Select(deleted => deleted.Name[(deleted.Name.LastIndexOf('.') + 1)..]).ToHashSet(StringComparer.Ordinal);
        var unlisted = RemovedSinceBaseline.Names.Where(name => !published.Contains(name)).Order(StringComparer.Ordinal).ToList();

        Assert.True(RemovedSinceBaseline.Names.Count >= FewestRemoved, $"the suppression files give {RemovedSinceBaseline.Names.Count} removed names, fewer than the {FewestRemoved} measured");
        Assert.True(unlisted.Count == 0, $"{unlisted.Count} names the packages removed that DeletedName.All does not list as published: " + string.Join(", ", unlisted));
    }

    /// <summary>The change log's Removed list of this release is what a caller upgrading reads: every name the suppression files remove
    /// and the packages no longer have stands in it, with what replaces it.</summary>
    [Fact]
    public void EveryNameThePackagesRemovedSinceTheReleaseBefore_IsListedUnderRemovedInTheChangeLog()
    {
        var released = SourceFile.SectionOf(File.ReadAllText(Path.Join(RepositoryRoot.Folder, "CHANGELOG.md")), $"[{Repository.PackageVersion}]");

        var unlisted = UnlistedIn(SourceFile.SubsectionOf(released, "Removed"), RemovedSinceBaseline.Names);

        Assert.True(unlisted.Count == 0, $"{unlisted.Count} names the packages removed that the change log's Removed list does not name: " + string.Join(", ", unlisted));
    }

    [Fact]
    public void ANameTheRemovedListDoesNotHoldInCode_IsUnlisted()
    {
        const string removed = "### Removed\n- `Gadget.OldOne()` -> `Gadget.New`.\n- OldTwo -> nothing.";

        Assert.Equal(["OldTwo", "OldThree"], UnlistedIn(removed, ["OldOne", "OldTwo", "OldThree"]));
    }

    // The names no code span of the list holds as a whole name.
    private static List<string> UnlistedIn(string removed, IEnumerable<string> names)
    {
        var listed = SourceFile.CodeSpansOf(removed).SelectMany(span => System.Text.RegularExpressions.Regex.Matches(span.Code, @"\w+").Select(word => word.Value)).ToHashSet(StringComparer.Ordinal);
        return [.. names.Where(name => !listed.Contains(name))];
    }

    /// <summary>How many names the suppression files remove that the packages no longer have, at least: a reader that finds fewer reads
    /// the files from the wrong place. Measured, not chosen.</summary>
    private const int FewestRemoved = 25;
}
