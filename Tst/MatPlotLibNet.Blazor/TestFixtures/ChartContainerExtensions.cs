// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using Microsoft.AspNetCore.Components;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>What every chart component renders into its container: the drawing when it is given a figure, and a bare container when it is
/// given none. The components differ in the figure they take and in the class of their container; the two readings are written once.</summary>
internal static class ChartContainerExtensions
{
    extension(BunitContext context)
    {
        /// <summary>Renders <typeparamref name="TComponent"/> with the given parameters and checks its container holds an SVG.</summary>
        /// <typeparam name="TComponent">The chart component.</typeparam>
        /// <param name="container">The CSS selector of the component's container.</param>
        /// <param name="parameters">Gives the component its figure.</param>
        internal void AssertContainerHoldsAnSvg<TComponent>(string container, Action<ComponentParameterCollectionBuilder<TComponent>> parameters)
            where TComponent : IComponent
        {
            var cut = context.Render(parameters);

            Assert.NotNull(cut.Find(container));
            Assert.NotNull(cut.Find("svg"));
        }

        /// <summary>Renders <typeparamref name="TComponent"/> with no figure and checks its container is there and holds no SVG.</summary>
        /// <typeparam name="TComponent">The chart component.</typeparam>
        /// <param name="container">The CSS selector of the component's container.</param>
        internal void AssertContainerIsEmpty<TComponent>(string container) where TComponent : IComponent
        {
            var cut = context.Render<TComponent>();

            Assert.NotNull(cut.Find(container));
            Assert.Throws<ElementNotFoundException>(() => cut.Find("svg"));
        }
    }
}
