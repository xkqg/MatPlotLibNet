// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.Models;
using Xunit;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// <see cref="ISeriesVisitor"/> gives every <c>Visit</c> overload a no-op default body, for ISP compatibility —
/// a concrete visitor only needs to override the series types it actually draws. <see cref="SvgSeriesRenderer"/>
/// is the ONLY production implementor in <c>Src/</c>, so a series type it does not override renders nothing: its
/// data range still folds into the axes (every series computes that itself), so the axes scale to data no reader
/// can see, and nothing says so. Measured once already (<c>StreamingLineSeries</c>/<c>StreamingScatterSeries</c>/
/// <c>StreamingSignalSeries</c>, 2026-08-22) and missed again when <c>StreamingCandlestickSeries</c> shipped
/// without the same override. This test is the guard so a THIRD occurrence turns red instead of shipping silent.
/// </summary>
public class SeriesVisitorCoverageContractTests
{
    [Fact]
    public void SvgSeriesRenderer_OverridesVisit_ForEveryConcreteSeriesType()
    {
        var seriesTypes = SeriesCountContractTests.ConcreteSeries();
        var overridden = typeof(SvgSeriesRenderer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == nameof(MatPlotLibNet.Rendering.ISeriesVisitor.Visit) && m.GetParameters().Length == 2)
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToHashSet();

        var missing = seriesTypes.Where(t => !overridden.Contains(t)).ToArray();

        Assert.True(missing.Length == 0,
            "SvgSeriesRenderer has no Visit override for (renders nothing, silently): " +
            string.Join(", ", missing.Select(t => t.Name)));
    }
}
