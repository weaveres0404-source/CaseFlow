<template>
  <div class="min-h-screen bg-slate-50 text-slate-800">
    <header class="sticky top-0 z-30 border-b border-slate-200 bg-white/90 backdrop-blur">
      <div class="flex h-14 items-center gap-3 px-4">
        <button @click="toggleNav" class="rounded-md p-2 text-slate-600 hover:bg-slate-100" aria-label="切換選單">
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
          </svg>
        </button>

        <router-link to="/dashboard" class="flex shrink-0 items-center gap-2.5">
          <div class="grid h-8 w-8 place-items-center rounded-lg bg-indigo-700 text-white shadow-sm shadow-indigo-700/20">
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h10M4 18h7" />
              <circle cx="18" cy="18" r="3" stroke-width="2" />
            </svg>
          </div>
          <div class="hidden leading-tight sm:block">
            <div class="text-sm font-semibold tracking-tight text-slate-900">CaseFlow</div>
            <div class="text-[10px] text-slate-400">任務追蹤系統</div>
          </div>
        </router-link>

        <div class="flex-1"></div>

        <div class="hidden items-center gap-1 rounded-lg bg-amber-50 p-1 text-xs ring-1 ring-amber-200 xl:flex">
          <span class="rounded bg-amber-600 px-1.5 py-0.5 text-[10px] text-white">目前角色</span>
          <span class="rounded bg-white px-2.5 py-1 font-medium text-slate-900 shadow-sm">{{ roleShortLabel }}</span>
        </div>

        <router-link
          to="/cases"
          class="hidden h-9 items-center gap-2 rounded-lg border border-slate-200 bg-white pl-2.5 pr-2 text-xs text-slate-500 hover:border-slate-300 md:inline-flex"
        >
          <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 10.5a6.5 6.5 0 11-13 0 6.5 6.5 0 0113 0z" />
          </svg>
          <span>快速搜尋案件</span>
          <span class="ml-2 rounded bg-slate-100 px-1 text-[10px] text-slate-500">Ctrl+K</span>
        </router-link>

        <router-link
          to="/notifications"
          class="relative grid h-9 w-9 place-items-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100"
          aria-label="通知中心"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M14.25 18.75a2.25 2.25 0 01-4.5 0M4.5 16.5h15c-.75-.75-1.5-2.625-1.5-6a6 6 0 10-12 0c0 3.375-.75 5.25-1.5 6z" />
          </svg>
          <span
            v-if="unreadCount > 0"
            class="absolute right-0.5 top-0.5 flex min-w-[1rem] items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] leading-4 text-white"
          >
            {{ unreadCount > 99 ? '99+' : unreadCount }}
          </span>
        </router-link>

        <router-link to="/profile" class="flex items-center gap-2 rounded-lg p-1 pr-2.5 hover:bg-slate-100">
          <div class="grid h-8 w-8 place-items-center rounded-full bg-gradient-to-br from-indigo-500 to-indigo-700 text-sm font-semibold text-white">
            {{ userInitial }}
          </div>
          <div class="hidden text-left leading-tight sm:block">
            <div class="text-xs font-medium text-slate-900">{{ displayName }}</div>
            <div class="text-[10px] text-slate-500">{{ roleLabel }}</div>
          </div>
          <svg class="hidden h-3.5 w-3.5 text-slate-400 sm:block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 9l6 6 6-6" />
          </svg>
        </router-link>
      </div>
    </header>

    <div class="flex">
      <aside
        class="hidden shrink-0 border-r border-slate-200 bg-white transition-[width] duration-200 lg:block"
        :class="desktopNavCollapsed ? 'w-14' : 'w-48'"
      >
        <nav class="sticky top-14 min-h-[calc(100vh-3.5rem)] px-2.5 py-3 text-sm">
          <div class="px-3 pb-1 pt-2 text-[10px] font-medium uppercase tracking-wider text-slate-400" :class="desktopNavCollapsed ? 'hidden' : ''">工作區</div>

          <div class="space-y-1">
            <router-link
              v-for="item in primaryItems"
              :key="item.path"
              :to="item.path"
              class="group relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition"
              :class="navItemClass(item.path)"
            >
              <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
              <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
              </svg>
              <span v-if="!desktopNavCollapsed" class="flex-1">{{ item.label }}</span>
            </router-link>
          </div>

          <div class="mt-3">
            <div class="group relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm" :class="reportsGroupActive ? 'bg-indigo-50 font-medium text-indigo-800' : 'text-slate-600'">
              <span v-if="reportsGroupActive" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
              <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('reports')" />
              </svg>
              <span v-if="!desktopNavCollapsed" class="flex-1">統計報表</span>
            </div>

            <div v-if="!desktopNavCollapsed" class="mt-1 space-y-1">
              <router-link
                v-for="item in reportItems"
                :key="item.path"
                :to="item.path"
                class="group relative flex items-center gap-3 rounded-lg px-3 py-2 pl-10 text-sm transition"
                :class="navItemClass(item.path)"
              >
                <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
                <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
                </svg>
                <span class="flex-1">{{ item.label }}</span>
              </router-link>
            </div>
          </div>

          <div class="px-3 pb-1 pt-4 text-[10px] font-medium uppercase tracking-wider text-slate-400" :class="desktopNavCollapsed ? 'hidden' : ''">系統</div>

          <div class="space-y-1">
            <router-link
              v-for="item in systemItems"
              :key="item.path"
              :to="item.path"
              class="group relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition"
              :class="navItemClass(item.path)"
            >
              <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
              <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
              </svg>
              <span v-if="!desktopNavCollapsed" class="flex-1">{{ item.label }}</span>
              <span
                v-if="item.path === '/notifications' && unreadCount > 0 && !desktopNavCollapsed"
                class="rounded-full bg-rose-500 px-1.5 py-0.5 text-[10px] leading-none text-white"
              >
                {{ unreadCount > 99 ? '99+' : unreadCount }}
              </span>
            </router-link>
          </div>

          <div v-if="!desktopNavCollapsed" class="mx-1 mt-6 rounded-lg border border-slate-200 bg-slate-50 p-3">
            <div class="text-[10px] font-medium uppercase tracking-wider text-slate-400">登入資訊</div>
            <div class="mt-2 text-sm font-medium text-slate-900">{{ displayName }}</div>
            <div class="text-[11px] text-slate-500">{{ roleLabel }}</div>
            <button @click="handleLogout" class="mt-3 inline-flex items-center gap-1.5 text-xs font-medium text-rose-600 hover:text-rose-700">
              <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" />
              </svg>
              登出
            </button>
          </div>
        </nav>
      </aside>

      <transition name="fade">
        <div v-if="mobileMenuOpen" class="fixed inset-0 z-40 lg:hidden" @click="closeMobileMenu">
          <div class="absolute inset-0 bg-slate-900/35"></div>
          <aside class="relative h-full w-72 border-r border-slate-200 bg-white shadow-xl" @click.stop>
            <div class="flex items-center gap-2.5 border-b border-slate-200 px-4 py-4">
              <div class="grid h-8 w-8 place-items-center rounded-lg bg-indigo-700 text-white shadow-sm shadow-indigo-700/20">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h10M4 18h7" />
                  <circle cx="18" cy="18" r="3" stroke-width="2" />
                </svg>
              </div>
              <div class="leading-tight">
                <div class="text-sm font-semibold tracking-tight text-slate-900">CaseFlow</div>
                <div class="text-[10px] text-slate-400">任務追蹤系統</div>
              </div>
            </div>

            <nav class="px-2.5 py-3 text-sm">
              <div class="px-3 pb-1 pt-2 text-[10px] font-medium uppercase tracking-wider text-slate-400">工作區</div>

              <div class="space-y-1">
                <router-link
                  v-for="item in primaryItems"
                  :key="`mobile-${item.path}`"
                  :to="item.path"
                  class="group relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition"
                  :class="navItemClass(item.path)"
                  @click="closeMobileMenu"
                >
                  <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
                  <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
                  </svg>
                  <span class="flex-1">{{ item.label }}</span>
                </router-link>
              </div>

              <div class="mt-3">
                <div class="relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm" :class="reportsGroupActive ? 'bg-indigo-50 font-medium text-indigo-800' : 'text-slate-600'">
                  <span v-if="reportsGroupActive" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
                  <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('reports')" />
                  </svg>
                  <span class="flex-1">統計報表</span>
                </div>

                <div class="mt-1 space-y-1">
                  <router-link
                    v-for="item in reportItems"
                    :key="`mobile-${item.path}`"
                    :to="item.path"
                    class="group relative flex items-center gap-3 rounded-lg px-3 py-2 pl-10 text-sm transition"
                    :class="navItemClass(item.path)"
                    @click="closeMobileMenu"
                  >
                    <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
                    <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
                    </svg>
                    <span class="flex-1">{{ item.label }}</span>
                  </router-link>
                </div>
              </div>

              <div class="px-3 pb-1 pt-4 text-[10px] font-medium uppercase tracking-wider text-slate-400">系統</div>

              <div class="space-y-1">
                <router-link
                  v-for="item in systemItems"
                  :key="`mobile-${item.path}`"
                  :to="item.path"
                  class="group relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition"
                  :class="navItemClass(item.path)"
                  @click="closeMobileMenu"
                >
                  <span v-if="isActive(item.path)" class="absolute inset-y-1.5 left-0 w-0.5 rounded-r bg-indigo-700"></span>
                  <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
                  </svg>
                  <span class="flex-1">{{ item.label }}</span>
                  <span
                    v-if="item.path === '/notifications' && unreadCount > 0"
                    class="rounded-full bg-rose-500 px-1.5 py-0.5 text-[10px] leading-none text-white"
                  >
                    {{ unreadCount > 99 ? '99+' : unreadCount }}
                  </span>
                </router-link>
              </div>

              <div class="mx-1 mt-6 rounded-lg border border-slate-200 bg-slate-50 p-3">
                <div class="text-[10px] font-medium uppercase tracking-wider text-slate-400">登入資訊</div>
                <div class="mt-2 text-sm font-medium text-slate-900">{{ displayName }}</div>
                <div class="text-[11px] text-slate-500">{{ roleLabel }}</div>
                <button @click="handleLogout" class="mt-3 inline-flex items-center gap-1.5 text-xs font-medium text-rose-600 hover:text-rose-700">
                  <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" />
                  </svg>
                  登出
                </button>
              </div>
            </nav>
          </aside>
        </div>
      </transition>

      <main class="min-w-0 flex-1 overflow-x-hidden">
        <div class="px-5 py-6 lg:px-6">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const meta = useMetaStore()

