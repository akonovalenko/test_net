import { render, screen, waitFor } from '@testing-library/vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import { describe, expect, it, vi } from 'vitest'

import Home from '../src/views/Home.vue'

function renderHome() {
  return render(Home, {
    global: {
      plugins: [
        createPinia(),
        [PrimeVue, { theme: { preset: Aura } }]
      ]
    }
  })
}

describe('Home', () => {
  it('loads and displays market rates', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        application: 'Market Rate Info',
        status: 'ok',
        database: 'SQLite',
        rates: [
          {
            symbol: 'EUR/USD',
            rate: 1.1,
            updatedAtUtc: '2026-08-21T10:00:00Z'
          }
        ]
      })
    }))

    renderHome()

    expect(screen.getByText('Market Rate Info')).toBeInTheDocument()
    expect(screen.getByText('Application status')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByText('EUR/USD')).toBeInTheDocument()
      expect(screen.getByText('1.1')).toBeInTheDocument()
    })
  })

  it('displays an API error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 500
    }))

    renderHome()

    await waitFor(() => {
      expect(screen.getByText('API request failed: 500')).toBeInTheDocument()
    })
  })
})
