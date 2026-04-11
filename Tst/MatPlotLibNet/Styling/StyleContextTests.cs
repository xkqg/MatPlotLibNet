// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="StyleContext"/> scoping, disposal, and nesting.</summary>
public class StyleContextTests
{
    [Fact]
    public void StyleContext_OverridesCurrentDuringScope()
    {
        var sheet = new StyleSheet("test", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 99.0
        });

        using (new StyleContext(sheet))
        {
            Assert.Equal(99.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
        }
    }

    [Fact]
    public void StyleContext_Dispose_RestoresPreviousCurrent()
    {
        var outer = RcParams.Current;

        var sheet = new StyleSheet("test", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 99.0
        });

        using (new StyleContext(sheet)) { }

        // After dispose, Current should be restored to outer
        Assert.Same(outer, RcParams.Current);
    }

    [Fact]
    public void StyleContext_Nested_Scopes_StackCorrectly()
    {
        var inner = new StyleSheet("inner", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 20.0
        });
        var outer = new StyleSheet("outer", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 10.0
        });

        using (new StyleContext(outer))
        {
            Assert.Equal(10.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));

            using (new StyleContext(inner))
            {
                Assert.Equal(20.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
            }

            // Inner disposed → back to outer
            Assert.Equal(10.0, RcParams.Current.Get<double>(RcParamKeys.FontSize));
        }
    }

    [Fact]
    public async Task StyleContext_AsyncAwait_FlowsToChildren()
    {
        double capturedFontSize = 0;

        var sheet = new StyleSheet("async-test", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 77.0
        });

        using (new StyleContext(sheet))
        {
            await Task.Run(() =>
            {
                capturedFontSize = RcParams.Current.Get<double>(RcParamKeys.FontSize);
            }, TestContext.Current.CancellationToken);
        }

        Assert.Equal(77.0, capturedFontSize);
    }
}
