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
namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="MatPlotLibNet.Indicators.SignalResult"/>: it is made from an array, read by index and length, and read back as an array.</summary>
public class SignalResultTests
{

    // SignalResult: exercise both implicit conversions + indexer + Length property.
    [Fact] public void SignalResult_AllPublicSurface_Exercised()
    {
        SignalResult fromArray = new[] { 1.0, 2.0, 3.0 };
        Assert.Equal(3, fromArray.Length);
        Assert.Equal(2.0, fromArray[1]);
        double[] toArray = fromArray;
        Assert.Equal([1.0, 2.0, 3.0], toArray);
    }
}
