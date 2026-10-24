import { computed, ref } from 'vue'
import type { Ref } from 'vue'
import { useAuthStore } from '@/stores/auth'

export type RouteName = 'home' | 'login' | 'register' | 'access-denied' | 'admin-users'

const routePath = ref(window.location.pathname || '/')

function normalize(path: string): string {
  if (!path || path === '/') return '/'
  return path.replace(/\/$/, '')
}

function getRouteName(path: string): RouteName {
  const pathname = new URL(path, window.location.origin).pathname
  switch (normalize(pathname)) {
    case '/login': return 'login'
    case '/register': return 'register'
    case '/access-denied': return 'access-denied'
    case '/admin/users': return 'admin-users'
    default: return 'home'
  }
}

export function useSpaRouter(): {
  path: Ref<string>
  routeName: Ref<RouteName>
  push: (path: string) => Promise<void>
} {
  const routeName = computed<RouteName>(() => getRouteName(routePath.value))

  async function push(path: string): Promise<void> {
    const target = path.startsWith('/') ? path : '/'
    const pathname = normalize(new URL(target, window.location.origin).pathname)
    const auth = useAuthStore()

    if (pathname !== '/login' && pathname !== '/register' && !auth.isAuthenticated) {
      history.pushState({}, '', `/login?returnUrl=${encodeURIComponent(pathname)}`)
      routePath.value = '/login'
      return
    }

    if ((pathname === '/login' || pathname === '/register') && auth.isAuthenticated) {
      history.pushState({}, '', '/')
      routePath.value = '/'
      return
    }

    if (pathname === '/admin/users' && !auth.isAdmin) {
      history.pushState({}, '', '/access-denied')
      routePath.value = '/access-denied'
      return
    }

    history.pushState({}, '', target)
    routePath.value = pathname
  }

  return { path: routePath, routeName, push }
}

export async function initializeSpaRouter(): Promise<void> {
  const auth = useAuthStore()
  await auth.initialize()

  const path = normalize(window.location.pathname)
  const guestRoute = path === '/login' || path === '/register'
  const publicRoute = guestRoute || path === '/access-denied'

  if (!auth.isAuthenticated && !guestRoute) {
    const returnUrl = encodeURIComponent(path + window.location.search)
    history.replaceState({}, '', `/login?returnUrl=${returnUrl}`)
    routePath.value = '/login'
    return
  }

  if (auth.isAuthenticated && path === '/admin/users' && !auth.isAdmin) {
    history.replaceState({}, '', '/access-denied')
    routePath.value = '/access-denied'
    return
  }

  if (auth.isAuthenticated && guestRoute) {
    history.replaceState({}, '', '/')
    routePath.value = '/'
    return
  }

  if (!publicRoute && path !== '/') {
    history.replaceState({}, '', '/')
    routePath.value = '/'
  }
}

export function subscribeToBrowserNavigation(callback: () => void): () => void {
  const handler = () => {
    routePath.value = normalize(window.location.pathname)
    callback()
  }

  window.addEventListener('popstate', handler)
  return () => window.removeEventListener('popstate', handler)
}
