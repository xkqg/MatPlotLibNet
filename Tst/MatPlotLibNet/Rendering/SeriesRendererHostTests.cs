// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies the seam a series of a package draws through (<see cref="ISeriesRendererHost"/>): the library's series renderer hands
/// the renderer the series names the context of its pass, as it hands every renderer of its own, and a visitor that is not the library's
/// is handed nothing, as it is handed no series it has no <c>Visit</c> for.</summary>
public class SeriesRendererHostTests
{
    private static readonly Rect Plot = new(0, 0, 100, 100);

    /// <summary>The renderer a package's series names is made over the context of the pass: its theme, the colour of the series' slot and
    /// the transform of its axes, so it draws as every renderer of the library draws.</summary>
    [Fact]
    public void TheSvgRenderer_MakesThePackagesRenderer_OverTheContextOfItsPass()
    {
        var transform = new DataTransform(-20, 80, 5, 65, Plot);
        var renderer = new SvgSeriesRenderer(transform, new SvgRenderContext(), Colors.Blue, Theme.Dark);
        SeriesRenderContext? seen = null;

        new PackageSeries().Accept(renderer, context => seen = context, () => { });

        Assert.NotNull(seen);
        Assert.Same(Theme.Dark, seen.Theme);
        Assert.Same(transform, seen.Transform);
        Assert.Equal(Colors.Blue, seen.SeriesColor);
    }

    /// <summary>A pass that collects the marks a legend keeps clear of draws nothing: the package's renderer is asked for its marks, and
    /// a series that fills its plot (a map) has none.</summary>
    [Fact]
    public void APassThatCollectsMarks_DrawsNothingOfThePackagesSeries()
    {
        var drawn = new SvgRenderContext();
        var collector = new SvgSeriesRenderer(new DataTransform(0, 1, 0, 1, Plot), drawn, Colors.Blue, Theme.Default)
        {
            Marks = new MarkObstacles.Builder(),
        };
        int made = 0, rendered = 0;

        new PackageSeries().Accept(collector, _ => made++, () => rendered++);

        Assert.Equal(1, made);
        Assert.Equal(0, rendered);
    }

    /// <summary>A visitor that is not the library's renderer is handed no package series, as it is handed none it has no
    /// <c>Visit</c> for: the series draws nothing of its own into the area.</summary>
    [Fact]
    public void AVisitorOfYourOwn_IsHandedNothing()
    {
        int made = 0;

        new PackageSeries().Accept(new TestSeriesVisitor(), _ => made++, () => { });
        new PackageSeries().Accept(DispatchProxy.Create<ISeriesVisitor, SilentVisitor>(), _ => made++, () => { });

        Assert.Equal(0, made);
    }

    /// <summary>The public visitor carries no member this assembly alone can implement, so an application can still build a
    /// proxy or a mock of it.</summary>
    [Fact]
    public void ThePublicVisitor_CanStillBeProxied()
    {
        Assert.NotNull(DispatchProxy.Create<ISeriesVisitor, SilentVisitor>());
        Assert.IsNotAssignableFrom<ISeriesRendererHost>(new TestSeriesVisitor());
    }

    /// <summary>An <see cref="ISeriesVisitor"/> that does nothing.</summary>
    public class SilentVisitor : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }

    /// <summary>A series as a package writes one: it hands itself to the visitor with the renderer it is drawn by.</summary>
    private sealed class PackageSeries : ChartSeries
    {
        internal void Accept(ISeriesVisitor visitor, Action<SeriesRenderContext> made, Action rendered) =>
            visitor.Draw(this, context =>
            {
                made(context);
                return new PackageRenderer(context, rendered);
            });

        public override void Accept(ISeriesVisitor visitor, RenderArea area) => Accept(visitor, _ => { }, () => { });

        public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

        public override MatPlotLibNet.Serialization.SeriesDto ToSeriesDto() => new() { Type = "package" };
    }

    /// <summary>The renderer of <see cref="PackageSeries"/>: tells when it draws.</summary>
    private sealed class PackageRenderer(SeriesRenderContext context, Action rendered) : SeriesRenderer<PackageSeries>(context)
    {
        public override void Render(PackageSeries series) => rendered();
    }
}