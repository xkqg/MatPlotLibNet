// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>An indicator is reached through four surfaces (its batch class, its streaming form, the verb on <c>AxesBuilder</c> and
/// the <c>WithStreaming</c> verb) and each rule about it has one owner they all read: a default is a <c>public const</c> on the batch
/// class, a label, the labels of the lines, a parameter check and the first value are said by the batch class's <c>Spec</c> factory
/// (an <c>IndicatorSpec</c>, made nowhere else) and read through the base of either form, the warm-up of a step is dropped by the
/// base's one fold, the bars a data frame gives are read by one call per shape, the opacity of a band by
/// <c>IndicatorLook.BandOpacity</c>, and a window of inputs is kept in <c>InputWindow</c>. Each test reads the source and names every
/// place that states the rule a second time.</summary>
public sealed class IndicatorSurfaceContractTests
{
    private const string Indicators = "Src/MatPlotLibNet/Indicators";

    private static IEnumerable<SourceFile> IndicatorFiles() => RepositoryTree.Files(Indicators, "*.cs");

    private static IEnumerable<SourceFile> StreamingFiles() => RepositoryTree.Files(Indicators + "/Streaming", "Streaming*.cs");

    private static IEnumerable<SourceFile> SurfaceFiles() =>
    [
        .. IndicatorFiles(),
        .. RepositoryTree.Files("Src/MatPlotLibNet.DataFrame", "DataFrameIndicatorExtensions.cs"),
        .. RepositoryTree.Files("Src/MatPlotLibNet/Builders", "AxesBuilder.cs"),
    ];

    private static List<string> Find(IEnumerable<SourceFile> files, Regex rule, Func<string, bool>? exempt = null)
    {
        var found = new List<string>();
        foreach (var file in files)
        {
            if (exempt?.Invoke(file.Path) == true) continue;
            var lines = file.CodeLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (rule.IsMatch(lines[i])) found.Add($"{Path.GetFileName(file.Path)}:{i + 1}: {lines[i].Trim()}");
            }
        }