const mobileMenuOpen = ref(false)
const desktopNavCollapsed = ref(false)
const unreadCount = ref(0)
let pollTimer = null

const primaryItems = computed(() => {
  const items = [
    { path: '/dashboard', label: '儀表板', icon: 'dashboard' },
    { path: '/cases', label: '案件列表', icon: 'cases' }
  ]

  if (auth.user?.role !== 'SE') {
    items.push({ path: '/cases/new', label: '新增案件', icon: 'new' })
  }

  return items
})

const reportItems = [
  { path: '/reports/hours', label: '工時統計', icon: 'hours' },
  { path: '/reports/cases', label: '案件數量統計', icon: 'casesReport' }
]

const systemItems = [
  { path: '/notifications', label: '通知中心', icon: 'notifications' },
  { path: '/profile', label: '個人設定', icon: 'profile' }
]

const roleLabel = computed(() =>
  ({ ADMIN: '系統管理員', SysAdmin: '系統管理員', PM: '專案經理', SE: '工程師' }[auth.user?.role] || auth.user?.role || '未登入')
)

const roleShortLabel = computed(() => auth.user?.role || 'Guest')
const displayName = computed(() => auth.user?.full_name || auth.user?.username || '使用者')
const userInitial = computed(() => (displayName.value || 'U').trim().charAt(0).toUpperCase())
const reportsGroupActive = computed(() => reportItems.some((item) => isActive(item.path)))

