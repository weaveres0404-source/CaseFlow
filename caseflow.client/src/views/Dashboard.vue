<template>
  <div class="space-y-5">

    <!-- Greeting row -->
    <div class="flex flex-col md:flex-row md:items-end md:justify-between gap-3">
      <div>
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-2">
          <span>{{ todayDateStr }}</span>
          <span class="w-1 h-1 rounded-full bg-slate-300"></span>
          <span>第 {{ weekNum }} 週</span>
        </div>
        <h1 class="text-2xl md:text-[28px] font-bold text-slate-900 tracking-tight">早安，{{ userName }} 👋</h1>
        <p class="text-sm text-slate-500 mt-1">
          您有 <span class="text-slate-900 font-medium">{{ pendingCount }}</span> 件待處理與
          <span class="text-slate-900 font-medium">{{ completedCount }}</span> 件已完工待確認。
        </p>
      </div>
      <div class="flex items-center gap-2">
        <div class="text-xs text-slate-500 hidden sm:block">最後同步 <span class="tabular-nums text-slate-700">{{ lastSyncTime }}</span></div>
        <button @click="refresh" class="h-9 px-3 inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700">
          重新整理
        </button>
        <router-link to="/cases/new" class="h-9 px-3.5 inline-flex items-center gap-1.5 rounded-lg bg-indigo-700 hover:bg-indigo-800 text-white text-sm font-medium shadow-sm">
          ＋ 立案新案件
        </router-link>
      </div>
    </div>

    <!-- KPI cards (4 main statuses) -->
    <section class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <router-link
        v-for="kpi in kpiCards"
        :key="kpi.label"
        :to="kpi.to"
        class="group bg-white border border-slate-200 rounded-xl p-4 hover:border-slate-300 hover:shadow-sm transition"
        :class="kpi.ring"
      >
        <div class="flex items-center justify-between">
          <span class="inline-flex items-center gap-1.5 text-xs" :class="kpi.labelClass">
            <span class="w-1.5 h-1.5 rounded-full" :class="kpi.dotColor"></span>{{ kpi.label }}
          </span>
          <span v-if="kpi.actionBadge" class="text-[10px] px-1.5 py-0.5 rounded-full" :class="kpi.actionBadgeClass">{{ kpi.actionBadge }}</span>
          <span v-else class="text-slate-300 group-hover:text-indigo-600 transition text-sm">↗</span>
        </div>
        <div class="mt-2 flex items-baseline gap-2">
          <span class="tabular-nums text-3xl font-semibold text-slate-900">{{ kpi.count }}</span>
          <span v-if="kpi.hint" class="text-xs" :class="kpi.hintClass">{{ kpi.hint }}</span>
        </div>
        <div class="mt-2 h-1.5 rounded-full bg-slate-100 overflow-hidden">
          <div class="h-full rounded-full transition-all duration-500" :class="kpi.barColor" :style="{ width: `${kpi.barWidth}%` }"></div>
        </div>
      </router-link>
    </section>

    <!-- Main grid: 我的待辦案件 + 最新通知 -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">

      <!-- 我的待辦案件 -->
      <section class="lg:col-span-2 bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100">
          <div>
            <h2 class="text-sm font-semibold text-slate-900">我的待辦案件</h2>
            <p class="text-[11px] text-slate-500 mt-0.5">依更新時間排序 · 僅顯示需您採取行動的案件</p>
          </div>
          <router-link to="/cases" class="text-xs text-indigo-700 hover:text-indigo-800 inline-flex items-center gap-1">查看全部 →</router-link>
        </div>
        <ul class="divide-y divide-slate-100">
          <li
            v-for="item in openCases"
            :key="item.id"
            class="px-5 py-3.5 flex items-start gap-3 hover:bg-slate-50 cursor-pointer"
            @click="goToCase(item.id)"
          >
            <span class="mt-1.5 w-2 h-2 rounded-full shrink-0" :class="statusDotColor(item.status)"></span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="tabular-nums text-sm font-medium text-indigo-700">{{ item.case_number }}</span>
                <span class="text-[11px] px-1.5 py-0.5 rounded-full ring-1" :class="statusClass(item.status)">{{ statusLabel(item.status) }}</span>
                <span class="text-[11px] px-1.5 py-0.5 rounded-full ring-1" :class="priorityClass(item.priority)">{{ priorityLabel(item.priority) }}</span>
              </div>
              <div class="text-sm text-slate-800 mt-1 truncate">{{ item.project?.code }} · {{ item.customer?.name }}</div>
              <div class="text-[11px] text-slate-500 mt-0.5">立案人 {{ item.created_by?.full_name || '—' }} · {{ formatTime(item.updated_at) }}</div>
            </div>
          </li>
          <li v-if="openCases.length === 0" class="px-5 py-12 text-center text-sm text-slate-400">目前沒有待辦案件</li>
        </ul>
      </section>

      <!-- 最新通知 -->
      <section class="bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100">
          <div>
            <h2 class="text-sm font-semibold text-slate-900 inline-flex items-center gap-1.5">
              最新通知
              <span v-if="notifications.length > 0" class="tabular-nums text-[10px] px-1.5 rounded-full bg-rose-500 text-white">{{ notifications.length }}</span>
            </h2>
            <p class="text-[11px] text-slate-500 mt-0.5">只顯示未讀 Top 5</p>
          </div>
          <router-link to="/notifications" class="text-xs text-slate-500 hover:text-slate-800">全部已讀</router-link>
        </div>
        <ul class="divide-y divide-slate-100 text-sm">
          <li
            v-for="n in notifications"
            :key="n.id"
            class="px-5 py-3 flex gap-3 cursor-pointer hover:bg-slate-50"
            @click="goToCase(n.case_id)"
          >
            <div class="w-8 h-8 rounded-lg bg-indigo-50 text-indigo-700 grid place-items-center shrink-0 text-base">📩</div>
            <div class="flex-1 min-w-0">
              <div class="font-medium text-slate-900 truncate">{{ n.title }}</div>
              <div class="text-[12px] text-slate-500 truncate">{{ n.message }}</div>
              <div class="text-[11px] text-slate-400 mt-0.5">{{ formatTime(n.created_at) }}</div>
            </div>
          </li>
          <li v-if="notifications.length === 0" class="px-5 py-12 text-center text-sm text-slate-400">暫無通知</li>
        </ul>
      </section>
    </div>

    <!-- 案件狀態分佈 -->
    <section class="bg-white border border-slate-200 rounded-xl p-5">
      <div class="flex items-center justify-between mb-3">
        <div>
          <h2 class="text-sm font-semibold text-slate-900">案件狀態分佈</h2>
          <p class="text-[11px] text-slate-500 mt-0.5">本月 · 共 {{ totalCases }} 件</p>
        </div>
        <router-link to="/cases" class="text-xs text-indigo-700 hover:underline">前往列表</router-link>
      </div>
      <div class="flex flex-col md:flex-row md:items-center gap-6">
        <ul class="flex-1 grid grid-cols-2 gap-x-6 gap-y-2 text-xs">
          <li v-for="stat in statusStats" :key="stat.label" class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-sm shrink-0" :class="stat.bg"></span>
            <span class="flex-1 text-slate-600">{{ stat.label }}</span>
            <span class="tabular-nums text-slate-900">{{ stat.count }}</span>
          </li>
        </ul>
        <div class="relative w-36 h-36 shrink-0 mx-auto md:mx-0">
          <svg viewBox="0 0 42 42" class="w-full h-full -rotate-90">
            <circle cx="21" cy="21" r="15.915" fill="none" stroke="#f1f5f9" stroke-width="6"/>
            <circle
              v-for="(seg, idx) in donutSegments"
              :key="idx"
              cx="21" cy="21" r="15.915"
              fill="none"
              :stroke="seg.color"
              stroke-width="6"
              :stroke-dasharray="`${seg.dash} ${100 - seg.dash}`"
              :stroke-dashoffset="`-${seg.offset}`"
            />
          </svg>
          <div class="absolute inset-0 grid place-items-center">
            <div class="text-center">
              <div class="tabular-nums text-2xl font-semibold text-slate-900">{{ totalCases }}</div>
              <div class="text-[10px] text-slate-500">件</div>
            </div>
          </div>
        </div>
      </div>
    </section>

  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const router = useRouter()
