// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useMplChart } from '../lib/use-mpl-chart';

describe('useMplChart', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('fetches SVG from the given URL', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg>chart</svg>'),
    }));

    const { result } = renderHook(() => useMplChart('/api/chart.svg'));

    await waitFor(() => expect(result.current.svgContent).toBe('<svg>chart</svg>'));
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('starts with loading true', () => {
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise(() => {})));

    const { result } = renderHook(() => useMplChart('/api/chart.svg'));

    expect(result.current.loading).toBe(true);
    expect(result.current.svgContent).toBe('');
  });

  it('handles fetch errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')));

    const { result } = renderHook(() => useMplChart('/api/chart.svg'));

    await waitFor(() => expect(result.current.error).not.toBeNull());
    expect(result.current.error!.message).toBe('Network error');
    expect(result.current.loading).toBe(false);
  });

  it('re-fetches when URL changes', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ text: () => Promise.resolve('<svg>first</svg>') })
      .mockResolvedValueOnce({ text: () => Promise.resolve('<svg>second</svg>') });
    vi.stubGlobal('fetch', fetchMock);

    const { result, rerender } = renderHook(
      ({ url }) => useMplChart(url),
      { initialProps: { url: '/api/chart1.svg' } }
    );

    await waitFor(() => expect(result.current.svgContent).toBe('<svg>first</svg>'));

    rerender({ url: '/api/chart2.svg' });

    await waitFor(() => expect(result.current.svgContent).toBe('<svg>second</svg>'));
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('does not fetch when URL is empty', () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    renderHook(() => useMplChart(''));

    expect(fetchMock).not.toHaveBeenCalled();
  });
});
