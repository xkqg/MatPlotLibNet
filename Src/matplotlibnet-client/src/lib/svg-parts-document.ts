// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/** One subplot that changed: its index and its new part, the whole `<g data-mpl-part="axes-…">` element. */
export interface SvgAxesPart {
  index: number;
  markup: string;
}

/**
 * The parts of a chart's SVG that changed, as the server's `UpdateChartParts` carries them: each part is the whole
 * `<g data-mpl-part="…">` element, and a part that did not change is `null` or absent.
 */
export interface SvgParts {
  rootTag?: string | null;
  background?: string | null;
  overlay?: string | null;
  axes?: SvgAxesPart[] | null;
}

const PART_START = '<g data-mpl-part="';

/**
 * The last whole document of a chart, wrapped in its parts, into which every part the server sends is put in place of the
 * part of the same name, so a chart is handed a whole SVG while the wire carries only what changed. Every part runs from its
 * own `<g data-mpl-part>` to the next one; the last (the overlay) to the first script after it, or to the end of the
 * document: text in a part is written escaped, so no part holds a script tag.
 */
export class SvgPartsDocument {
  private svg: string;

  /** A document to patch, as the server sent it whole. */
  constructor(wrapped: string) {
    this.svg = wrapped;
  }

  /** Whether `svg` is a document in parts, which can be patched. */
  static isWrapped(svg: string): boolean {
    return svg.includes(PART_START);
  }

  /** The document as it stands, with its parts. */
  get wrapped(): string {
    return this.svg;
  }

  /**
   * The document without its part groups: what the figure's `ToSvg` writes, byte for byte, so a chart is shown the same
   * whether the server sent it whole or in parts. Each group's opening line and its closing `</g>` line are taken out with
   * the line breaks the server wrote.
   */
  get plain(): string {
    let text = '';
    let at = 0;
    for (let start = this.svg.indexOf(PART_START); start >= 0; start = this.svg.indexOf(PART_START, at)) {
      const end = endOfPart(this.svg, start);
      text += this.svg.slice(at, start);
      const body = this.svg.indexOf('\n', start) + 1;
      const close = this.svg.lastIndexOf('</g>', end - 1);
      text += this.svg.slice(body, close);
      at = end;
    }
    return text + this.svg.slice(at);
  }

  /**
   * Puts every part of `parts` in place of the part of the same name. Returns `false`, leaving the document as it was, when a
   * part names one the document does not have: the document then waits for the server's next whole one.
   */
  apply(parts: SvgParts): boolean {
    let text = this.svg;
    if (parts.rootTag) {
      text = parts.rootTag + text.slice(text.indexOf('\n') + 1);
    }

    const replacements: string[] = [];
    if (parts.background) replacements.push(parts.background);
    for (const axes of parts.axes ?? []) replacements.push(axes.markup);
    if (parts.overlay) replacements.push(parts.overlay);

    for (const part of replacements) {
      const name = nameOf(part);
      if (name === null) return false;
      const start = text.indexOf(PART_START + name + '"');
      if (start < 0) return false;
      text = text.slice(0, start) + part + text.slice(endOfPart(text, start));
    }

    this.svg = text;
    return true;
  }
}

/** The name a part's opening tag gives it, or `null` for markup that is not a part. */
function nameOf(part: string): string | null {
  if (!part.startsWith(PART_START)) return null;
  const close = part.indexOf('"', PART_START.length);
  return close < 0 ? null : part.slice(PART_START.length, close);
}

/** Where the part that begins at `start` ends: at the next part, or at the first script after it, or at the end. */
function endOfPart(text: string, start: number): number {
  const next = text.indexOf(PART_START, start + PART_START.length);
  if (next >= 0) return next;
  const script = text.indexOf('<script', start);
  return script >= 0 ? script : text.lastIndexOf('</svg>');
}
