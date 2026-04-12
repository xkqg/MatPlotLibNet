// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="Plt.Style"/> and rcParams integration end-to-end.</summary>
public class RcParamsIntegrationTests
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
            // Seaborn sets FontSize=11
            Assert.Equal(11.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
        }
    }

    [Fact]
    public void Style_Context_UnknownName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Plt.Style.Context("no-such-style-abc-999").Dispose());
    }

    // ── Plt.Style.Use ─────────────────────────────────────────────────────────

    [Fact]
    public void Style_Use_BySheet_AppliesGlobally()
    {
        // Save state to restore after test (don't leave global state dirty)
        double originalFontSize = RcParams.Default.FontSize;

        var sheet = new StyleSheet("temp-global", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 44.0
        });

        try
        {
            Plt.Style.Use(sheet);
            Assert.Equal(44.0, RcParams.Default.FontSize);
        }
        finally
        {
            // Restore to original
            RcParams.Default.Set(RcParamKeys.FontSize, originalFontSize);
        }
    }

    [Fact]
    public void Style_Use_ByName_AppliesGlobally()
    {
        double originalFontSize = RcParams.Default.FontSize;

        try
        {
            Plt.Style.Use("ggplot");
            // ggplot sets FontSize=10
            Assert.Equal(10.0, RcParams.Default.FontSize);
        }
        finally
        {
            RcParams.Default.Set(RcParamKeys.FontSize, originalFontSize);
        }
    }

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
}
