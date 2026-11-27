// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies the Okabe-Ito colormap and what only the ColorBlindSafe theme has, its Okabe-Ito cycle (what every theme states
/// is a row of <c>ThemeTests.Facts</c>).</summary>
public class ColorBlindSafeTests
{
    [Fact]
    public void OkabeIto_ColorMap_HasEightColors()
    {
        var map = QualitativeColorMaps.OkabeIto;
        // ListedColorMap with 8 entries: sample at 8 equally spaced positions
        var colors = new HashSet<Color>();
        for (int i = 0; i < 8; i++)
            colors.Add(map.GetColor((i + 0.5) / 8.0));
        Assert.Equal(8, colors.Count);
    }

    [Fact]
    public void OkabeIto_ColorMap_RegisteredByName()
    {
        var map = ColorMapRegistry.Get("okabe_ito");
        Assert.NotNull(map);
    }

    [Fact]
    public void OkabeIto_ReversedVariant_Registered()
    {
        var map = ColorMapRegistry.Get("okabe_ito_r");
        Assert.NotNull(map);
    }

    [Fact]
    public void OkabeIto_FirstColor_IsOrange()
    {
        var map = QualitativeColorMaps.OkabeIto;
        var first = map.GetColor(0.0625); // midpoint of first slot (1/16)
        Assert.Equal(Color.FromHex("#E69F00"), first);
    }

    [Fact]
    public void OkabeIto_LastColor_IsBlack()
    {
        var map = QualitativeColorMaps.OkabeIto;
        var last = map.GetColor(0.9375); // midpoint of last slot (15/16)
        Assert.Equal(Color.FromHex("#000000"), last);
    }

    /// <summary>The theme cycles the Okabe-Ito palette in the map's order, read from the one list that writes it: orange first, black last.</summary>
    [Fact]
    public void ColorBlindSafeTheme_CyclesTheOkabeItoTable_TheMapsOwn()
    {
        Color[] table = QualitativeColorMaps.OkabeItoCycle();

        Assert.Equal(table, Theme.ColorBlindSafe.CycleColors);
        Assert.Equal(table, Enumerable.Range(0, 8).Select(i => QualitativeColorMaps.OkabeIto.GetColor((i + 0.5) / 8.0)));
        Assert.NotSame(QualitativeColorMaps.OkabeItoCycle(), QualitativeColorMaps.OkabeItoCycle());
    }

    /// <summary>The alarm palette's warning and critical are the Okabe-Ito amber and vermillion, read from the same list; the dim-room ground
    /// keeps them as they are and changes only its resting shades.</summary>
    [Fact]
    public void TheAlarmPalette_WarnsAndAlarmsInTheOkabeItoAmberAndVermillion()
    {
        Assert.Equal(QualitativeColorMaps.OkabeItoAt(0), AlarmPalette.Default.Warning);
        Assert.Equal(QualitativeColorMaps.OkabeItoAt(5), AlarmPalette.Default.Critical);
        Assert.Equal(Color.FromHex("#D55E00"), AlarmPalette.Default.Critical);
        Assert.Equal(AlarmPalette.Default.Warning, Theme.OpsNight.Alarm.Warning);
        Assert.Equal(AlarmPalette.Default.Critical, Theme.OpsNight.Alarm.Critical);
    }
}
