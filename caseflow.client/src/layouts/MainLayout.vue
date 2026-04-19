<template>
  <!-- Root: single col on mobile, [260px + 1fr] on desktop -->
  <div class="grid min-h-screen lg:grid-cols-[260px_1fr]">

    <!-- ═══ SIDEBAR (hidden on mobile, flex column on desktop) ═══ -->
    <aside class="hidden lg:flex flex-col w-64 bg-slate-900 text-white sticky top-0 h-screen overflow-y-auto">
      <!-- Brand -->
      <div class="flex items-center gap-3 px-5 py-5 border-b border-slate-700/60">
        <div class="flex items-center justify-center w-9 h-9 rounded-xl bg-indigo-500 shrink-0">
          <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
          </svg>
        </div>
        <div class="min-w-0">
          <p class="text-sm font-bold text-white leading-tight truncate">叫修管理系統</p>
          <p class="text-xs text-slate-400">CaseFlow</p>
        </div>
      </div>

      <!-- Nav -->
      <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <router-link v-for="item in menuItems" :key="item.path" :to="item.path"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150 select-none"
          :class="isActive(item.path)
            ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-600/30'
            : 'text-slate-300 hover:bg-slate-800 hover:text-white'">
          <span class="text-base w-5 text-center shrink-0">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
          <span v-if="item.path === '/notifications' && unreadCount > 0"
            class="ml-auto bg-red-500 text-white text-xs rounded-full px-1.5 py-0.5 leading-none">
            {{ unreadCount > 9 ? '9+' : unreadCount }}
          </span>
        </router-link>
      </nav>

      <!-- User footer -->
      <div class="px-3 py-4 border-t border-slate-700/60 space-y-1">
        <div class="flex items-center gap-3 px-3 py-2">
          <div class="w-8 h-8 rounded-full bg-indigo-500/30 flex items-center justify-center text-sm font-bold text-indigo-300 shrink-0">
            {{ auth.user?.full_name?.[0] || 'U' }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-white truncate">{{ auth.user?.full_name || auth.user?.username }}</p>
            <p class="text-xs text-slate-400 truncate">{{ roleLabel }}</p>
          </div>
        </div>
        <button @click="handleLogout"
          class="w-full flex items-center gap-2 px-3 py-2 text-sm text-red-400 hover:bg-red-500/10 rounded-lg transition-colors">
          <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
          </svg>
          登出
        </button>
      </div>
    </aside>

    <!-- ─── Main Content Area ─────────────────────────────── -->
    <div class="flex flex-col min-h-screen">

      <!-- Mobile header (< lg) -->
      <header class="lg:hidden flex items-center justify-between px-4 h-14 bg-white border-b border-gray-200 shrink-0">
        <button @click="mobileMenuOpen = !mobileMenuOpen" class="p-2 rounded-lg hover:bg-gray-100 text-gray-600">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
          </svg>
        </button>
        <span class="text-sm font-bold text-indigo-600">叫修管理系統</span>
        <router-link to="/notifications" class="relative p-2 rounded-lg hover:bg-gray-100 text-gray-600">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
          </svg>
          <span v-if="unreadCount > 0" class="absolute -top-0.5 -right-0.5 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center leading-none">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
        </router-link>
      </header>

      <!-- Mobile slide-in menu -->
      <transition name="slide">
        <div v-if="mobileMenuOpen" class="lg:hidden fixed inset-0 z-50 flex" @click="mobileMenuOpen = false">
          <aside class="w-72 h-full bg-slate-900 shadow-xl flex flex-col" @click.stop>
            <div class="flex items-center gap-3 px-5 py-5 border-b border-slate-700/60">
              <div class="w-8 h-8 rounded-xl bg-indigo-500 flex items-center justify-center shrink-0">
                <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                </svg>
              </div>
              <span class="text-sm font-bold text-white">叫修管理系統</span>
            </div>
            <nav class="flex-1 px-3 py-4 space-y-1">
              <router-link v-for="item in menuItems" :key="item.path" :to="item.path" @click="mobileMenuOpen = false"
                class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium"
                :class="isActive(item.path) ? 'bg-indigo-600 text-white' : 'text-slate-300 hover:bg-slate-800 hover:text-white'">
                <span class="text-base w-5 text-center">{{ item.icon }}</span>
                <span>{{ item.label }}</span>
              </router-link>
            </nav>
          </aside>
          <div class="flex-1 bg-black/50"></div>
        </div>
      </transition>

      <!-- Desktop topbar (lg+) -->
      <header class="hidden lg:flex items-center justify-between px-8 h-16 bg-white border-b border-gray-200 shrink-0">
        <h1 class="text-lg font-semibold text-gray-900">{{ currentPageTitle }}</h1>
        <div class="flex items-center gap-4">
          <router-link to="/notifications"
            class="relative flex items-center justify-center w-9 h-9 rounded-lg hover:bg-gray-100 text-gray-500 transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
            </svg>
            <span v-if="unreadCount > 0"
              class="absolute -top-0.5 -right-0.5 min-w-[1.1rem] h-[1.1rem] bg-red-500 text-white text-xs rounded-full flex items-center justify-center px-1 leading-none">
              {{ unreadCount > 9 ? '9+' : unreadCount }}
            </span>
          </router-link>
          <div class="flex items-center gap-2 pl-4 border-l border-gray-200">
            <div class="w-8 h-8 rounded-full bg-indigo-100 flex items-center justify-center text-xs font-bold text-indigo-700">
              {{ auth.user?.full_name?.[0] || 'U' }}
            </div>
            <span class="text-sm font-medium text-gray-700">{{ auth.user?.full_name || auth.user?.username }}</span>
          </div>
        </div>
      </header>

      <!-- Main content -->
      <main class="flex-1 bg-slate-50 p-6 lg:p-10 overflow-y-auto">
        <div class="w-full">
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

const menuItems = [
  { path: '/dashboard', label: '儀表板', icon: '📊' },
  { path: '/cases', label: '案件管理', icon: '📋' },
  { path: '/reports/hours', label: '工時統計', icon: '⏱️' },
  { path: '/reports/cases', label: '案件統計', icon: '📈' },
  { path: '/notifications', label: '通知中心', icon: '🔔' },
  { path: '/profile', label: '個人設定', icon: '👤' }
]

function isActive(path) {
  if (path === '/dashboard') return route.path === '/dashboard' || route.path === '/'
  return route.path === path || route.path.startsWith(path + '/')
}

const currentPageTitle = computed(() => {
  const item = menuItems.find(m => isActive(m.path))
  return item?.label || '叫修管理系統'
})

const roleLabel = computed(() =>
  ({ SysAdmin: '系統管理員', PM: '專案經理', SE: '工程師' }[auth.user?.role] || auth.user?.role || '')
)

const mobileMenuOpen = ref(false)
const unreadCount = ref(0)
let pollTimer = null

async function fetchUnread() {
  try {
    const { data: res } = await api.get('/notifications', { params: { is_read: false, page_size: 1 } })
    if (res.success) unreadCount.value = res.meta?.total ?? 0
  } catch { /* ignore */ }
}

function handleLogout() {
  auth.logout()
  router.push('/login')
}

onMounted(async () => {
  if (!meta.loaded) await meta.fetchDropdowns()
  if (auth.token && !auth.user?.email) {
    try { await auth.fetchMe() } catch { /* ignore */ }
  }
  fetchUnread()
  pollTimer = setInterval(fetchUnread, 30000)
})

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer)
})
</script>

<style scoped>
.slide-enter-active, .slide-leave-active { transition: opacity 0.2s; }
.slide-enter-from, .slide-leave-to { opacity: 0; }
</style>
