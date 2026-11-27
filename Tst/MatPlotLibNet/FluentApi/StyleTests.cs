// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="Plt.Style"/>: a sheet used inside a context scopes the current parameters, and a sheet used outside
/// one writes the process-wide defaults every figure built without a theme wears, so the class runs on its own and puts every
/// default back.</summary>
[Collection("RcParamsGlobalState")]
public class StyleTests
{
    // ── Plt.Style.Context ─────────────────────────────────────────────────────

    [Fact]
    public void Style_Context_BySheet_ScopesCurrentRcParams()
    {
        var sheet = new StyleSheet("test-ctx", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 55.0
        });

        using (Plt.Style.Context(sheet))
        {
            Assert.Equal(55.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
        }

        // Restored after scope
        Assert.NotEqual(55.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
    }

    [Fact]
    public void Style_Context_ByName_ScopesCurrentRcParams()
    {
        using (Plt.Style.Context("seaborn"))
        {
            // The seaborn sheet is the seaborn theme's: its font size, in points.
            Assert.Equal((double)StyleSheet.FromTheme(Theme.Seaborn).Parameters[RcParamKeys.FontSize], RcParams.Current.Get<double>(RcParamKeys.FontSize));
        }
    }

    [Fact]
    public void Style_Context_UnknownName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Plt.Style.Context("no-such-style-abc-999").Dispose());
    }

    // ── Plt.Style.Use ─────────────────────────────────────────────────────────

    [Fact]
    public void Style_Use_UnknownName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Plt.Style.Use("no-such-style-xyz-777"));
    }

    // ── Theme.ToStyleSheet ────────────────────────────────────────────────────

    [Fact]
    public void Theme_ToStyleSheet_ReturnsMappedSheet()
    {
        var sheet = Theme.Default.ToStyleSheet();
        Assert.NotNull(sheet);
        Assert.Equal("default", sheet.Name);
        Assert.True(sheet.Parameters.ContainsKey(RcParamKeys.FontSize));
    }

    [Fact]
    public void Style_Use_BySheet_AppliesGlobally()
    {
        using var restore = new MatPlotLibNet.Tests.Styling.RcParamsDefaultsRestorer();

        var sheet = new StyleSheet("temp-global", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 44.0
        });

        Plt.Style.Use(sheet);
        Assert.Equal(44.0, RcParams.Default.FontSize);
    }

    [Fact]
    public void Style_Use_ByName_AppliesGlobally()
    {
        using var restore = new MatPlotLibNet.Tests.Styling.RcParamsDefaultsRestorer();

        Plt.Style.Use("ggplot");
        // The ggplot sheet is the ggplot theme's: its font size, in points.
        Assert.Equal((double)StyleSheet.FromTheme(Theme.Ggplot).Parameters[RcParamKeys.FontSize], RcParams.Default.FontSize);
    }
}

