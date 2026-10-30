// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Transforms;

/// <summary>Writes an animation as one SVG file that steps through its frames: every frame is in the document, one is shown at a
/// time, and a slider, a play button and the arrow keys move between them. The same shape as the GIF transform: the frames, the
/// time between them, whether it loops, and where to write. It is the animation for a page where a GIF is the wrong tool: it stays
/// sharp at any size, its text is text, and its frames can be inspected.
/// <para>Each frame is a whole SVG, put in the document under a prefix of its own for its ids (<c>f0-</c>, <c>f1-</c>, …), so the
/// document never defines one twice and every reference still finds its id. A frame is a picture: the scripts of an interactive
/// figure are left out. Without its script the document is its first frame, which is what an <c>&lt;img&gt;</c>, a README or a
/// viewer that runs nothing shows (the controls are hidden until the script reveals them). A reader who asked the page for reduced
/// motion gets no autoplay.</para>
/// <para>The file is as large as its frames together: no frame is dropped or shared, so a long animation of detailed figures is a
/// large file. Frames of different sizes are placed at the top left, and the document is as large as the largest.</para></summary>
public sealed class StepThroughSvgTransform
{
    /// <summary>The height, in pixels, of the strip below the frames that holds the slider and the play button.</summary>
    public const double ControlsHeight = 36;

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private const double TrackLeft = 48;
    private const double TrackRightMargin = 16;
    private const string PlayGlyph = "M16 11 L16 25 L26 18 Z";

    /// <summary>Writes the animation to <paramref name="output"/>.</summary>
    /// <param name="frames">The frames, in order; enumerated once. At least one.</param>
    /// <param name="interval">The time each frame is shown while the animation plays.</param>
    /// <param name="loop">Whether it starts again after the last frame, or stops there.</param>
    /// <param name="output">The destination stream.</param>
    /// <exception cref="ArgumentException"><paramref name="frames"/> holds no frame.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not a positive time.</exception>
    public void Transform(IEnumerable<Figure> frames, TimeSpan interval, bool loop, Stream output)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(output);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The time between frames is a positive time.");

        var pictures = new List<Picture>();
        foreach (var figure in frames)
            pictures.Add(Draw(figure, pictures.Count));
        if (pictures.Count == 0)
            throw new ArgumentException("A step-through SVG needs at least one frame.", nameof(frames));

