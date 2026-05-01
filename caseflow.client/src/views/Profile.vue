<template>
  <div class="mx-auto flex w-full max-w-[1200px] flex-col gap-4">
    <div class="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
      <div>
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-1.5">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">個人設定</span>
        </div>
        <h1 class="text-2xl md:text-[28px] font-bold text-slate-900 tracking-tight">個人設定</h1>
        <p class="text-sm text-slate-500 mt-1">檢視目前登入身分、角色權限與最近一次登入資訊。</p>
      </div>
      <div class="inline-flex items-center gap-2 rounded-full bg-white border border-slate-200 px-3 py-1.5 text-xs text-slate-500 shadow-sm w-fit max-w-full">
        <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
        登入中
        <span class="font-medium text-slate-700 break-words [overflow-wrap:anywhere]">{{ displayName }}</span>
      </div>
    </div>

    <section class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div class="relative overflow-hidden bg-slate-900 px-6 py-5 text-slate-100 md:px-7 md:py-6">
        <div class="absolute inset-0 opacity-40" style="background-image:linear-gradient(rgba(255,255,255,.06) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.06) 1px,transparent 1px);background-size:36px 36px"></div>
        <div class="absolute -top-24 right-0 w-72 h-72 opacity-60" style="background:radial-gradient(circle at center, rgba(129,140,248,.35), rgba(67,56,202,0) 70%)"></div>
        <div class="relative flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
          <div class="flex items-center gap-4 min-w-0">
            <div class="w-16 h-16 rounded-2xl bg-white/10 ring-1 ring-white/10 backdrop-blur grid place-items-center text-2xl font-semibold text-indigo-100 shrink-0">
              {{ userInitial }}
            </div>
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2 mb-1.5">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ring-1 ring-white/15 bg-white/10 text-slate-100">{{ roleLabel }}</span>
                <span class="text-xs text-slate-400 tabular-nums">{{ usernameText }}</span>
              </div>
              <h2 class="text-2xl font-semibold text-white break-words [overflow-wrap:anywhere]">{{ displayName }}</h2>
              <p class="text-sm text-slate-300 mt-1 break-words [overflow-wrap:anywhere]">{{ emailText }}</p>
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3 text-xs min-w-[240px]">
            <div class="rounded-xl bg-white/8 border border-white/10 p-3">
              <div class="text-slate-400 mb-1">角色權限</div>
              <div class="text-sm font-medium text-white">{{ roleLabel }}</div>
            </div>
            <div class="rounded-xl bg-white/8 border border-white/10 p-3">
              <div class="text-slate-400 mb-1">最近登入</div>
              <div class="text-sm font-medium text-white tabular-nums">{{ lastLoginText }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="grid gap-4 p-5 md:p-6 lg:grid-cols-[1.15fr_.85fr]">
        <section class="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 md:p-5">
          <div class="flex items-center gap-2 mb-4">
            <div class="w-8 h-8 rounded-xl bg-brand-50 text-brand-700 grid place-items-center">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>
            </div>
            <div>
              <h3 class="text-sm font-semibold text-slate-900">帳號資訊</h3>
              <p class="text-xs text-slate-500 mt-0.5">這些資料由後端登入身分提供。</p>
            </div>
          </div>
          <dl class="divide-y divide-slate-200">
            <div class="py-3 flex items-start justify-between gap-4">
              <dt class="text-sm text-slate-500">顯示名稱</dt>
              <dd class="min-w-0 text-sm font-medium text-slate-900 text-right break-words [overflow-wrap:anywhere]">{{ displayName }}</dd>
            </div>
            <div class="py-3 flex items-start justify-between gap-4">
              <dt class="text-sm text-slate-500">帳號</dt>
              <dd class="min-w-0 text-sm font-medium text-slate-900 tabular-nums text-right break-words [overflow-wrap:anywhere]">{{ usernameText }}</dd>
            </div>
            <div class="py-3 flex items-start justify-between gap-4">
              <dt class="text-sm text-slate-500">Email</dt>
              <dd class="min-w-0 text-sm font-medium text-slate-900 text-right break-words [overflow-wrap:anywhere]">{{ emailText }}</dd>
            </div>
            <div class="py-3 flex items-start justify-between gap-4">
              <dt class="text-sm text-slate-500">角色</dt>
              <dd class="text-sm font-medium text-slate-900 text-right">{{ roleLabel }}</dd>
            </div>
            <div class="py-3 flex items-start justify-between gap-4">
              <dt class="text-sm text-slate-500">上次登入</dt>
              <dd class="text-sm font-medium text-slate-900 tabular-nums text-right">{{ lastLoginText }}</dd>
            </div>
          </dl>
        </section>

        <section class="space-y-3">
          <div class="rounded-2xl border border-slate-200 bg-white p-4 md:p-5">
            <div class="flex items-center gap-2 mb-3">
              <div class="w-8 h-8 rounded-xl bg-emerald-50 text-emerald-700 grid place-items-center">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-slate-900">安全狀態</h3>
                <p class="text-xs text-slate-500 mt-0.5">目前僅提供檢視，不開放自行修改密碼。</p>
              </div>
            </div>
            <ul class="space-y-2 text-sm text-slate-600">
              <li class="flex items-start gap-2">
                <span class="mt-1 w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                本次登入已完成身分驗證。
              </li>
              <li class="flex items-start gap-2">
                <span class="mt-1 w-1.5 h-1.5 rounded-full bg-slate-300"></span>
                若需重設密碼，請由系統管理員重新發送設定連結。
              </li>
            </ul>
          </div>

          <div class="rounded-2xl border border-amber-200 bg-amber-50/80 p-4 text-sm text-amber-900 md:p-5">
            <div class="font-medium mb-1">權限提示</div>
            <p class="leading-relaxed">您目前以 {{ roleLabel }} 身分操作系統；側邊選單與案件操作按鈕會依此角色自動調整。</p>
          </div>
        </section>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'

defineOptions({
  name: 'UserProfileView'
})

const auth = useAuthStore()
const user = computed(() => auth.user || null)

const roleLabel = computed(() => ({
  ADMIN: '系統管理員',
  SysAdmin: '系統管理員',
  PM: '專案經理',
  SE: '工程師'
}[user.value?.role] || user.value?.role || '未定義角色'))

const displayName = computed(() => user.value?.full_name || user.value?.username || '未命名使用者')
const usernameText = computed(() => user.value?.username || '尚未提供')
const emailText = computed(() => user.value?.email || '尚未提供')
const userInitial = computed(() => (displayName.value || 'U').trim().charAt(0).toUpperCase())
const lastLoginText = computed(() => formatTime(user.value?.last_login_at))

function formatTime(iso) {
  if (!iso) return '—'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('zh-TW', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  })
}
</script>
