// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { SvgPartsDocument } from '../lib/svg-parts-document';

const N = '\r\n';
const part = (name: string, body: string) => `<g data-mpl-part="${name}">${N}${body}${N}</g>${N}`;
const ROOT = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" data-xmin="0">';
const SCRIPT = '<script type="text/ecmascript"><![CDATA[ (function(){})(); ]]></script>';
const doc = (root: string, bg: string, a0: string, a1: string, overlay: string, tail: string) =>
  root + N + '<title id="chart-title">t</title>' + N + part('background', bg) + part('axes-0', a0) + part('axes-1', a1)
  + part('overlay', overlay) + tail + '</svg>' + N;

describe('SvgPartsDocument', () => {
  it('puts an axes part in place of the part of its name', () => {
    const document = new SvgPartsDocument(doc(ROOT, '<rect/>', '<g><g>first</g></g>', '<g>second</g>', '<text>o</text>', SCRIPT + N));

    expect(document.apply({ axes: [{ index: 1, markup: part('axes-1', '<g>changed</g>') }] })).toBe(true);

    expect(document.wrapped).toBe(doc(ROOT, '<rect/>', '<g><g>first</g></g>', '<g>changed</g>', '<text>o</text>', SCRIPT + N));
  });

  it('replaces the root tag, the background and the overlay too', () => {
    const document = new SvgPartsDocument(doc(ROOT, '<rect/>', 'a', 'b', '<text>o</text>', ''));
    const root = ROOT.replace('data-xmin="0"', 'data-xmin="2"');

    expect(document.apply({ rootTag: root + N, background: part('background', '<rect x="1"/>'), overlay: part('overlay', '<text>p</text>') })).toBe(true);

    expect(document.wrapped).toBe(doc(root, '<rect x="1"/>', 'a', 'b', '<text>p</text>', ''));
  });

  it('ends an overlay followed by scripts where the first script begins', () => {
    const document = new SvgPartsDocument(doc(ROOT, 'r', 'a', 'b', '<text>o</text>', SCRIPT + N + SCRIPT + N));

    expect(document.apply({ overlay: part('overlay', '<text>new</text>') })).toBe(true);

    expect(document.wrapped).toBe(doc(ROOT, 'r', 'a', 'b', '<text>new</text>', SCRIPT + N + SCRIPT + N));
  });

  it('refuses a part the document does not have and stays as it was', () => {
    const before = doc(ROOT, 'r', 'a', 'b', 'o', '');
    const document = new SvgPartsDocument(before);

    expect(document.apply({ axes: [{ index: 5, markup: part('axes-5', 'x') }] })).toBe(false);
    expect(document.apply({ axes: [{ index: 0, markup: 'not a part' }] })).toBe(false);

    expect(document.wrapped).toBe(before);
  });

  it('gives the plain document without its part groups', () => {
    const document = new SvgPartsDocument(doc(ROOT, '<rect/>', '<g><g>a</g></g>', '<g>b</g>', '<text>o</text>', SCRIPT + N));

    expect(document.plain).toBe(ROOT + N + '<title id="chart-title">t</title>' + N + '<rect/>' + N + '<g><g>a</g></g>' + N
      + '<g>b</g>' + N + '<text>o</text>' + N + SCRIPT + N + '</svg>' + N);
  });

  it('takes the line breaks the server wrote, whichever they are', () => {
    const unix = '<svg>\n<g data-mpl-part="background">\n<rect/>\n</g>\n<g data-mpl-part="overlay">\n<text/>\n</g>\n</svg>\n';

    expect(new SvgPartsDocument(unix).plain).toBe('<svg>\n<rect/>\n<text/>\n</svg>\n');
  });

  it('tells a document in parts from one without', () => {
    expect(SvgPartsDocument.isWrapped('<svg><rect/></svg>')).toBe(false);
    expect(SvgPartsDocument.isWrapped(doc(ROOT, 'r', 'a', 'b', 'o', ''))).toBe(true);
  });
});