        return found;
    }

    private static void None(List<string> found, string rule) =>
        Assert.True(found.Count == 0, $"{rule} ({found.Count}):\n" + string.Join('\n', found));

    /// <summary>The parameters of a public or internal signature, across lines.</summary>
    private static readonly Regex Signature = new(@"(?:public|internal)\s+(?:[\w<>\[\]?.]+\s+)?\w+\s*\(([^)]*)\)", RegexOptions.Compiled);

    private static readonly Regex LiteralDefault = new(@"\b(?:int|double)\s+\w+\s*=\s*-?[0-9][0-9_.eE+-]*\s*(?:,|$)", RegexOptions.Compiled);

    [Fact]
    public void EveryDefaultOfAnIndicatorParameter_IsAConstOfTheBatchClass_NeverALiteral()
    {
        var found = new List<string>();
        foreach (var file in SurfaceFiles())
        {
            string code = file.Code;
            foreach (Match signature in Signature.Matches(code))
            {
                foreach (var parameter in signature.Groups[1].Value.Split(','))
                {
                    if (LiteralDefault.IsMatch(parameter.Trim() + ",") && !NotAnIndicatorParameter(file.Path, signature.Value))
                        found.Add($"{Path.GetFileName(file.Path)}: {parameter.Trim()}");
                }
            }
        }

        None(found, "a literal default on an indicator surface; name the batch class's Default const");
    }

    /// <summary>The two verbs of <c>AxesBuilder</c> that take a numeric default and are not indicators.</summary>
    private static bool NotAnIndicatorParameter(string path, string signature) =>
        path.EndsWith("AxesBuilder.cs", StringComparison.Ordinal) && (signature.Contains("WithDownsampling(") || signature.Contains("DensityScatter("));

    [Fact]
    public void TheOpacityOfABand_IsStatedOnce()
    {
        None(Find(IndicatorFiles(), new Regex(@"\b0\.15\b"), p => p.EndsWith("IndicatorLook.cs", StringComparison.Ordinal)),
            "a band opacity outside IndicatorLook.BandOpacity");
    }

    /// <summary>The arithmetic a batch form and its streaming form share is written once: the CCI of a window in
    /// <c>ChannelIndexExtensions</c> (Lambert's constant is its mark).</summary>
    [Fact]
    public void TheChannelIndexOfAWindow_IsWrittenOnce()
    {
        None(Find(IndicatorFiles(), new Regex(@"\b0\.015\b"), p => p.EndsWith("ChannelIndexExtensions.cs", StringComparison.Ordinal)),
            "the CCI arithmetic outside ChannelIndexExtensions");
    }

    /// <summary>The DataFrame package reads the bars of each shape from named columns in one place: each factory of
    /// <c>OhlcvBars</c> is called once in the package, by the call of its shape in <c>DataFrameIndicatorExtensions</c>.</summary>
    [Fact]
    public void BarsFromNamedColumns_AreReadOncePerShape()
    {
        var calls = Find(RepositoryTree.Files("Src/MatPlotLibNet.DataFrame", "*.cs"), new Regex(@"OhlcvBars\.(Hlc|Hl|Hlv|Cv|Ohlc|Hlcv)\("));

        var elsewhere = calls.Where(c => !c.StartsWith("DataFrameIndicatorExtensions.cs:", StringComparison.Ordinal)).ToList();
        None(elsewhere, "bars built from columns outside the call of their shape; read df.Hlc(...) and the other shapes");
        Assert.Equal(6, calls.Count);
    }

    [Fact]
    public void AWindowOfInputs_WrapsInInputWindowOnly()
    {
        IEnumerable<SourceFile> files = [.. IndicatorFiles(), .. RepositoryTree.Files("Src/MatPlotLibNet/Data", "RingBuffer.cs")];
        None(Find(files, new Regex(@"%\s*\(?\s*_?\w*(Length|Width|Capacity|History|Lags|period|_period|_window)\b|\+\+_\w+\s*==|_\w+(Index|Slot)\s*=\s*\(")),
            "a ring wrap outside InputWindow");
    }

    [Fact]
    public void ALabelOfAStreamingForm_IsNeverWrittenByTheForm()
    {
        None(Find(StreamingFiles(), new Regex(@"\bLabel\s*=\s*(\$?""|.*\.ToString\()")),
            "a streaming form that writes its own label; take it from the batch class's Spec");
    }

    [Fact]
    public void AParameterCheck_IsWrittenOnlyInTheBatchClassSpecFactory()
    {
        None(Find(StreamingFiles(), new Regex(@"must be")),
            "a streaming form that checks its own parameters; the batch class's Spec checks them for both");
        None(Find(IndicatorFiles(), new Regex(@"must be (>=|>) -?\d+ \(got")),
            "a range check spelled by hand; read ThrowIfBelow / ThrowIfNotAbove / ThrowIfNotBetween");
    }

    [Fact]
    public void ASpec_IsMadeOnlyByItsFactory()
    {
        None(Find(IndicatorFiles(), new Regex(@"new IndicatorSpec\s*\(|default\(IndicatorSpec\)|IndicatorSpec\s+\w+\s*=\s*default")),
            "an IndicatorSpec made around its factory");
    }

    /// <summary>A streaming form has one root, <c>StreamingIndicator&lt;TInput&gt;</c>: the count, the warm-up, the lines, the pin and the
    /// way a price or a bar reaches an input are stated there once. No second root stands beside it.</summary>
    [Fact]
    public void EveryStreamingForm_StandsOnTheOneGenericRoot()
    {
        var root = typeof(MatPlotLibNet.Indicators.Streaming.StreamingIndicator<>);
        var forms = typeof(MatPlotLibNet.Indicators.Streaming.IStreamingIndicator).Assembly.GetTypes()
            .Where(t => t.IsClass && typeof(MatPlotLibNet.Indicators.Streaming.IStreamingIndicator).IsAssignableFrom(t))
            .ToList();

        var offRoot = forms.Where(t => t != root && !StandsOn(t, root)).Select(t => t.FullName).ToList();

        Assert.True(forms.Count > 50, $"only {forms.Count} streaming forms found");
        None(offRoot.Select(name => name ?? "?").ToList(), "a streaming form, or a second root, off StreamingIndicator<TInput>");
    }

    private static bool StandsOn(Type type, Type genericRoot)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == genericRoot) return true;
        }

        return false;
    }

    /// <summary>The batch indicators: every file of <c>Indicators</c> outside the streaming forms.</summary>
    private static IEnumerable<SourceFile> BatchFiles() =>
        IndicatorFiles().Where(file => !file.Path.Contains("/Streaming/", StringComparison.Ordinal));

    /// <summary>A batch indicator is named by its spec once, by its base (<c>Indicator&lt;TResult&gt;</c> takes the spec in its
    /// constructor and sets the label from it); the labels of its other lines are read through <c>LineLabel</c>.</summary>
    [Fact]
    public void ALabelOfABatchIndicator_IsSetOnceByItsBase()
    {
        None(Find(BatchFiles(), new Regex(@"\bLabel\s*=\s*(?:Spec\(|_spec\b)|\bIndicatorSpec\s+_spec\b"), p => Path.GetFileName(p) == "Indicator.cs"),
            "a batch indicator that sets its own label from its spec, or keeps the spec itself; pass Spec(...) to the base constructor");
    }

    /// <summary>What a step says before the first value is dropped by the one fold of the base
    /// (<c>Indicator&lt;TResult&gt;.FoldBars</c>, reached through <c>FoldPrices</c>, <c>FoldHlc</c> and the others): no batch indicator
    /// skips the warm-up bars of a step by hand.</summary>
    [Fact]
    public void TheWarmUpOfAStep_IsDroppedByTheOneFold()
    {
        None(Find(BatchFiles(), new Regex(@"if \((?:i|t|b|w|bar)\s*(?:>=|<|==)(?>\s*)(?!FirstValueIndex)[^)]*\)\s*(?:continue;|\w+\[(?:i|t|b|w|bar)\s*-\s*[^\]]+\]\s*=)"),
                p => Path.GetFileName(p) == "Indicator.cs"),
            "a fold that skips the warm-up of a step by hand; fold through FoldPrices, FoldHlc, FoldCloseVolume or FoldHlcv");
    }

    /// <summary>The checks of the bars an indicator is given are written once, in <c>BarCheckExtensions</c>: a bar whose high is below
    /// its low, and a price that is not above zero; the batch indicators and the streaming forms call them.</summary>
    [Fact]
    public void ACheckOfTheBars_IsWrittenOnce()
    {
        None(Find(IndicatorFiles(), new Regex(@"requires (?:H >= L|strictly positive)|\[\w+\]\s*(?:<=\s*0\b|<\s*[\w.]*Low\[)|\binput(?:\.High)?\s*(?:<=\s*0\b|<\s*input\.Low\b)"),
                p => Path.GetFileName(p) == "BarCheckExtensions.cs"),
            "a check of the bars written by hand; read ThrowIfHighBelowLow, ThrowIfNotPositivePrices or ThrowIfNotPositiveOhlc");
    }

    [Fact]
    public void AStreamingForm_WarmsUpAsItsSpecSays()
    {
        None(Find(StreamingFiles(), new Regex(@"WarmupPeriod\s*=>(?!\s*Spec\.Warmup;)")),
            "a warm-up stated by a streaming form; read Spec.Warmup");
    }
}
