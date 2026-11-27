// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary><see cref="Plt.Style.Use(StyleSheet)"/> writes the process-wide defaults every build reads, so the tests that
/// call it run on their own and put every default back.</summary>
[Collection("RcParamsGlobalState")]
public class StyleToThemeGlobalTests
{
    [Fact]
    public void AStyleUsedGlobally_DressesEveryFigureBuiltAfterIt()
    {
        using var restore = new RcParamsDefaultsRestorer();

        Plt.Style.Use(StyleSheet.Ggplot);
        var figure = Plt.Create().Plot([1.0, 2.0], [1.0, 2.0]).Build();

        Assert.Equal(Color.FromHex("#E5E5E5"), figure.Theme.AxesBackground);
    }
}
