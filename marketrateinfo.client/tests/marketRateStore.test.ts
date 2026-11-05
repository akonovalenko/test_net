import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

import {
  addSymbol,
  getHome,
  getMarketRates,
  getSymbols,
  refreshRate,
  RemoveCurrencyPair
} from '../src/api/homeApi'
import { useMarketRateStore } from '../src/stores/marketRate'

vi.mock('../src/api/homeApi', () => ({
  addSymbol: vi.fn(),
  getHome: vi.fn(),
  getMarketRates: vi.fn(),
  getSymbols: vi.fn(),
  refreshRate: vi.fn(),
  RemoveCurrencyPair: vi.fn()
}))

const homeResponse = {
  application: 'Market Rate Info',
  status: 'ok',
  database: 'SQLite',
  rates: [
    {
      symbol: 'EUR/USD',
      rate: 1.1,
      updatedAtUtc: '2026-01-05T10:00:00Z'
    }
  ]
}

describe('market rate store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('loads home data and updates application state', async () => {
    vi.mocked(getHome).mockResolvedValue(homeResponse)
    const store = useMarketRateStore()

    await store.loadHome()

    expect(getHome).toHaveBeenCalledOnce()
    expect(store.applicationName).toBe('Market Rate Info')
    expect(store.status).toBe('ok')
    expect(store.rates).toEqual(homeResponse.rates)
    expect(store.error).toBeNull()
  })

  it('sets an error state when the home request fails', async () => {
    vi.mocked(getHome).mockRejectedValue(new Error('API server is unavailable.'))
    const store = useMarketRateStore()

    await store.loadHome()

    expect(store.status).toBe('error')
    expect(store.error).toBe('API server is unavailable.')
  })

  it('loads history using local calendar dates in yyyy-mm-dd format', async () => {
    const rates = [
      { symbol: 'EUR/USD', rate: 1.2, updatedAtUtc: '2026-01-06T00:00:00Z' }
    ]
    vi.mocked(getMarketRates).mockResolvedValue(rates)
    const store = useMarketRateStore()

    await store.loadRates(
      'EUR/USD',
      new Date(2026, 0, 5, 12),
      new Date(2026, 0, 6, 12)
    )

    expect(getMarketRates).toHaveBeenCalledWith(
      { symbol: 'EUR/USD', from: '2026-01-05', to: '2026-01-06' },
      expect.any(AbortSignal)
    )
    expect(store.rates).toEqual(rates)
    expect(store.status).toBe('ok')
  })

  it('adds a symbol and reloads the symbol list', async () => {
    vi.mocked(addSymbol).mockResolvedValue()
    vi.mocked(getSymbols).mockResolvedValue(['EUR/USD', 'BTC/USD'])
    const store = useMarketRateStore()

    await store.addNewSymbol('BTC/USD')

    expect(addSymbol).toHaveBeenCalledWith('BTC/USD', expect.any(AbortSignal))
    expect(getSymbols).toHaveBeenCalledOnce()
    expect(store.symbols).toEqual(['EUR/USD', 'BTC/USD'])
    expect(store.status).toBe('ok')
  })

  it('removes a symbol and reloads the symbol list', async () => {
    vi.mocked(RemoveCurrencyPair).mockResolvedValue()
    vi.mocked(getSymbols).mockResolvedValue(['EUR/USD'])
    const store = useMarketRateStore()

    await expect(store.RemoveCurrencyPair('BTC/USD')).resolves.toBe(true)

    expect(RemoveCurrencyPair).toHaveBeenCalledWith('BTC/USD', expect.any(AbortSignal))
    expect(store.symbols).toEqual(['EUR/USD'])
    expect(store.status).toBe('ok')
  })

  it('stores refresh result and reloads the home data after a global refresh', async () => {
    const refreshResult = {
      providersUsed: 2,
      ratesSaved: 4,
      refreshedAtUtc: '2026-01-06T10:00:00Z'
    }
    vi.mocked(refreshRate).mockResolvedValue(refreshResult)
    vi.mocked(getHome).mockResolvedValue(homeResponse)
    const store = useMarketRateStore()

    await store.refreshSingleRate()

    expect(refreshRate).toHaveBeenCalledWith(
      undefined,
      undefined,
      undefined,
      expect.any(AbortSignal)
    )
    expect(store.lastRefresh).toEqual(refreshResult)
    expect(store.rates).toEqual(homeResponse.rates)
    expect(store.status).toBe('ok')
  })

  it('rethrows add-symbol errors after recording them in state', async () => {
    vi.mocked(addSymbol).mockRejectedValue(new Error('Symbol already exists.'))
    const store = useMarketRateStore()

    await expect(store.addNewSymbol('EUR/USD')).rejects.toThrow('Symbol already exists.')
    expect(store.status).toBe('error')
    expect(store.error).toBe('Symbol already exists.')
  })
})
