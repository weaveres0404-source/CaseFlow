<template>
  <div class="min-h-screen bg-slate-50 text-slate-800">
    <header class="sticky top-0 z-30 border-b border-slate-200 bg-white/80 backdrop-blur transition-[padding] duration-300" :class="sidebarCollapsed ? 'lg:pl-[68px]' : 'lg:pl-48'">
      <div class="flex h-14 items-center justify-between px-4 lg:px-6 gap-4">
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
      <!-- ── Desktop Sidebar (white/light) ── -->
      <aside
        class="hidden lg:fixed lg:inset-y-0 lg:left-0 lg:z-40 lg:flex lg:flex-col lg:border-r lg:border-slate-200 lg:bg-white transition-[width] duration-300 overflow-hidden"
        :class="sidebarCollapsed ? 'lg:w-[68px]' : 'lg:w-48'"
      >
        <!-- Logo -->
        <div class="flex items-center border-b border-slate-200 h-14 px-3" :class="sidebarCollapsed ? 'justify-center' : 'gap-2.5 px-4'">
          <a href="/" class="flex items-center gap-2">
            <div class="shrink-0 grid h-8 w-8 place-items-center rounded-lg bg-brand-700 text-black">
              <svg viewBox="0 0 24 24" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.2">
                <path d="M4 6h16M4 12h10M4 18h7"/><circle cx="18" cy="18" r="3"/>
              </svg>
            </div>
            <div v-if="!sidebarCollapsed" class="leading-tight">
              <div class="text-sm font-semibold text-slate-900 tracking-tight">CaseFlow</div>
              <div class="text-[10px] text-slate-400 -mt-0.5">任務追蹤系統</div>
            </div>
          </a>
          <button v-if="!sidebarCollapsed" @click="sidebarCollapsed = true" class="ml-auto grid h-7 w-7 place-items-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600" title="收起側欄">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 19l-7-7 7-7M18 19l-7-7 7-7"/></svg>
          </button>
        </div>

        <nav class="flex min-h-0 flex-1 flex-col" :class="sidebarCollapsed ? 'items-center py-3 px-2' : 'p-2.5'">
          <!-- 收起時展開按鈕 -->
          <button v-if="sidebarCollapsed" @click="sidebarCollapsed = false" class="mb-2 grid h-9 w-9 place-items-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700" title="展開側欄">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5l7 7-7 7M6 5l7 7-7 7"/></svg>
          </button>

          <!-- 工作區 group -->
          <div v-if="!sidebarCollapsed" class="px-3 pt-2 pb-1 text-[10px] font-medium text-slate-400 tracking-wider uppercase">工作區</div>
          <div :class="sidebarCollapsed ? 'flex flex-col items-center gap-1 w-full' : 'space-y-0.5'">
            <router-link
              v-for="item in primaryItems"
              :key="item.path"
              :to="item.path"
              class="relative flex items-center text-sm transition-all duration-150"
              :class="[
                sidebarCollapsed ? 'justify-center rounded-lg w-10 h-10' : 'gap-3 px-3 py-2 rounded-lg',
                navItemClass(item)
              ]"
              :title="sidebarCollapsed ? item.label : undefined"
            >
              <span v-if="!sidebarCollapsed && isActive(item)" class="nav-active-bar"></span>
              <svg class="shrink-0 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
              </svg>
              <span v-if="!sidebarCollapsed" class="flex-1">{{ item.label }}</span>
              <span v-if="sidebarCollapsed && item.path === '/notifications' && unreadCount > 0" class="absolute right-1 top-1 h-2 w-2 rounded-full bg-rose-500"></span>
            </router-link>
          </div>

          <!-- 系統 group -->
          <div v-if="!sidebarCollapsed" class="px-3 pt-4 pb-1 text-[10px] font-medium text-slate-400 tracking-wider uppercase">系統</div>
          <div :class="sidebarCollapsed ? 'flex flex-col items-center gap-1 w-full mt-2' : 'space-y-0.5'">
            <router-link
              v-for="item in systemItems"
              :key="item.path"
              :to="item.path"
              class="relative flex items-center text-sm transition-all duration-150"
              :class="[
                sidebarCollapsed ? 'justify-center rounded-lg w-10 h-10' : 'gap-3 px-3 py-2 rounded-lg',
                navItemClass(item)
              ]"
              :title="sidebarCollapsed ? item.label : undefined"
            >
              <span v-if="!sidebarCollapsed && isActive(item)" class="nav-active-bar"></span>
              <svg class="shrink-0 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" />
              </svg>
              <span v-if="!sidebarCollapsed" class="flex-1">{{ item.label }}</span>
              <span v-if="!sidebarCollapsed && item.path === '/notifications' && unreadCount > 0"
                class="grid h-4 min-w-[16px] place-items-center rounded-full bg-rose-500 px-1 text-[10px] text-white">
                {{ unreadCount > 99 ? '99+' : unreadCount }}
              </span>
              <span v-if="sidebarCollapsed && item.path === '/notifications' && unreadCount > 0" class="absolute right-1 top-1 h-2 w-2 rounded-full bg-rose-500"></span>
            </router-link>
          </div>

          <!-- 底部使用者資訊 -->
          <div class="mt-auto border-t border-slate-200 pt-3" :class="sidebarCollapsed ? 'flex flex-col items-center gap-2 w-full' : 'mx-1 pb-1'">
            <template v-if="sidebarCollapsed">
              <div class="grid h-8 w-8 place-items-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700" :title="displayName">{{ userInitial }}</div>
              <button @click="handleLogout" class="grid h-9 w-9 place-items-center rounded-lg text-rose-500 transition-colors hover:bg-slate-100" title="登出">
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" /></svg>
              </button>
            </template>
            <template v-else>
              <div class="flex items-center gap-2">
                <div class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">{{ userInitial }}</div>
                <div class="min-w-0 flex-1 leading-tight">
                  <div class="truncate text-xs font-semibold text-slate-900">{{ displayName }}</div>
                  <div class="truncate text-[10px] text-slate-400">{{ roleLabel }}</div>
                </div>
              </div>
              <button @click="handleLogout" class="mt-2 flex items-center gap-1.5 text-xs font-medium text-rose-500 hover:text-rose-600">
                <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" /></svg>
                登出
              </button>
            </template>
          </div>
        </nav>
      </aside>

      <transition name="fade">
        <div v-if="mobileMenuOpen" class="fixed inset-0 z-40 lg:hidden" @click="closeMobileMenu">
          <div class="absolute inset-0 bg-slate-900/50 backdrop-blur-sm"></div>
          <aside class="relative flex h-full w-48 max-w-[86vw] flex-col bg-white shadow-2xl" @click.stop>
            <div class="flex items-center gap-2.5 border-b border-slate-200 h-14 px-4">
              <div class="grid h-8 w-8 place-items-center rounded-lg bg-brand-700 text-black">
                <svg viewBox="0 0 24 24" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.2"><path d="M4 6h16M4 12h10M4 18h7"/><circle cx="18" cy="18" r="3"/></svg>
              </div>
              <div class="leading-tight">
                <div class="text-sm font-semibold text-slate-900 tracking-tight">CaseFlow</div>
                <div class="text-[10px] text-slate-400 -mt-0.5">任務追蹤系統</div>
              </div>
            </div>
            <nav class="flex min-h-0 flex-1 flex-col p-2.5">
              <div class="px-3 pt-2 pb-1 text-[10px] font-medium text-slate-400 tracking-wider uppercase">工作區</div>
              <div class="space-y-0.5">
                <router-link v-for="item in primaryItems" :key="`mobile-${item.path}`" :to="item.path"
                  class="relative flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-all" :class="navItemClass(item)" @click="closeMobileMenu">
                  <span v-if="isActive(item)" class="nav-active-bar"></span>
                  <svg class="shrink-0 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" /></svg>
                  <span class="flex-1">{{ item.label }}</span>
                </router-link>
              </div>
              <div class="px-3 pt-4 pb-1 text-[10px] font-medium text-slate-400 tracking-wider uppercase">系統</div>
              <div class="space-y-0.5">
                <router-link v-for="item in systemItems" :key="`mobile-sys-${item.path}`" :to="item.path"
                  class="relative flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-all" :class="navItemClass(item)" @click="closeMobileMenu">
                  <span v-if="isActive(item)" class="nav-active-bar"></span>
                  <svg class="shrink-0 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath(item.icon)" /></svg>
                  <span class="flex-1">{{ item.label }}</span>
                  <span v-if="item.path === '/notifications' && unreadCount > 0" class="grid h-4 min-w-[16px] place-items-center rounded-full bg-rose-500 px-1 text-[10px] text-white">{{ unreadCount }}</span>
                </router-link>
              </div>
              <div class="mt-auto border-t border-slate-200 pt-3">
                <div class="flex items-center gap-2">
                  <div class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">{{ userInitial }}</div>
                  <div class="min-w-0 flex-1"><div class="truncate text-xs font-semibold text-slate-900">{{ displayName }}</div><div class="text-[10px] text-slate-400">{{ roleLabel }}</div></div>
                </div>
                <button @click="handleLogout" class="mt-2 flex items-center gap-1.5 text-xs font-medium text-rose-500 hover:text-rose-600">
                  <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" :d="iconPath('logout')" /></svg>
                  登出
                </button>
              </div>
            </nav>
          </aside>
        </div>
      </transition>

      <main class="min-w-0 flex-1 overflow-x-hidden transition-[padding] duration-300" :class="sidebarCollapsed ? 'lg:pl-[68px]' : 'lg:pl-48'">
        <div class="mx-auto w-full max-w-[1600px] px-4 py-5 sm:px-6 lg:px-7">
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
  ({ ADMIN: '系統管理員', SysAdmin: '系統管理員', PM: '專案經理', SE: '專案成員' }[auth.user?.role] || auth.user?.role || '未登入')
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
    ? 'bg-brand-50 text-brand-800 font-medium'
    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
}

// 保留舊函式以避免 template 引用錯誤
function iconColor() { return {} }
function iconColorClass() { return '' }

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
