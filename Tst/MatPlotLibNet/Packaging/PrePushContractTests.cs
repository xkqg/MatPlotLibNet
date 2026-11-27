// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The site takes its pictures from <c>images/</c> itself: <c>docs/docfx.json</c> names that folder as a resource, and docfx
/// rewrites a cookbook page's link to it. The site was once built from a copy in <c>docs/images/</c>, which git ignores, so a page's
/// link pointed at the copy and was broken on GitHub for every picture; and PowerShell's <c>Copy-Item</c> put the second copy inside
/// the first (<c>docs/images/images</c>), so the site kept the pictures of the first run. Neither the push gate nor the Pages workflow
/// copies the folder any more, and the push gate takes away a copy an older run left, which docfx would otherwise publish twice.</summary>
public class PrePushContractTests
{
    [Fact]
    public void TheSite_TakesItsPicturesFromTheImagesFolder_AndNothingCopiesThem()
    {
        string docfx = File.ReadAllText(Path.Join(RepositoryRoot.Folder, "docs", "docfx.json"));
        string[] prepush = [.. RepositoryTree.FileAt("tools", "prepush.ps1").CodeLines];
        string pages = File.ReadAllText(Path.Join(RepositoryRoot.Folder, ".github", "workflows", "pages.yml"));

        Assert.Contains("{ \"files\": [\"images/**\"], \"src\": \"..\" }", docfx, StringComparison.Ordinal);
        Assert.DoesNotContain(prepush, line => line.Contains("Copy-Item", StringComparison.Ordinal) && line.Contains("images", StringComparison.Ordinal));
        Assert.Contains(prepush, line => line.Contains("Remove-Item", StringComparison.Ordinal) && line.Contains("docs/images", StringComparison.Ordinal));
        Assert.DoesNotContain("docs/images", pages, StringComparison.Ordinal);
    }
}
