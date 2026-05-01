<template>
  <div class="min-h-screen bg-slate-50 text-slate-800">
    <header class="sticky top-0 z-30 border-b border-slate-200 bg-white/95 backdrop-blur transition-[padding] duration-300" :class="sidebarCollapsed ? 'lg:pl-[68px]' : 'lg:pl-[300px]'">
      <div class="flex h-[64px] items-center justify-between px-4 lg:px-6 gap-4">
        <div class="flex items-center gap-3 min-w-0 flex-1">
          <button @click="toggleNav" class="rounded-xl p-2 text-slate-500 transition-colors hover:bg-slate-100 lg:hidden" aria-label="切換選單">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            </svg>
          </button>
          
          <h1 class="min-w-0 truncate text-[18px] font-medium tracking-tight text-slate-800">{{ pageTitle }}</h1>
        </div>

        <div class="flex items-center gap-5 lg:gap-7 shrink-0">
          <router-link to="/notifications" class="relative text-slate-500 transition-colors hover:text-slate-700">
            <svg class="h-[22px] w-[22px]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M14.25 18.75a2.25 2.25 0 01-4.5 0M4.5 16.5h15c-.75-.75-1.5-2.625-1.5-6a6 6 0 10-12 0c0 3.375-.75 5.25-1.5 6z" />
            </svg>
            <span v-if="unreadCount > 0" class="absolute -right-1 -top-1 grid h-[18px] min-w-[18px] place-items-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white ring-2 ring-white">
              {{ unreadCount > 99 ? '99+' : unreadCount }}
            </span>
          </router-link>

          <div class="hidden h-9 w-px bg-slate-200 sm:block"></div>

          <router-link to="/profile" class="flex items-center gap-3 rounded-full px-1.5 py-1 transition-colors hover:bg-slate-50 group">
            <div class="grid h-10 w-10 place-items-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700 transition-colors group-hover:bg-indigo-200">
              {{ userInitial }}
            </div>
            <div class="hidden items-center gap-2 sm:flex min-w-0">
              <div class="max-w-[160px] truncate text-[15px] font-medium text-slate-700">
                {{ displayName }}
              </div>
            </div>
          </router-link>
        </div>
      </div>
    </header>

    <div class="flex">
      <aside
        class="hidden lg:fixed lg:inset-y-0 lg:left-0 lg:z-40 lg:flex lg:flex-col lg:border-r lg:border-slate-800 lg:bg-[linear-gradient(180deg,#10182f_0%,#151d34_100%)] lg:text-slate-100 lg:shadow-[18px_0_40px_rgba(15,23,42,0.16)] transition-[width] duration-300 overflow-hidden"
        :class="sidebarCollapsed ? 'lg:w-[68px]' : 'lg:w-[300px]'"
      >
        <div class="flex items-center border-b border-white/8 px-3 py-5" :class="sidebarCollapsed ? 'justify-center' : 'gap-4 px-5'">
          <div class="shrink-0 grid h-10 w-10 place-items-center rounded-[14px] bg-gradient-to-br from-indigo-500 to-violet-600 text-white shadow-lg shadow-indigo-950/40">
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016" />
            </svg>
          </div>
          <div v-if="!sidebarCollapsed" class="min-w-0 flex-1 leading-tight overflow-hidden">
            <div class="truncate text-[15px] font-bold tracking-tight text-white">任務追蹤系統</div>
            <div class="mt-0.5 text-[12px] font-medium text-slate-300">CaseFlow</div>
          </div>
          <button v-if="!sidebarCollapsed" @click="sidebarCollapsed = true" class="shrink-0 grid h-7 w-7 place-items-center rounded-lg text-slate-400 transition-colors hover:bg-white/8 hover:text-white" title="收起側欄">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 19l-7-7 7-7M18 19l-7-7 7-7"/></svg>
          </button>
        </div>

        <nav class="flex min-h-0 flex-1 flex-col py-5" :class="sidebarCollapsed ? 'items-center px-2' : 'px-4'">
          <!-- 收起時顯示展開按鈕 -->
          <button v-if="sidebarCollapsed" @click="sidebarCollapsed = false" class="mb-3 grid h-9 w-9 place-items-center rounded-xl text-slate-400 transition-colors hover:bg-white/8 hover:text-white" title="展開側欄">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5l7 7-7 7M6 5l7 7-7 7"/></svg>
          </button>

          <div :class="sidebarCollapsed ? 'flex flex-col items-center gap-1 w-full' : 'space-y-1.5'">
            <router-link
              v-for="item in allNavItems"
              :key="item.path"
              :to="item.path"
              class="relative flex items-center transition-all duration-150"
              :class="[
                sidebarCollapsed
                  ? 'justify-center rounded-xl w-10 h-10'
                  : 'gap-4 rounded-[16px] px-4 py-3.5 text-[15px] font-medium',
                navItemClass(item)
              ]"
              :title="sidebarCollapsed ? item.label : undefined"
            >
              <svg class="shrink-0" :class="sidebarCollapsed ? 'h-5 w-5' : 'h-[20px] w-[20px]'" :style="!isActive(item) ? iconColor(item.path) : {}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
              </svg>
              <span v-if="!sidebarCollapsed" class="flex-1">{{ item.label }}</span>
              <span
                v-if="!sidebarCollapsed && item.path === '/notifications' && unreadCount > 0"
                class="grid h-5 min-w-[20px] place-items-center rounded-full bg-rose-500 px-1.5 text-[10px] font-bold tracking-wider text-white shadow-sm"
              >
                {{ unreadCount > 99 ? '99+' : unreadCount }}
              </span>
              <!-- 收起時通知紅點 -->
              <span v-if="sidebarCollapsed && item.path === '/notifications' && unreadCount > 0" class="absolute right-1 top-1 h-2 w-2 rounded-full bg-rose-500"></span>
            </router-link>
          </div>

          <div class="mt-auto w-full border-t border-white/8 pt-5" :class="sidebarCollapsed ? 'flex flex-col items-center gap-3 px-0' : 'px-2 pb-2'">
            <template v-if="sidebarCollapsed">
              <div class="grid h-9 w-9 place-items-center rounded-full bg-indigo-500/25 text-sm font-semibold text-indigo-200 ring-1 ring-white/10" :title="displayName">
                {{ userInitial }}
              </div>
              <button @click="handleLogout" class="grid h-9 w-9 place-items-center rounded-xl text-rose-500 transition-colors hover:bg-white/8 hover:text-rose-400" title="登出">
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" /></svg>
              </button>
            </template>
            <template v-else>
              <div class="flex items-center gap-3">
                <div class="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-indigo-500/25 text-sm font-semibold text-indigo-200 ring-1 ring-white/10">
                  {{ userInitial }}
                </div>
                <div class="min-w-0 flex-1 leading-tight">
                  <div class="truncate text-[14px] font-semibold text-white">{{ displayName }}</div>
                  <div class="mt-0.5 truncate text-[11px] text-slate-400">{{ roleLabel }}</div>
                </div>
              </div>
              <button @click="handleLogout" class="mt-4 flex items-center gap-2 text-[14px] font-medium text-rose-500 transition-colors hover:text-rose-400">
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" /></svg>
                登出
              </button>
            </template>
          </div>
        </nav>
      </aside>

      <transition name="fade">
        <div v-if="mobileMenuOpen" class="fixed inset-0 z-40 lg:hidden" @click="closeMobileMenu">
          <div class="absolute inset-0 bg-slate-900/50 backdrop-blur-sm"></div>
          <aside class="relative flex h-full w-[300px] max-w-[86vw] flex-col bg-[linear-gradient(180deg,#10182f_0%,#151d34_100%)] text-slate-100 shadow-2xl" @click.stop>
            <div class="flex items-center gap-4 border-b border-white/8 px-6 py-7">
              <div class="grid h-11 w-11 place-items-center rounded-[18px] bg-gradient-to-br from-indigo-500 to-violet-600 text-white shadow-lg shadow-indigo-950/40">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016" />
                </svg>
              </div>
              <div class="leading-tight">
                <div class="text-[14px] font-bold tracking-widest text-white">叫修管理系統</div>
                <div class="mt-0.5 text-[12px] font-medium text-slate-300">CaseFlow</div>
              </div>
            </div>

            <nav class="flex min-h-0 flex-1 flex-col px-4 py-7">
              <div class="space-y-2">
                <router-link
                  v-for="item in allNavItems"
                  :key="`mobile-${item.path}`"
                  :to="item.path"
                  class="group relative flex items-center gap-4 rounded-[16px] px-4 py-4 text-[15px] font-medium transition-all duration-150"
                  :class="navItemClass(item)"
                  @click="closeMobileMenu"
                >
                  <svg class="h-[21px] w-[21px] shrink-0" :class="iconColorClass(item)" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
                  </svg>
                  <span class="flex-1">{{ item.label }}</span>
                  <span
                    v-if="item.path === '/notifications' && unreadCount > 0"
                    class="grid h-5 min-w-[20px] place-items-center rounded-full bg-rose-500 px-1.5 text-[10px] font-bold tracking-wider text-white shadow-sm"
                  >
                    {{ unreadCount > 99 ? '99+' : unreadCount }}
                  </span>
                </router-link>
              </div>

              <div class="mt-auto -mx-4 border-t border-white/8 px-6 pb-6 pt-8">
                <div class="flex items-center gap-4">
                  <div class="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-indigo-500/25 text-sm font-semibold text-indigo-200 ring-1 ring-white/10">
                    {{ userInitial }}
                  </div>
                  <div class="min-w-0 flex-1 leading-tight">
                    <div class="truncate text-[15px] font-semibold text-white">{{ displayName }}</div>
                    <div class="mt-1 truncate text-[12px] text-slate-400">{{ roleLabel }}</div>
                  </div>
                </div>
                <button @click="handleLogout" class="mt-6 flex items-center gap-2 text-[15px] font-medium text-rose-500 transition-colors hover:text-rose-400">
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" />
                  </svg>
                  登出
                </button>
              </div>
            </nav>
          </aside>
        </div>
      </transition>

      <main class="min-w-0 flex-1 overflow-x-hidden transition-[padding] duration-300" :class="sidebarCollapsed ? 'lg:pl-[68px]' : 'lg:pl-[300px]'">
        <div class="mx-auto w-full max-w-[1600px] px-4 py-4 sm:px-6 sm:py-5 lg:px-8 lg:py-6">
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
const sidebarCollapsed = ref(false)
const unreadCount = ref(0)
let pollTimer = null

