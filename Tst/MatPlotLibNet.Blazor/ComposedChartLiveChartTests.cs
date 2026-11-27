// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Bunit;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>A live chart component is fed the JSON a server published and draws the chart from it. Every composed shape,
/// with and without a missing value, must come out of the component as the picture <see cref="MplChart"/> draws for the
/// figure itself: the same element, attribute by attribute.</summary>
public class ComposedChartLiveChartTests : BunitContext
{
    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public void ALiveChartFedTheJson_DrawsThePictureOfTheFigure(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();
        var client = new RecordingSubscriptionClient();
        var live = Render<MplLiveChart>(parameters => { parameters.Add(x => x.ChartId, "composed"); parameters.Add(x => x.Client, client); });

        client.FireJson("composed", figure.ToJson());
        live.Render();

        var drawn = Render<MplChart>(parameters => parameters.Add(x => x.Figure, figure));
        Assert.Equal(drawn.Find("svg").OuterHtml, live.Find("svg").OuterHtml);
    }
}
