import axios from 'axios'

const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json'
  }
})

let csrfToken: string | null = null
let csrfTokenRequest: Promise<string> | null = null

async function getCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken

  if (!csrfTokenRequest) {
    csrfTokenRequest = apiClient.get<{ token: string }>('/auth/csrf')
      .then(({ data }) => {
        csrfToken = data.token
        return data.token
      })
      .finally(() => {
        csrfTokenRequest = null
      })
  }

  return csrfTokenRequest
}

apiClient.interceptors.request.use(async config => {
  const method = (config.method ?? 'get').toLowerCase()
  if (['post', 'put', 'patch', 'delete'].includes(method) &&
      !config.url?.endsWith('/auth/csrf')) {
    config.headers.set('X-CSRF-TOKEN', await getCsrfToken())
  }

  return config
})

export default apiClient
