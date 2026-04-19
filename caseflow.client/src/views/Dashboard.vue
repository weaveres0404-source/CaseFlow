<template>
  <div class="space-y-8">
    <!-- ── Page header ── -->
    <div class="flex items-center justify-between">
      <h2 class="text-2xl font-bold text-gray-900">儀表板</h2>
      <span class="text-sm text-gray-400">{{ todayStr }}</span>
    </div>

    <!-- ── KPI cards: 4 columns on xl desktop ── -->
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
      <div v-for="kpi in kpiCards" :key="kpi.label"
        class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 flex items-start gap-4 hover:shadow-md transition-shadow">
        <div class="flex items-center justify-center w-12 h-12 rounded-xl shrink-0" :class="kpi.iconBg">
          <span class="text-2xl">{{ kpi.icon }}</span>
        </div>
        <div>
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">{{ kpi.label }}</p>
          <p class="text-4xl font-bold mt-1" :class="kpi.textColor">{{ kpi.count }}</p>
        </div>
      </div>
    </div>

    <!-- ── Two-column row: notifications (2/3) + quick actions (1/3) ── -->
    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
      <!-- Recent notifications -->
      <div class="xl:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-100 flex justify-between items-center">
          <h3 class="text-base font-semibold text-gray-900">最新通知</h3>
          <router-link to="/notifications" class="text-sm text-indigo-600 hover:text-indigo-800 font-medium">查看全部 →</router-link>
        </div>
        <div class="divide-y divide-gray-50">
          <div v-for="n in notifications" :key="n.id"
            class="px-6 py-4 hover:bg-slate-50 cursor-pointer transition-colors"
            @click="goToCase(n.case_id)">
            <div class="flex justify-between items-start gap-4">
              <div class="min-w-0">
                <p class="text-sm font-medium text-gray-900 truncate">{{ n.title }}</p>
                <p class="text-xs text-gray-500 mt-0.5 line-clamp-1">{{ n.message }}</p>
              </div>
              <span class="text-xs text-gray-400 whitespace-nowrap shrink-0">{{ formatTime(n.created_at) }}</span>
            </div>
          </div>
          <div v-if="notifications.length === 0" class="px-6 py-12 text-center text-gray-400 text-sm">暫無通知</div>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 flex flex-col gap-3">
        <h3 class="text-base font-semibold text-gray-900 mb-1">快速操作</h3>
        <router-link to="/cases/new"
          class="flex items-center justify-center gap-2 px-4 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm font-medium transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          新增案件
        </router-link>
        <router-link to="/cases"
          class="flex items-center justify-center gap-2 px-4 py-3 bg-white border border-gray-200 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium transition-colors">
          查看我的案件
        </router-link>
        <router-link to="/reports/hours"
          class="flex items-center justify-center gap-2 px-4 py-3 bg-white border border-gray-200 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium transition-colors">
          工時報表
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../utils/api'

const router = useRouter()
const kpiCards = ref([
  { label: '待處理',    count: 0, textColor: 'text-gray-700',   iconBg: 'bg-gray-100',  icon: '📥' },
  { label: '待派工',    count: 0, textColor: 'text-blue-600',   iconBg: 'bg-blue-50',   icon: '📤' },
  { label: '處理中',    count: 0, textColor: 'text-yellow-600', iconBg: 'bg-yellow-50', icon: '⚙️' },
  { label: '完工待確認', count: 0, textColor: 'text-green-600',  iconBg: 'bg-green-50',  icon: '✅' }
])

const todayStr = new Date().toLocaleDateString('zh-TW', {
  year: 'numeric', month: 'long', day: 'numeric', weekday: 'long'
})
const notifications = ref([])

async function fetchKPIs() {
  const statuses = [10, 20, 30, 40]
  for (let i = 0; i < statuses.length; i++) {
    try {
      const { data: res } = await api.get('/cases', { params: { status: statuses[i], page_size: 1 } })
      if (res.success) kpiCards.value[i].count = res.meta.total
    } catch { /* ignore */ }
  }
}

async function fetchNotifications() {
  try {
    const { data: res } = await api.get('/notifications', { params: { is_read: false, page_size: 5 } })
    if (res.success) notifications.value = res.data
  } catch { /* ignore */ }
}

function goToCase(caseId) {
  if (caseId) router.push(`/cases/${caseId}`)
}

function formatTime(dt) {
  if (!dt) return ''
  const d = new Date(dt)
  const now = new Date()
  const diff = (now - d) / 1000
  if (diff < 60) return '剛剛'
  if (diff < 3600) return `${Math.floor(diff / 60)} 分鐘前`
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小時前`
  return d.toLocaleDateString('zh-TW')
}

onMounted(() => {
  fetchKPIs()
  fetchNotifications()
})
</script>
