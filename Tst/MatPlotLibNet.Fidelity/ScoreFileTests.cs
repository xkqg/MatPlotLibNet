// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Fidelity;

/// <summary>The score file (<c>MPL_FIDELITY_SCORES</c>) and the gallery of rendered pictures (<c>MPL_FIDELITY_DUMP</c>) hold the
/// records of the real rows and nothing else. The
/// blank-page rows of <see cref="FidelityLimitsBindTests"/> compare an empty figure with the picture of a real row ("bar",
/// "line") to prove a limit binds; written to the file they stood under the key of that real row, with a blank page's score and
/// the bind test's own limits, so a script that tightens limits from the file read 255 / 0 / 70 and a blank page as the row's
/// own.</summary>
public sealed class ScoreFileTests
{
    /// <summary>A row of the suite that records its score in <see cref="ScoresPath"/>, and its picture in <see cref="DumpFolder"/>,
    /// as every real row does.</summary>
    private sealed class RecordingRow(string path) : FidelityTest
    {
        internal override string? ScoresPath => path;
        internal override string? DumpFolder => path + ".renders";

        [FidelityTolerance(Rms = 255, Ssim = 0, DeltaE = 200)]
        public void Compare() =>
            AssertFidelity(FidelityFigure(Theme.MatplotlibV2).Build(), "bar");
    }

    /// <summary>A row that says its records are not the suite's, as the bind tests do.</summary>
    private sealed class SilentRow(string path) : FidelityTest
    {
        internal override string? ScoresPath => path;
        internal override string? DumpFolder => path + ".renders";
        internal override bool RecordsItsRows => false;

        [FidelityTolerance(Rms = 255, Ssim = 0, DeltaE = 200)]
        public void Compare() =>
            AssertFidelity(FidelityFigure(Theme.MatplotlibV2).Build(), "bar");
    }

    private static string NewScoreFile() => Path.Combine(Path.GetTempPath(), $"mpl-scores-{Guid.NewGuid():N}.tsv");

    /// <summary>Deletes the score file of a row and the folder its pictures were dumped to.</summary>
    private static void Delete(string path)
    {
        File.Delete(path);
        if (Directory.Exists(path + ".renders")) Directory.Delete(path + ".renders", recursive: true);
    }

    [Fact]
    public void ARealRow_WritesItsScoreUnderItsOwnKey()
    {
        string path = NewScoreFile();
        try
        {
            new RecordingRow(path).Compare();

            Assert.StartsWith("v2/bar\t", Assert.Single(SourceFile.ReadFrom(path).Lines, line => line.Length > 0));
        }
        finally { Delete(path); }
    }

    [Fact]
    public void ARowThatRecordsNoScores_WritesNothing()
    {
        string path = NewScoreFile();
        try
        {
            new SilentRow(path).Compare();

            Assert.False(File.Exists(path));
        }
        finally { Delete(path); }
    }

    /// <summary>The rendered picture (<c>MPL_FIDELITY_DUMP</c>) is a record of the row as its score is: a real row writes it under
    /// its own key, a row that records nothing writes none. Written, the blank page of a bind test stood in the gallery under the
    /// key of the real row it compares with, and raced that row (another class, run beside it) for the one file, which failed
    /// whichever of the two was second to open it.</summary>
    [Fact]
    public void ARealRow_DumpsItsPictureUnderItsOwnKey_AndARowThatRecordsNothing_DumpsNone()
    {
        string real = NewScoreFile(), silent = NewScoreFile();
        try
        {
            new RecordingRow(real).Compare();
            new SilentRow(silent).Compare();

            Assert.True(File.Exists(Path.Combine(real + ".renders", "v2", "bar.png")));
            Assert.False(Directory.Exists(silent + ".renders"));
        }
        finally
        {
            Delete(real);
            Delete(silent);
        }
    }

    [Fact]
    public void TheBlankPageRowsOfTheLimitBindTests_RecordNothing()
    {
        Assert.False(new FidelityLimitsBindTests().RecordsItsRows);
    }
}
