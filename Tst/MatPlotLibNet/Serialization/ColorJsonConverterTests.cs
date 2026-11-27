// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;
using System.Text.Json;
namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies <see cref="MatPlotLibNet.Serialization.ColorJsonConverter"/>: a JSON null reads as the default colour.</summary>
public class ColorJsonConverterTests
{

    // `hex is not null ? Color.FromHex(hex) : default` null arm.
    [Fact] public void ColorJsonConverter_ReadNullColor_ReturnsDefault()
    {
        var converter = new MatPlotLibNet.Serialization.ColorJsonConverter();
        var json = "null";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();
        var result = converter.Read(ref reader, typeof(Color), new JsonSerializerOptions());
        Assert.Equal(default, result);
    }
}