function isActive(path) {
  if (path === '/dashboard') return route.path === '/' || route.path === '/dashboard'
  return route.path === path || route.path.startsWith(path + '/')
}

function navItemClass(path) {
  return isActive(path)
    ? 'bg-indigo-50 font-medium text-indigo-800'
    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
}

function iconPath(icon) {
  return {
    dashboard: 'M3.75 4.5h6.75v6.75H3.75V4.5zm9.75 0h6.75V9h-6.75V4.5zM3.75 14.25h6.75V21H3.75v-6.75zm9.75-3.75h6.75V21h-6.75V10.5z',
    cases: 'M8.25 3.75h7.5v1.5h.75A2.25 2.25 0 0118.75 7.5v10.5a2.25 2.25 0 01-2.25 2.25h-9A2.25 2.25 0 015.25 18V7.5A2.25 2.25 0 017.5 5.25h.75v-1.5zm-1.5 6h10.5m-10.5 3h6.75m-6.75 3h8.25',
    new: 'M12 9v6m3-3H9m11.25 0a8.25 8.25 0 11-16.5 0 8.25 8.25 0 0116.5 0z',
    reports: 'M6.75 3.75h7.5l3.75 3.75v9.75a2.25 2.25 0 01-2.25 2.25h-9A2.25 2.25 0 014.5 17.25V6A2.25 2.25 0 016.75 3.75zm1.5 10.5V16.5m3-4.5v4.5m3-7.5v7.5',
    hours: 'M12 7.5v4.5l3 1.5m6-1.5a9 9 0 11-18 0 9 9 0 0118 0z',
    casesReport: 'M4.5 19.5h15M7.5 16.5V10.5m4.5 6V7.5m4.5 9v-3.75',
    notifications: 'M14.25 18.75a2.25 2.25 0 01-4.5 0M4.5 16.5h15c-.75-.75-1.5-2.625-1.5-6a6 6 0 10-12 0c0 3.375-.75 5.25-1.5 6z',
    profile: 'M15.75 6.75a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.5 20.118a7.5 7.5 0 0115 0A17.93 17.93 0 0112 21.75c-2.676 0-5.216-.584-7.5-1.632z',
    logout: 'M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6A2.25 2.25 0 005.25 5.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M18 12H9m9 0l-3-3m3 3l-3 3'
  }[icon] || ''
}

function toggleNav() {
  if (typeof window !== 'undefined' && window.innerWidth < 1024) {
    mobileMenuOpen.value = !mobileMenuOpen.value
    return
  }

  desktopNavCollapsed.value = !desktopNavCollapsed.value
}

function closeMobileMenu() {
  mobileMenuOpen.value = false
}

async function fetchUnread() {
  try {
    const { data: res } = await api.get('/notifications', { params: { is_read: false, page_size: 1 } })
    if (res.success) unreadCount.value = res.meta?.total ?? 0
  } catch {
    // ignore fetch errors to keep layout stable
  }
}

function handleLogout() {
  mobileMenuOpen.value = false
  auth.logout()
  router.push('/login')
}

onMounted(async () => {
  if (!meta.loaded) {
    try {
      await meta.fetchDropdowns()
    } catch {
      // 401 handled by api interceptor
    }
  }

  if (auth.token && !auth.user?.email) {
    try {
      await auth.fetchMe()
    } catch {
      // ignore current-user refresh errors
    }
  }

  fetchUnread()
  pollTimer = setInterval(fetchUnread, 30000)
})

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer)
})
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
