import axios from 'axios'
import apiClient from './axios'

import type {
  HomeResponse,
  MarketRate,
  MarketRateQuery,
  MarketRateRefreshResult
} from '@/types/marketRate'

import type {
  WeatherForecast
} from '@/types/weather'

export async function getHome(
  signal?: AbortSignal
): Promise<HomeResponse> {
  try {
    const response = await apiClient.get<HomeResponse>(
      '/home',
      { signal }
    )

    return response.data
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function getSymbols(
  signal?: AbortSignal
): Promise<string[]> {
  try {
    const response = await apiClient.get<string[]>(
      '/rates/symbols',
      { signal }
    )

    return response.data
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function addSymbol(
  symbol: string,
  signal?: AbortSignal
): Promise<void> {
  try {
    await apiClient.post(
      '/rates/symbols',
      { symbol },
      { signal }
    )
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function RemoveCurrencyPair(
  symbol: string,
  signal?: AbortSignal
): Promise<void> {
  try {
    await apiClient.delete('/rates/symbols', {
      params: { symbol },
      signal
    })
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function getMarketRates(
  query: MarketRateQuery,
  signal?: AbortSignal
): Promise<MarketRate[]> {
  try {
    const response = await apiClient.get<MarketRate[]>(
      '/rates',
      {
        params: query,
        signal
      }
    )

    return response.data
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function refreshRate(
  startDate?: string,
  endDate?: string,
  symbol?: string,
  signal?: AbortSignal
): Promise<MarketRateRefreshResult> {
  try {
    const params: Record<string, string> = {}

    if (startDate) {
      params.startDate = startDate
    }

    if (endDate) {
      params.endDate = endDate
    }

    if (symbol) {
      params.symbol = symbol
    }

    const response = await apiClient.post<MarketRateRefreshResult>(
      '/rates/refresh',
      null,
      {
        params: Object.keys(params).length > 0
          ? params
          : undefined,
        signal
      }
    )

    return response.data
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

export async function getWeather(
  params?: {
    latitude: number
    longitude: number
  },
  signal?: AbortSignal
): Promise<WeatherForecast[]> {
  try {
    const response = await apiClient.get<WeatherForecast[]>(
      '/weatherforecast',
      {
        params,
        signal
      }
    )

    return response.data
  } catch (error: unknown) {
    throw toApiError(error)
  }
}

function toApiError(error: unknown): Error {
  if (axios.isCancel(error)) {
    return new Error('Request was cancelled.')
  }

  if (axios.isAxiosError(error)) {
    if (error.response) {
      const data = error.response.data as
        | { message?: string; title?: string }
        | string
        | undefined

      const message =
        typeof data === 'string'
          ? data
          : data?.message ?? data?.title

      return new Error(
        message
          ? `API request failed (${error.response.status}): ${message}`
          : `API request failed: ${error.response.status}`
      )
    }

    if (error.request) {
      return new Error('API server is unavailable.')
    }
  }

  return error instanceof Error
    ? error
    : new Error('Unknown API error')
}
