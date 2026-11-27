// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The one place a series is made from its JSON object or reported as unknown: the primary series list, the
/// right-hand and top axes and the outward Y slots of a figure all read through it, so the report is word for word the same.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class AxesSerializationExtensionsTests
{
    [Fact]
    public void CreateFromDtoOrReport_AKnownKind_BuildsTheSeriesOnTheAxesAndReportsNothing()
    {
        var axes = new Axes();
        ISeries? created = null;

        var reports = DiagnosticsCapture.During(() => created = axes.CreateFromDtoOrReport(new SeriesDto { Type = "line", XData = [1, 2], YData = [3, 4] }));

        Assert.IsType<LineSeries>(created);
        Assert.Same(created, Assert.Single(axes.Series));
        Assert.Empty(reports);
    }

    [Fact]
    public void CreateFromDtoOrReport_AnUnknownKind_ReturnsNullAndReportsItByName()
    {
        var axes = new Axes();
        ISeries? created = null;

        var reports = DiagnosticsCapture.During(() => created = axes.CreateFromDtoOrReport(new SeriesDto { Type = "__bogus_kind__" }));

        Assert.Null(created);
        Assert.Empty(axes.Series);
        var report = Assert.Single(reports);
        Assert.Equal("ChartSerializer", report.Source);
        Assert.Equal("Unknown series discriminator '__bogus_kind__' was skipped during deserialization.", report.Message);
        Assert.Null(report.Exception);
    }

    /// <summary>A kind that is known but whose data its reader cannot make a series of is reported as unreadable, with the reason
    /// its reader gives, and not as a kind nobody knows. Measured before the change: "Unknown series discriminator '...'".</summary>
    [Fact]
    public void CreateFromDtoOrReport_AKnownKindWhoseDataIsUnreadable_IsReportedSo_WithTheReason()
    {
        const string kind = "__unreadable_kind__";
        SeriesRegistry.RegisterReader(kind, (_, _) => SeriesRead.Unreadable("its projection 'Peirce' is not one this package makes"));
        try
        {
            var axes = new Axes();

            var reports = DiagnosticsCapture.During(() => Assert.Null(axes.CreateFromDtoOrReport(new SeriesDto { Type = kind })));

            Assert.Equal($"The '{kind}' series was skipped during deserialization: its data is unreadable: its projection 'Peirce' is not one this package makes.",
                Assert.Single(reports).Message);
        }
        finally
        {
            SeriesRegistry.ResetForTestsInternal();
        }
    }

    /// <summary>A reader registered through the public <see cref="SeriesRegistry.Register"/> gives no reason; its kind is known all the
    /// same.</summary>
    [Fact]
    public void CreateFromDtoOrReport_AKnownKindWhosePublicReaderMakesNothing_IsReportedAsUnreadable()
    {
        const string kind = "__nothing_kind__";
        SeriesRegistry.Register(kind, (_, _) => null);
        try
        {
            var axes = new Axes();

            var reports = DiagnosticsCapture.During(() => Assert.Null(axes.CreateFromDtoOrReport(new SeriesDto { Type = kind })));

            Assert.Equal($"The '{kind}' series was skipped during deserialization: its data is unreadable: its reader made nothing of it.",
                Assert.Single(reports).Message);
        }
        finally
        {
            SeriesRegistry.ResetForTestsInternal();
        }
    }

    [Fact]
    public void CreateFromDtoOrReport_AKindThatWasNeverWritten_IsReportedAsUnknown()
    {
        var axes = new Axes();

        var reports = DiagnosticsCapture.During(() => Assert.Null(axes.CreateFromDtoOrReport(new SeriesDto())));

        Assert.Equal("Unknown series discriminator 'unknown' was skipped during deserialization.", Assert.Single(reports).Message);
    }

    [Fact]
    public void CreateOnSecondarySeat_AnUnknownKind_ReportsThroughTheSameOwnerAndSeatsNothing()
    {
        var axes = new Axes();
        var seated = new List<ISeries>();

        var reports = DiagnosticsCapture.During(() => Assert.Null(axes.CreateOnSecondarySeat(new SeriesDto { Type = "__bogus_kind__" }, seated.Add)));

        Assert.Empty(seated);
        Assert.Equal("Unknown series discriminator '__bogus_kind__' was skipped during deserialization.", Assert.Single(reports).Message);
    }

    [Fact]
    public void CreateOnSecondarySeat_AKnownKind_MovesTheSeriesOffThePrimaryListToTheSeat()
    {
        var axes = new Axes();
        var seated = new List<ISeries>();

        var created = axes.CreateOnSecondarySeat(new SeriesDto { Type = "line", XData = [1, 2], YData = [3, 4] }, seated.Add);

        Assert.Same(created, Assert.Single(seated));
        Assert.Empty(axes.Series);
    }
}
