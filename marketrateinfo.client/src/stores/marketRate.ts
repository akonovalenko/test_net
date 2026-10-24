import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  addSymbol,
  RemoveCurrencyPair as removeSymbol,
  getHome,
  getMarketRates,
  getSymbols,
  refreshRate
} from '@/api/homeApi'
import type { MarketRate } from '@/types/marketRate'

export type AppStatus = 'idle' | 'loading' | 'ok' | 'error'

export const useMarketRateStore = defineStore('marketRate', () => {
  const applicationName = ref('Market Rate Info')
  const status = ref<AppStatus>('idle')
  const rates = ref<MarketRate[]>([])
  const symbols = ref<string[]>([])
  const error = ref<string | null>(null)
  const lastRefresh = ref<{ ratesSaved: number; providersUsed: number; refreshedAtUtc: string } | null>(null)

  let homeController: AbortController | null = null
  let symbolsController: AbortController | null = null
  let ratesController: AbortController | null = null
  let addSymbolController: AbortController | null = null
  let refreshRateController: AbortController | null = null
  let removeSymbolController: AbortController | null = null

  async function loadHome(): Promise<void> {
    if (homeController) {
      homeController.abort()
    }
    homeController = new AbortController()

    status.value = 'loading'
    error.value = null

    try {
      const data = await getHome(homeController.signal)

      applicationName.value = data.application ?? applicationName.value
      rates.value = data.rates ?? []
      status.value = data.status === 'ok' ? 'ok' : 'error'
    } catch (err: unknown) {
      if (isAbortError(err)) return

      status.value = 'error'
      error.value = getErrorMessage(err)
    } finally {
      homeController = null
    }
  }

  async function loadSymbols(): Promise<void> {
    if (symbolsController) {
      symbolsController.abort()
    }
    symbolsController = new AbortController()

    error.value = null

    try {
      symbols.value = await getSymbols(symbolsController.signal)
    } catch (err: unknown) {
      if (isAbortError(err)) return

      error.value = getErrorMessage(err)
    } finally {
      symbolsController = null
    }
  }

  async function loadRates(
    symbol: string,
    from: Date,
    to: Date
  ): Promise<void> {
    if (ratesController) {
      ratesController.abort()
    }
    ratesController = new AbortController()

    status.value = 'loading'
    error.value = null

    try {
      rates.value = await getMarketRates(
        {
          symbol,
          from: formatDate(from),
          to: formatDate(to)
        },
        ratesController.signal
      )

      status.value = 'ok'
    } catch (err: unknown) {
      if (isAbortError(err)) return

      status.value = 'error'
      error.value = getErrorMessage(err)
    } finally {
      ratesController = null
    }
  }

  async function refreshSingleRate(
    symbol?: string,
    from?: Date,
    to?: Date
  ): Promise<void> {
    if (refreshRateController) {
      refreshRateController.abort()
    }
    refreshRateController = new AbortController()

    status.value = 'loading'
    error.value = null

    try {
      const result = await refreshRate(
        from ? formatDate(from) : undefined,
        to ? formatDate(to) : undefined,
        symbol,
        refreshRateController.signal
      )

      lastRefresh.value = result

      // The refresh endpoint updates SQLite. Reload the selected range only
      // after the server confirms that the database update has completed.
      if (symbol && from && to) {
        await loadRates(symbol, from, to)
      } else {
        await loadHome()
      }
    } catch (err: unknown) {
      if (isAbortError(err)) return

      status.value = 'error'
      error.value = getErrorMessage(err)
    } finally {
      refreshRateController = null
    }
  }

  async function addNewSymbol(symbol: string): Promise<void> {
    if (addSymbolController) {
      addSymbolController.abort()
    }
    addSymbolController = new AbortController()

    status.value = 'loading'
    error.value = null

    try {
      await addSymbol(symbol, addSymbolController.signal)
      await loadSymbols()
      status.value = 'ok'
    } catch (err: unknown) {
      if (isAbortError(err)) return

      status.value = 'error'
      error.value = getErrorMessage(err)
      throw err
    } finally {
      addSymbolController = null
    }
  }

  async function RemoveCurrencyPair(symbol: string): Promise<boolean> {
    if (removeSymbolController) {
      removeSymbolController.abort()
    }
    removeSymbolController = new AbortController()

    status.value = 'loading'
    error.value = null

    try {
      await removeSymbol(symbol, removeSymbolController.signal) // вызов API/стора на удаление
      await loadSymbols() // обновляем список доступных пар
      status.value = 'ok'
      return true;
    } catch (err: unknown) {
      if (isAbortError(err)) return false

      status.value = 'error'
      error.value = getErrorMessage(err)
      throw err
    } finally {
      removeSymbolController = null
    }
  }

  function isAbortError(err: unknown): boolean {
    return err instanceof Error && err.message === 'Request was cancelled.'
  }

  function formatDate(date: Date): string {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')

    return `${year}-${month}-${day}`
  }

  function getErrorMessage(err: unknown): string {
    return err instanceof Error
      ? err.message
      : 'Unknown error'
  }

  return {
    applicationName,
    status,
    rates,
    symbols,
    error,
    lastRefresh,
    loadHome,
    loadSymbols,
    loadRates,
    addNewSymbol,
    refreshSingleRate,
    RemoveCurrencyPair
  }
})
