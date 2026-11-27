// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The publishers a test hands a session or a registry: the silent one declares only the members of the interface that have no
/// default body, and the recording one keeps what it is told and behaves as asked.</summary>
public sealed class PublisherDoublesTests
{
    [Fact]
    public async Task TheSilentPublisher_DeclaresTheMembersNoDefaultBodyCovers_AndTheDefaultOneStillRuns()
    {
        var withoutBody = typeof(IChartPublisher).GetMethods().Where(m => m.IsAbstract).Select(m => m.Name).Order().ToList();
        var declared = typeof(SilentChartPublisher)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(m => m.Name).Order().ToList();

        Assert.Equal(withoutBody, declared);
        await ((IChartPublisher)new SilentChartPublisher()).PublishViewChangeAsync("c", "v", "Viewer 1", TestContext.Current.CancellationToken);
    }

}
