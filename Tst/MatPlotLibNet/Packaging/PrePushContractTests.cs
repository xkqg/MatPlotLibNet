// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The site is built from a copy of <c>images/</c> that lives in <c>docs/images/</c>, which git ignores. PowerShell's
/// <c>Copy-Item -Recurse images docs/images</c> puts the folder INSIDE <c>docs/images</c> when that exists, so the second run
/// of the push gate copied into <c>docs/images/images</c> and the pictures the site shows stayed those of the first run (179 of
/// the 180 pictures the two folders share had differed). The push gate removes the previous copy before it copies, as a fresh
/// checkout, which the Pages workflow builds from, has none.</summary>
public class PrePushContractTests
{
    [Fact]
    public void ThePicturesCopiedForTheSite_ReplaceThePreviousCopy()
    {
        string[] lines = [.. RepositoryTree.FileAt("tools", "prepush.ps1").CodeLines];
        int copy = Array.FindIndex(lines, line => line.Contains("Copy-Item", StringComparison.Ordinal) && line.Contains("docs/images", StringComparison.Ordinal));
        int remove = Array.FindIndex(lines, line => line.Contains("Remove-Item", StringComparison.Ordinal) && line.Contains("docs/images", StringComparison.Ordinal));

        Assert.True(copy >= 0, "prepush.ps1 no longer copies images/ to docs/images");
        Assert.True(remove >= 0 && remove < copy, "prepush.ps1 must remove docs/images before it copies images/ there, or the copy lands in docs/images/images");
    }
}
