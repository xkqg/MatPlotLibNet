// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useMplLiveChart } from '../lib/use-mpl-live-chart';

const mockConnect = vi.fn().mockResolvedValue(undefined);
const mockSubscribe = vi.fn().mockResolvedValue(undefined);
const mockUnsubscribe = vi.fn().mockResolvedValue(undefined);
const mockDispose = vi.fn().mockResolvedValue(undefined);
const mockOnSvgUpdated = vi.fn();

vi.mock('../lib/chart-subscription.client', () => ({
  ChartSubscriptionClient: vi.fn().mockImplementation(() => ({
    connect: mockConnect,
    subscribe: mockSubscribe,
    unsubscribe: mockUnsubscribe,
    dispose: mockDispose,
    onSvgUpdated: mockOnSvgUpdated,
    onChartUpdated: vi.fn(),
    isConnected: true,
  })),
}));

describe('useMplLiveChart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('connects to the hub on mount', async () => {
    renderHook(() => useMplLiveChart('chart-1'));

    await vi.waitFor(() => {
      expect(mockConnect).toHaveBeenCalledWith('/charts-hub');
    });
  });

  it('subscribes to the chartId after connecting', async () => {
    renderHook(() => useMplLiveChart('sensor-42', '/my-hub'));

    await vi.waitFor(() => {
      expect(mockConnect).toHaveBeenCalledWith('/my-hub');
      expect(mockSubscribe).toHaveBeenCalledWith('sensor-42');
    });
  });

  it('registers an SVG update handler', async () => {
    renderHook(() => useMplLiveChart('chart-1'));

    await vi.waitFor(() => {
      expect(mockOnSvgUpdated).toHaveBeenCalledWith(expect.any(Function));
    });
  });

  it('updates svgContent when matching chartId update arrives', async () => {
    const { result } = renderHook(() => useMplLiveChart('chart-1'));

    await vi.waitFor(() => {
      expect(mockOnSvgUpdated).toHaveBeenCalled();
    });

    const handler = mockOnSvgUpdated.mock.calls[0][0];
    act(() => {
      handler('chart-1', '<svg>updated</svg>');
    });

    expect(result.current.svgContent).toBe('<svg>updated</svg>');
  });

  it('ignores updates for different chartId', async () => {
    const { result } = renderHook(() => useMplLiveChart('chart-1'));

    await vi.waitFor(() => {
      expect(mockOnSvgUpdated).toHaveBeenCalled();
    });

    const handler = mockOnSvgUpdated.mock.calls[0][0];
    act(() => {
      handler('other-chart', '<svg>wrong</svg>');
    });

    expect(result.current.svgContent).toBe('');
  });

  it('cleans up on unmount', async () => {
    const { unmount } = renderHook(() => useMplLiveChart('chart-1'));

    await vi.waitFor(() => {
      expect(mockSubscribe).toHaveBeenCalled();
    });

    unmount();

    await vi.waitFor(() => {
      expect(mockUnsubscribe).toHaveBeenCalledWith('chart-1');
      expect(mockDispose).toHaveBeenCalled();
    });
  });
});