        double width = pictures.Max(p => p.Width), height = pictures.Max(p => p.Height);
        bool steps = pictures.Count > 1;
        double total = height + (steps ? ControlsHeight : 0);
        string size(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        var root = new XElement(Svg + "svg",
            new XAttribute("viewBox", $"0 0 {size(width)} {size(total)}"),
            new XAttribute("width", size(width)),
            new XAttribute("height", size(total)),
            new XAttribute("role", "group"),
            new XAttribute("aria-label", $"Animation of {pictures.Count} {(pictures.Count == 1 ? "frame" : "frames")}"),
            new XAttribute("aria-roledescription", "animation"),
            new XAttribute("data-interval-ms", ((long)interval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("data-loop", loop ? "true" : "false"));
        if (steps) root.Add(new XAttribute("tabindex", "0"));

        for (int k = 0; k < pictures.Count; k++)
        {
            var frame = new XElement(Svg + "g", new XAttribute("data-frame", k.ToString(CultureInfo.InvariantCulture)), pictures[k].Element);
            if (k > 0) frame.Add(new XAttribute("display", "none"));
            root.Add(frame);
        }

        if (steps)
        {
            root.Add(Controls(width, height, pictures.Count));
            root.Add(ScriptElement());
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>A frame as a whole SVG under the id prefix of its place, without scripts, sized as the figure it was drawn from is.</summary>
    private static Picture Draw(Figure figure, int position)
    {
        string svg = SvgRendererDefault.Current.Render(figure).WithSaveOptions(new SaveOptions { SvgIdPrefix = $"f{position.ToString(CultureInfo.InvariantCulture)}-" });
        var element = XDocument.Parse(svg).Root!;

        element.Descendants().Where(e => e.Name.LocalName == "script").ToList().ForEach(e => e.Remove());

        double width = figure.Width, height = figure.Height;

        // The frame keeps the size it was drawn at inside the animation: a figure that scales to its container would scale to
        // the whole animation instead.
        element.SetAttributeValue("width", width.ToString("0.##", CultureInfo.InvariantCulture));
        element.SetAttributeValue("height", height.ToString("0.##", CultureInfo.InvariantCulture));
        element.SetAttributeValue("style", null);
        element.SetAttributeValue("x", "0");
        element.SetAttributeValue("y", "0");
        return new Picture(element, width, height);
    }

    private readonly record struct Picture(XElement Element, double Width, double Height);

    /// <summary>The strip below the frames: a play button and a slider whose track is clicked to go to a frame. Hidden until the
    /// script runs.</summary>
    private static XElement Controls(double width, double top, int frames)
    {
        double track = Math.Max(1, width - TrackLeft - TrackRightMargin);
        string number(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        return new XElement(Svg + "g",
            new XAttribute("data-steps-controls", ""),
            new XAttribute("display", "none"),
            new XAttribute("transform", $"translate(0,{number(top)})"),
            new XElement(Svg + "rect", new XAttribute("width", number(width)), new XAttribute("height", number(ControlsHeight)), new XAttribute("fill", "#f3f3f3")),
            new XElement(Svg + "g",
                new XAttribute("role", "button"), new XAttribute("tabindex", "0"),
                new XAttribute("aria-label", "Play"), new XAttribute("aria-pressed", "false"),
                new XAttribute("style", "cursor:pointer"),
                new XElement(Svg + "rect", new XAttribute("x", "8"), new XAttribute("y", "6"), new XAttribute("width", "24"), new XAttribute("height", "24"),
                    new XAttribute("rx", "4"), new XAttribute("fill", "#dcdcdc")),
                new XElement(Svg + "path", new XAttribute("data-steps-glyph", ""), new XAttribute("d", PlayGlyph), new XAttribute("fill", "#333333"))),
            new XElement(Svg + "g",
                new XAttribute("data-steps-track", ""), new XAttribute("data-width", number(track)),
                new XAttribute("transform", $"translate({number(TrackLeft)},0)"),
                new XAttribute("style", "cursor:pointer"),
                new XElement(Svg + "rect", new XAttribute("x", "0"), new XAttribute("y", "6"), new XAttribute("width", number(track)), new XAttribute("height", "24"),
                    new XAttribute("fill", "transparent")),
                new XElement(Svg + "rect", new XAttribute("x", "0"), new XAttribute("y", "16"), new XAttribute("width", number(track)), new XAttribute("height", "4"),
                    new XAttribute("rx", "2"), new XAttribute("fill", "#c8c8c8")),
                new XElement(Svg + "circle",
                    new XAttribute("role", "slider"), new XAttribute("tabindex", "0"), new XAttribute("aria-label", "Frame"),
                    new XAttribute("aria-valuemin", "0"), new XAttribute("aria-valuemax", (frames - 1).ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("aria-valuenow", "0"), new XAttribute("aria-valuetext", $"Frame 1 of {frames.ToString(CultureInfo.InvariantCulture)}"),
                    new XAttribute("cx", "0"), new XAttribute("cy", "18"), new XAttribute("r", "7"), new XAttribute("fill", "#555555"))));
    }

    /// <summary>The script, as an element: its source is data inside CDATA, so it is added as a node and not parsed.</summary>
    private static XElement ScriptElement()
    {
        var parsed = XElement.Parse(SvgStepThroughScript.GetScript());
        return new XElement(Svg + "script", new XAttribute("type", "text/ecmascript"), new XCData(parsed.Value));
    }
}