const auth = useAuthStore()
const meta = useMetaStore()

// ── user info ─────────────────────────────────────────────
const userName = computed(() => auth.user?.full_name || auth.user?.username || '用戶')

// ── date / week ───────────────────────────────────────────
const now = new Date()
const todayDateStr = now.toLocaleDateString('zh-TW', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' })
const startOfYear = new Date(now.getFullYear(), 0, 1)
const weekNum = Math.ceil(((now - startOfYear) / 86400000 + startOfYear.getDay() + 1) / 7)
const lastSyncTime = ref(now.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', second: '2-digit' }))

// ── raw status counts from API ────────────────────────────
const statusSummary = ref({ pending: 0, assigned: 0, in_progress: 0, returned: 0, completed: 0, closed: 0, cancelled: 0 })
const thisMonth = ref({ total_hours: 0, completed_cases: 0 })

const statusKeyMap = { 10: 'pending', 20: 'assigned', 30: 'in_progress', 35: 'returned', 40: 'completed', 50: 'closed', 60: 'cancelled' }
const openCaseStatuses = new Set([10, 20, 30, 35, 40])

function getCount(statusKey) {
  return Number(statusSummary.value[statusKey] ?? 0)
}

const pendingCount = computed(() => getCount('pending'))
const completedCount = computed(() => getCount('completed'))

// ── 4 KPI cards ───────────────────────────────────────────
const kpiCards = computed(() => {
  const counts = [
    { label: '待處理', key: 'pending', to: '/cases?status=10', dotColor: 'bg-slate-400', labelClass: 'text-slate-500', barColor: 'bg-slate-400' },
    { label: '已派工', key: 'assigned', to: '/cases?status=20', dotColor: 'bg-blue-500', labelClass: 'text-slate-500', barColor: 'bg-blue-500' },
    { label: '處理中', key: 'in_progress', to: '/cases?status=30', dotColor: 'bg-amber-500', labelClass: 'text-slate-500', barColor: 'bg-amber-500' },
    {
      label: '已完工待確認', key: 'completed', to: '/cases?status=40',
      dotColor: 'bg-emerald-500', labelClass: 'text-emerald-700 font-medium',
      barColor: 'bg-emerald-500', ring: 'ring-1 ring-emerald-200/60',
      actionBadge: '需您處理', actionBadgeClass: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200'
    }
  ]
  const maxVal = Math.max(...counts.map(c => getCount(c.key)), 1)
  return counts.map(c => ({
    ...c,
    count: getCount(c.key),
    barWidth: Math.max(8, Math.round(getCount(c.key) / maxVal * 100))
  }))
})

// ── status distribution ───────────────────────────────────
const statusStats = computed(() => [
  { label: '待處理', count: getCount('pending'), bg: 'bg-slate-400', donutColor: '#94a3b8' },
  { label: '已派工', count: getCount('assigned'), bg: 'bg-blue-500', donutColor: '#3b82f6' },
  { label: '處理中', count: getCount('in_progress'), bg: 'bg-amber-500', donutColor: '#f59e0b' },
  { label: '已退回', count: getCount('returned'), bg: 'bg-red-500', donutColor: '#ef4444' },
  { label: '已完工', count: getCount('completed'), bg: 'bg-green-500', donutColor: '#22c55e' },
  { label: '已結案', count: getCount('closed'), bg: 'bg-violet-500', donutColor: '#8b5cf6' },
  { label: '已取消', count: getCount('cancelled'), bg: 'bg-rose-500', donutColor: '#f43f5e' },
])

const totalCases = computed(() => statusStats.value.reduce((s, r) => s + r.count, 0))

const donutSegments = computed(() => {
  const total = totalCases.value
  if (total === 0) return []
  let offset = 0
  return statusStats.value
    .filter(s => s.count > 0)
    .map(stat => {
      const dash = parseFloat(((stat.count / total) * 100).toFixed(2))
      const seg = { color: stat.donutColor, dash, offset }
      offset += dash
      return seg
    })
})

// ── open cases list ───────────────────────────────────────
const openCases = ref([])
const notifications = ref([])

const statusLabel = status => meta.statusMap[status]?.label || status
const statusClass = status => meta.statusMap[status]?.color || 'bg-slate-100 text-slate-600 ring-slate-200'
const priorityLabel = priority => meta.priorityMap[priority]?.label || priority || '未設定'
const priorityClass = priority => meta.priorityMap[priority]?.color || 'bg-slate-100 text-slate-600 ring-slate-200'

function statusDotColor(status) {
  const map = { 10: 'bg-slate-400', 20: 'bg-blue-500', 30: 'bg-amber-500', 35: 'bg-red-500', 40: 'bg-green-500', 50: 'bg-violet-500', 60: 'bg-rose-500' }
  return map[status] || 'bg-slate-300'
}

// ── fetch ─────────────────────────────────────────────────
async function fetchKPIs() {
  try {
    const { data: res } = await api.get('/reports/dashboard')
    if (res.success) {
      statusSummary.value = res.data.status_summary || {}
      thisMonth.value = res.data.this_month || {}
    }
  } catch { /* ignore */ }
}

async function fetchOpenCases() {
  try {
    const { data: res } = await api.get('/cases', { params: { page_size: 8, sort: 'updated_at,desc' } })
    if (res.success) {
      openCases.value = (res.data || []).filter(item => openCaseStatuses.has(Number(item.status))).slice(0, 5)
    }
  } catch { openCases.value = [] }
}

async function fetchNotifications() {
  try {
    const { data: res } = await api.get('/notifications', { params: { is_read: false, page_size: 5 } })
    if (res.success) notifications.value = res.data
  } catch { /* ignore */ }
}

async function refresh() {
  lastSyncTime.value = new Date().toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
  await Promise.all([fetchKPIs(), fetchNotifications(), fetchOpenCases()])
}

function goToCase(caseId) {
  if (caseId) router.push(`/cases/${caseId}`)
}

function formatTime(dt) {
  if (!dt) return ''
  const d = new Date(dt)
  const diff = (Date.now() - d) / 1000
  if (diff < 60) return '剛剛'
  if (diff < 3600) return `${Math.floor(diff / 60)} 分鐘前`
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小時前`
  return d.toLocaleDateString('zh-TW')
}

onMounted(async () => {
  if (!meta.loaded) await meta.fetchDropdowns()
  await Promise.all([fetchKPIs(), fetchNotifications(), fetchOpenCases()])
})
</script>