const primaryItems = computed(() => {
  const items = [
    { path: '/dashboard', label: '儀表板', icon: 'dashboard', match: 'dashboard' },
    { path: '/cases', label: '案件列表', icon: 'cases', exact: true }
  ]

  if (auth.user?.role !== 'SE') {
    items.push({ path: '/cases/new', label: '新增案件', icon: 'new', exact: true })
  }

  return items
})

const reportItems = [
  { path: '/reports/hours', label: '工時統計', icon: 'hours', exact: true },
  { path: '/reports/cases', label: '案件數量統計', icon: 'casesReport', exact: true }
]

const systemItems = [
  { path: '/notifications', label: '通知中心', icon: 'notifications', exact: true },
  { path: '/profile', label: '個人設定', icon: 'profile', exact: true }
]

const allNavItems = computed(() => {
  return [...primaryItems.value, ...reportItems, ...systemItems]
})

const pageTitle = computed(() => {
  const activeItem = allNavItems.value.find(item => isActive(item))
  return activeItem ? activeItem.label : '首頁'
})

const roleLabel = computed(() =>
  ({ ADMIN: '系統管理員', SysAdmin: '系統管理員', PM: '專案經理', SE: '工程師' }[auth.user?.role] || auth.user?.role || '未登入')
)

const displayName = computed(() => auth.user?.full_name || auth.user?.username || '使用者')
const userInitial = computed(() => (displayName.value || 'U').trim().charAt(0).toUpperCase())

function isActive(item) {
  if (!item) return false
  if (item.match === 'dashboard') return route.path === '/' || route.path === '/dashboard'
  if (item.exact) return route.path === item.path
  return route.path === item.path || route.path.startsWith(item.path + '/')
}

function navItemClass(item) {
  return isActive(item)
    ? 'bg-gradient-to-r from-indigo-600 to-violet-500 text-white shadow-[0_14px_30px_rgba(79,70,229,0.36)]'
    : 'text-slate-300 hover:bg-white/6 hover:text-white'
}

// 非啟用狀態的 icon 顏色（透過 inline style 取代 Tailwind class）
const iconColorMap = {
  '/dashboard': '#67e8f9',
  '/cases': '#fde68a',
  '/cases/new': '#c4b5fd',
  '/reports/hours': '#f9a8d4',
  '/reports/cases': '#7dd3fc',
  '/notifications': '#fcd34d',
  '/profile': '#e879f9'
}
function iconColor(path) {
  return { color: iconColorMap[path] || '#94a3b8' }
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
  mobileMenuOpen.value = !mobileMenuOpen.value
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
