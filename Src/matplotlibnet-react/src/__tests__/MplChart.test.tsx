// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { MplChart } from '../lib/MplChart';

describe('MplChart', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('renders with mpl-chart class', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg>test</svg>'),
    }));

    const { container } = render(<MplChart chartUrl="/api/chart.svg" />);

    expect(container.querySelector('.mpl-chart')).not.toBeNull();
  });

  it('renders SVG content after fetch', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg><rect width="100" height="50"/></svg>'),
    }));

    const { container } = render(<MplChart chartUrl="/api/chart.svg" />);

    await waitFor(() => {
      expect(container.innerHTML).toContain('<svg>');
      expect(container.innerHTML).toContain('<rect');
    });
  });

  it('applies cssClass to the container', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg/>'),
    }));

    const { container } = render(<MplChart chartUrl="/api/chart.svg" cssClass="my-chart" />);

    expect(container.querySelector('.mpl-chart.my-chart')).not.toBeNull();
  });
});
