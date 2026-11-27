// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Fidelity;

/// <summary>What a reference picture has to be, written down where a change to the generator or to a check meets it. A score
/// compares two pictures; it measures the renderer only while the two draw the same thing. The treemap reference of an earlier
/// run was seven bars because the package that draws a treemap was missing and the generator drew bars instead, and every score
/// on that row compared two unrelated pictures. The checks, in turn, drew numbers from <see cref="Random"/> where the generator
/// drew them from numpy, so the point clouds differed and the scores moved with them.</summary>
public partial class ReferenceContractTests
{
    private static readonly string Root = RepositoryRoot.Folder;

    /// <summary>A package the generator draws a chart with, set to <c>None</c> when the import fails, is a substitute waiting to
    /// happen: the chart then falls back to a picture of another library.</summary>
    [GeneratedRegex(@"^\s*(\w+)\s*=\s*None\s*$", RegexOptions.Multiline)]
    private static partial Regex ImportFallback();

    [Fact]
    public void TheGenerator_StopsForAMissingPackage_InsteadOfSettingItToNone()
    {
        string generator = File.ReadAllText(Path.Combine(Root, "tools", "mpl_reference", "generate.py"));

        var fallbacks = ImportFallback().Matches(generator).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.True(fallbacks.Length == 0,
            "tools/mpl_reference/generate.py sets " + string.Join(", ", fallbacks) + " to None when its import fails; a chart then " +
            "draws a substitute (the treemap was bars). A missing package must stop the run: use require(...) in the chart.");
    }

    [Fact]
    public void TheGenerator_DrawsNoSubstitutePicture_WhenAPackageIsMissing()
    {
        string generator = File.ReadAllText(Path.Combine(Root, "tools", "mpl_reference", "generate.py"));

        Assert.DoesNotContain("Fallback:", generator, StringComparison.Ordinal);
        Assert.DoesNotContain("is not None:\n            sns.", generator, StringComparison.Ordinal);
        Assert.DoesNotContain("if sns is not None", generator, StringComparison.Ordinal);
        Assert.DoesNotContain("if squarify is not None", generator, StringComparison.Ordinal);
    }

    private static IEnumerable<string> CheckFiles() =>
        Directory.EnumerateFiles(Path.Combine(Root, "Tst", "MatPlotLibNet.Fidelity", "Charts"), "*.cs");

    /// <summary>A check whose numbers come from <see cref="Random"/> draws other points than the generator's numpy does, however
    /// the two are seeded: the data comes from <see cref="ReferenceData"/>, which reads what the generator wrote.</summary>
    [Fact]
    public void NoCheck_DrawsItsNumbersFromRandom()
    {
        var offenders = CheckFiles()
            .Where(file => File.ReadAllText(file).Contains("new Random(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Order()
            .ToArray();

        Assert.True(offenders.Length == 0,
            "These checks draw numbers from System.Random where the reference was drawn from numpy's default_rng; read them with " +
            "ReferenceData.Load(chart) instead: " + string.Join(", ", offenders));
    }

    [GeneratedRegex(@"-?\d+\.\d{6}")]
    private static partial Regex SixDecimals();

    /// <summary>The numbers of a reference pasted into a check are a second copy that nothing keeps in step with the generator.</summary>
    [Fact]
    public void NoCheck_PastesTheNumbersOfAReference()
    {
        var offenders = CheckFiles()
            .Where(file => SixDecimals().Matches(File.ReadAllText(file)).Count > 5)
            .Select(Path.GetFileName)
            .Order()
            .ToArray();

        Assert.True(offenders.Length == 0,
            "These checks carry numbers rounded to six decimals, the shape of a reference's data pasted in; read them with " +
            "ReferenceData.Load(chart) instead: " + string.Join(", ", offenders));
    }
}
