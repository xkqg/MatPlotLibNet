// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Fidelity;

/// <summary>The limits a fidelity row states are the limits it is held to, under both styles. A row of the v2 style used to be
/// held to 1.7 times its stated colour distance, 1.5 times its stated RMS and 0.10 under its stated SSIM, so a row stated at a
/// colour distance of 70 passed at 108.93 and no limit read in a test said what the test held. A blank page against the v2 bar
/// picture is 100.00 away in the colour distance (the distance a missing dominant colour counts), which a limit of 70 refuses and
/// a limit of 100 allows.</summary>
public sealed class FidelityLimitsBindTests : FidelityTest
{
    /// <inheritdoc />
    /// <remarks>Its rows compare a blank page with the picture of a real row, so their scores and their pictures are no row's.</remarks>
    internal override bool RecordsItsRows => false;

    private static Figure Blank(Theme theme) => FidelityFigure(theme).Build();

    [FidelityTolerance(Rms = 255, Ssim = 0, DeltaE = 70)]
    private void DeltaEAtSeventy() { }

    [FidelityTolerance(Rms = 255, Ssim = 0, DeltaE = 105)]
    private void DeltaEAtOneHundredAndFive() { }

    [FidelityTolerance(Rms = 255, Ssim = 0, DeltaE = 70, V2DeltaE = 105)]
    private void DeltaEAtSeventyAndAtOneHundredAndFiveUnderV2() { }

    [FidelityTolerance(Rms = 255, Ssim = 0.95, DeltaE = 200, V2Ssim = 0.60)]
    private void SsimAtNinetyFiveAndAtSixtyUnderV2() { }

    [FidelityTolerance(Rms = 40, Ssim = 0, DeltaE = 200, V2Rms = 90)]
    private void RmsAtFortyAndAtNinetyUnderV2() { }

    [FidelityTolerance(Rms = 70, Ssim = 0, DeltaE = 200)]
    private void RmsAtSeventy() { }

    [FidelityTolerance(Rms = 255, Ssim = 0.75, DeltaE = 200)]
    private void SsimAtSeventyFive() { }

    [Theory]
    [InlineData("v2", "bar")]
    [InlineData("classic", "line")]
    public void ARow_OverItsStatedColourDistance_Fails(string themeId, string chart)
    {
        Assert.ThrowsAny<Exception>(() => AssertFidelity(Blank(themeId.AsTheme()), chart, nameof(DeltaEAtSeventy)));
    }

    [Fact]
    public void AV2Row_WithinItsStatedColourDistance_Passes()
    {
        AssertFidelity(Blank(Theme.MatplotlibV2), "bar", nameof(DeltaEAtOneHundredAndFive));
    }

    /// <summary>A row whose v2 picture differs by design states its own v2 limit (colour distance, SSIM or RMS); the classic row of the same
    /// test keeps the base one. The blank page is 100.00 from the v2 bar picture and from the classic line picture in colour distance,
    /// 0.6986 and 0.6901 from them in SSIM, 86.05 and 52.20 in RMS.</summary>
    [Theory]
    [InlineData(nameof(DeltaEAtSeventyAndAtOneHundredAndFiveUnderV2))]
    [InlineData(nameof(SsimAtNinetyFiveAndAtSixtyUnderV2))]
    [InlineData(nameof(RmsAtFortyAndAtNinetyUnderV2))]
    public void AV2Limit_AppliesToV2Only(string row)
    {
        AssertFidelity(Blank(Theme.MatplotlibV2), "bar", row);
        Assert.ThrowsAny<Exception>(() => AssertFidelity(Blank(Theme.MatplotlibClassic), "line", row));
    }

    /// <summary>The blank page is 86.05 from the v2 bar picture in RMS (1.5 times 70 would be 105) and 0.6986 from it in SSIM (0.10 under
    /// 0.75 would be 0.65).</summary>
    [Theory]
    [InlineData(nameof(RmsAtSeventy))]
    [InlineData(nameof(SsimAtSeventyFive))]
    public void AV2Row_OverItsStatedRmsOrUnderItsStatedSsim_Fails(string row)
    {
        Assert.ThrowsAny<Exception>(() => AssertFidelity(Blank(Theme.MatplotlibV2), "bar", row));
    }

}
