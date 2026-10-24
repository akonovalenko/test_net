export interface MarketRate {
  symbol: string
  rate: number
  updatedAtUtc: string
}

export interface HomeResponse {
  application: string
  status: string
  database: string
  rates: MarketRate[]
}

export interface MarketRateQuery {
  symbol: string
  from: string
  to: string
}

export interface MarketRateRefreshResult {
  providersUsed: number
  ratesSaved: number
  refreshedAtUtc: string
}
