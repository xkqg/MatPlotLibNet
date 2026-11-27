// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Bunit;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>The hub client of the Blazor package, against a real hub: the JSON it is sent draws the figure's picture, and the
/// SVG it is sent is the figure's picture.</summary>
public class ComposedChartClientTests : IClassFixture<ComposedChartClientFixture>
{
    private readonly ComposedChartClientFixture _fixture;

    public ComposedChartClientTests(ComposedChartClientFixture fixture) => _fixture = fixture;

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheJsonTheClientReceives_DrawsTheFiguresPicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string json = await _fixture.JsonReceivedAsync($"json-{name}-{missing}", figure, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(json).ToSvg());
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheSvgTheClientReceives_IsTheFiguresPicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string svg = await _fixture.SvgReceivedAsync($"svg-{name}-{missing}", figure, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), svg);
    }
}
