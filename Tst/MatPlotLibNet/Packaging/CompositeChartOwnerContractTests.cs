// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A chart made of other charts has one owner. A clustermap is the <see cref="ClustermapSeries"/> and a pair plot the
/// <see cref="PairGridSeries"/>: the template of each, and the faceted figure of a pair plot, hand their data to the series and draw
/// nothing of their own. The clustering of a matrix is the numerics' (<c>HierarchicalClustering</c>), read by the template that
/// clusters, and a dendrogram is drawn from one tree model, the <c>TreeNode</c> its merges become (both held by rows of
/// <c>OwnedRules.SeriesRules</c>). Every member of
/// <see cref="FigureTemplates"/> is a factory: its body makes the figure of its layout and builds it.</summary>
public class CompositeChartOwnerContractTests
{
    private static readonly double[,] Matrix =
    {
        { 1.0, 2.0, 0.5 },
        { 5.0, 0.1, 4.0 },
        { 1.1, 1.9, 0.6 },
        { 4.8, 0.2, 3.9 },
    };

    private static readonly double[][] Columns = [[1.0, 2, 3, 4], [2.0, 1, 4, 3], [5.0, 3, 1, 2]];

    /// <summary>The clustermap template draws the clustermap series: one axes, one series, its rows and columns clustered into the
    /// trees it reorders the matrix by.</summary>
    [Fact]
    public void TheClustermapTemplate_IsTheClustermapSeries()
    {
        var figure = FigureTemplates.Clustermap(Matrix).Build();

        var axes = Assert.Single(figure.SubPlots);
        var map = Assert.IsType<ClustermapSeries>(Assert.Single(axes.Series));
        Assert.Same(Matrix, map.Data);
        Assert.NotNull(map.RowTree);
        Assert.NotNull(map.ColumnTree);
    }

    /// <summary>The pair plot template, and the faceted figure it is, draw the pair grid series: one axes, one series over the
    /// columns given.</summary>
    [Fact]
    public void ThePairPlotTemplateAndFigure_AreThePairGridSeries()
    {
        foreach (var builder in new[] { FigureTemplates.PairPlot(Columns), new PairPlotFigure(Columns).Build() })
        {
            var axes = Assert.Single(builder.Build().SubPlots);
            var grid = Assert.IsType<PairGridSeries>(Assert.Single(axes.Series));
            Assert.Same(Columns, grid.Variables);
        }
    }

    

    /// <summary>Every template is a factory: its body makes the figure of its layout (<c>new XFigure(…) { … }</c>) and builds it.
    /// The layout lives in the figure, so a template holds no arithmetic, no loop and no subplot of its own.</summary>
    [Fact]
    public void EveryTemplate_IsAFactoryOverItsFigure()
    {
        string text = File.ReadAllText(Path.Combine(RepositoryRoot.Folder, "Src", "MatPlotLibNet", "FigureTemplates.cs"));
        var members = Regex.Matches(text,
            @"public static FigureBuilder (?<name>\w+)\((?<parameters>(?:[^()]|\([^()]*\))*)\)\s*(?<body>=>[^;]*;|\{)");

        Assert.NotEmpty(members);
        var notFactories = members
            .Where(member => !Regex.IsMatch(member.Groups["body"].Value, @"^=>\s*new\s+\w+Figure\b[^;]*\.Build\(\);$"))
            .Select(member => member.Groups["name"].Value)
            .ToArray();

        Assert.Empty(notFactories);
        Assert.Equal(typeof(FigureTemplates).GetMethods().Count(method => method.IsStatic && method.ReturnType == typeof(FigureBuilder)), members.Count);
    }
}
