import { defineStore } from 'pinia';
import { ref } from 'vue';
import { getWeather } from '@/api/homeApi';
import type { WeatherForecast } from '@/types/weather';

const WEATHER_REFRESH_INTERVAL = 60 * 60 * 1000; // 1 hour

const FALLBACK_LATITUDE = 50.4501;
const FALLBACK_LONGITUDE = 30.5234;

export const useWeatherStore = defineStore('weather', () => {
  const currentWeather = ref<WeatherForecast | null>(null);
  const weatherFetchedAt = ref<number | null>(null);

  const loading = ref(false);
  const error = ref(false);

  let abortController: AbortController | null = null;
  let refreshTimer: ReturnType<typeof setInterval> | null = null;

  let currentLatitude: number | null = null;
  let currentLongitude: number | null = null;

  const getForecastByCoords = async (
    latitude: number,
    longitude: number
  ): Promise<void> => {

    if (loading.value) {
      return;
    }

    abortController?.abort();

    const controller = new AbortController();
    abortController = controller;

    try {
      loading.value = true;
      error.value = false;

      currentLatitude = latitude;
      currentLongitude = longitude;

      const data = await getWeather(
        {
          latitude,
          longitude
        },
        controller.signal
      );

      if (controller.signal.aborted) {
        return;
      }

      if (data?.length > 0) {
        currentWeather.value = data[0];
        weatherFetchedAt.value = Date.now();
      }
    } catch (e: unknown) {
      if (e instanceof DOMException && e.name === 'AbortError') {
        return;
      }

      if (typeof e === 'object' && e !== null && 'name' in e &&
        (e.name === 'CanceledError' || e.name === 'AbortError')
      ) {
        return;
      }

      console.error('Failed to fetch weather:', e);
      error.value = true;
    } finally {
      if (abortController === controller) {
        abortController = null;
        loading.value = false;
      }
    }
  };

  const updateWeather = async (): Promise<void> => {
    if (
      currentLatitude !== null &&
      currentLongitude !== null
    ) {
      await getForecastByCoords(
        currentLatitude,
        currentLongitude
      );

      return;
    }

    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        async (position) => {
          currentLatitude = position.coords.latitude;
          currentLongitude = position.coords.longitude;

          await getForecastByCoords(
            currentLatitude,
            currentLongitude
          );
        },
        async (geoError) => {
          console.warn(
            'Geolocation failed:',
            geoError.message
          );

          currentLatitude = FALLBACK_LATITUDE;
          currentLongitude = FALLBACK_LONGITUDE;

          await getForecastByCoords(
            FALLBACK_LATITUDE,
            FALLBACK_LONGITUDE
          );
        },
        {
          timeout: 10000
        }
      );

      return;
    }
    currentLatitude = FALLBACK_LATITUDE;
    currentLongitude = FALLBACK_LONGITUDE;

    await getForecastByCoords(
      FALLBACK_LATITUDE,
      FALLBACK_LONGITUDE
    );
  };

  const startAutoRefresh = (): void => {
    if (refreshTimer !== null) {
      return;
    }
    refreshTimer = setInterval(() => {
      void updateWeather();
    }, WEATHER_REFRESH_INTERVAL);
  };

  const stopAutoRefresh = (): void => {
    if (refreshTimer === null) {
      return;
    }
    clearInterval(refreshTimer);
    refreshTimer = null;
  };

  const fetchWeather = async (): Promise<void> => {
    if (!currentWeather.value) {
      await updateWeather();
    }
    startAutoRefresh();
  };

  return {
    currentWeather,
    weatherFetchedAt,
    loading,
    error,
    fetchWeather,
    updateWeather,
    startAutoRefresh,
    stopAutoRefresh
  };
});
