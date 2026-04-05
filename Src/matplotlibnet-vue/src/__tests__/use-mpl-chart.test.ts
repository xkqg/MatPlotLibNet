// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ref, nextTick } from 'vue';
import { useMplChart } from '../lib/use-mpl-chart';

// Helper to flush microtasks and Vue reactivity
async function flush() {
  await nextTick();
  await new Promise(r => setTimeout(r, 0));
}

describe('useMplChart', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('fetches SVG from the given URL', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg>chart</svg>'),
    }));

    const { svgContent, loading, error } = useMplChart('/api/chart.svg');

    await flush();

    expect(svgContent.value).toBe('<svg>chart</svg>');
    expect(loading.value).toBe(false);
    expect(error.value).toBeNull();
  });

  it('handles fetch errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')));

    const { error, loading } = useMplChart('/api/chart.svg');

    await flush();

    expect(error.value).not.toBeNull();
    expect(error.value!.message).toBe('Network error');
    expect(loading.value).toBe(false);
  });

  it('re-fetches when reactive URL changes', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ text: () => Promise.resolve('<svg>first</svg>') })
      .mockResolvedValueOnce({ text: () => Promise.resolve('<svg>second</svg>') });
    vi.stubGlobal('fetch', fetchMock);

    const url = ref('/api/chart1.svg');
    const { svgContent } = useMplChart(url);

    await flush();
    expect(svgContent.value).toBe('<svg>first</svg>');

    url.value = '/api/chart2.svg';
    await flush();

    expect(svgContent.value).toBe('<svg>second</svg>');
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('does not fetch when URL is empty', () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    useMplChart('');

    expect(fetchMock).not.toHaveBeenCalled();
  });
});
