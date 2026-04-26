<template>
  <div class="max-w-6xl mx-auto space-y-5">
    <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div>
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-1.5">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">案件數量統計</span>
        </div>
        <h1 class="text-2xl md:text-[28px] font-bold text-slate-900 tracking-tight">案件數量統計</h1>
        <p class="text-sm text-slate-500 mt-1">依專案、客戶、分類或狀態查看案件量分布。</p>
      </div>
      <button @click="exportCsv" :disabled="rows.length === 0" class="h-9 px-3.5 rounded-lg bg-indigo-700 hover:bg-indigo-800 disabled:bg-slate-300 disabled:cursor-not-allowed text-sm font-medium text-white">匯出 CSV</button>
    </div>

    <!-- 統計摘要 -->
    <section class="grid gap-3 sm:grid-cols-3">
      <article class="bg-white border border-slate-200 rounded-2xl px-5 py-4 shadow-sm">
        <div class="text-xs text-slate-500">分群筆數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ rows.length }}</div>
      </article>
      <article class="bg-white border border-slate-200 rounded-2xl px-5 py-4 shadow-sm">
        <div class="text-xs text-slate-500">案件總數</div>
        <div class="mt-1 text-2xl font-semibold text-indigo-700 tabular-nums">{{ serverTotal ?? total }}</div>
      </article>
      <article class="bg-white border border-slate-200 rounded-2xl px-5 py-4 shadow-sm">
        <div class="text-xs text-slate-500">統計區間</div>
        <div class="mt-1 text-sm font-medium text-slate-900 tabular-nums">{{ periodLabel }}</div>
      </article>
    </section>

    <!-- 篩選條件 -->
    <section class="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden">
      <div class="px-5 py-4 border-b border-slate-100 grid gap-3 sm:grid-cols-2 lg:grid-cols-[1fr_1fr_1fr_1fr_1fr_1fr_auto] lg:items-end">
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">起始日期</label>
          <input v-model="filters.date_from" type="date" class="input-base" />
        </div>
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">結束日期</label>
          <input v-model="filters.date_to" type="date" class="input-base" />
        </div>
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">分群方式</label>
          <select v-model="filters.group_by" class="input-base">
            <option value="project">專案</option>
            <option value="customer">客戶</option>
            <option value="status">狀態</option>
            <option value="category">問題分類</option>
            <option value="case_type">案件類型</option>
            <option value="created_by">立案者</option>
            <option value="assigned_pm">轉派 PM</option>
          </select>
        </div>
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">專案</label>
          <select v-model="filters.project_id" class="input-base">
            <option value="">全部</option>
            <option v-for="p in meta.projects" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">客戶</label>
          <select v-model="filters.customer_id" class="input-base">
            <option value="">全部</option>
            <option v-for="c in meta.customers" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-[11px] font-medium text-slate-500 mb-1">狀態</label>
          <select v-model="filters.status" class="input-base">
            <option value="">全部</option>
            <option value="10">待處理</option>
            <option value="20">已指派</option>
            <option value="30">處理中</option>
            <option value="35">退回</option>
            <option value="40">已完成</option>
            <option value="50">已結案</option>
            <option value="60">已取消</option>
          </select>
        </div>
        <button @click="fetchReport" :disabled="loading" class="h-10 px-5 rounded-lg bg-indigo-700 hover:bg-indigo-800 disabled:bg-slate-300 disabled:cursor-not-allowed text-sm font-medium text-white whitespace-nowrap">
          {{ loading ? '查詢中…' : '查詢' }}
        </button>
      </div>

      <div v-if="requestError" class="mx-5 mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ requestError }}</div>

      <!-- 表格標頭說明 -->
      <div class="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between gap-3 text-sm">
        <div>
          <span class="font-medium text-slate-900">{{ groupLabel }}分布</span>
          <span class="text-xs text-slate-500 ml-2">顯示各群組的案件總量與相對占比</span>
        </div>
        <div class="text-xs text-slate-500 tabular-nums shrink-0">{{ rows.length }} 筆結果</div>
      </div>

      <!-- 表格 -->
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead class="bg-slate-50 text-slate-500 text-left">
            <tr class="[&_th]:px-5 [&_th]:py-3 [&_th]:font-medium [&_th]:whitespace-nowrap">
              <th class="w-12">#</th>
              <th>{{ groupLabel }}</th>
              <th class="text-right w-24">案件數</th>
              <th class="w-48">占比</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="loading">
              <td colspan="4" class="px-5 py-10 text-center text-sm text-slate-400">載入中…</td>
            </tr>
            <template v-else>
              <tr v-for="(row, idx) in rows" :key="row.label + idx" class="hover:bg-slate-50 transition-colors">
                <td class="px-5 py-3.5 text-slate-400 tabular-nums text-xs">{{ idx + 1 }}</td>
                <td class="px-5 py-3.5 font-medium text-slate-900">{{ row.label }}</td>
                <td class="px-5 py-3.5 text-right font-semibold text-indigo-700 tabular-nums">{{ row.value.toLocaleString() }}</td>
                <td class="px-5 py-3.5">
                  <div class="flex items-center gap-2">
                    <div class="flex-1 h-2 rounded-full bg-slate-100 overflow-hidden">
                      <div class="h-full bg-indigo-500 rounded-full transition-[width] duration-500" :style="{ width: `${row.percentage}%` }"></div>
                    </div>
                    <span class="text-[11px] tabular-nums text-slate-400 w-10 text-right">{{ row.percentageDisplay }}%</span>
                  </div>
                </td>
              </tr>
              <!-- 合計列 -->
              <tr v-if="rows.length > 1" class="bg-slate-50 font-semibold border-t border-slate-200">
                <td class="px-5 py-3.5 text-slate-400 text-xs">合計</td>
                <td class="px-5 py-3.5 text-slate-600">—</td>
                <td class="px-5 py-3.5 text-right text-indigo-700 tabular-nums">{{ total.toLocaleString() }}</td>
                <td class="px-5 py-3.5"></td>
              </tr>
              <tr v-if="!loading && rows.length === 0">
                <td colspan="4" class="px-5 py-16 text-center text-slate-400">沒有符合條件的報表資料</td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

defineOptions({
  name: 'ReportsCasesView'
})

const meta = useMetaStore()
const rows = ref([])
const loading = ref(false)
const requestError = ref('')
const serverTotal = ref(null)

const now = new Date()
const firstDay = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10)
const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10)

const filters = ref({
  date_from: firstDay,
  date_to: lastDay,
  group_by: 'project',
  project_id: '',
  customer_id: '',
  status: ''
})

const GROUP_LABELS = {
  status: '狀態',
  project: '專案',
  customer: '客戶',
  category: '問題分類',
  case_type: '案件類型',
  created_by: '立案者',
  assigned_pm: '轉派 PM'
}

const groupLabel = computed(() => GROUP_LABELS[filters.value.group_by] || filters.value.group_by)

const total = computed(() => rows.value.reduce((sum, row) => sum + Number(row.value || 0), 0))
const periodLabel = computed(() => {
  const from = filters.value.date_from || '—'
  const to = filters.value.date_to || '—'
  return `${from} 至 ${to}`
})

async function fetchReport() {
  loading.value = true
  requestError.value = ''
  serverTotal.value = null
  try {
    const params = {}
    if (filters.value.date_from) params.date_from = filters.value.date_from
    if (filters.value.date_to)   params.date_to   = filters.value.date_to
    if (filters.value.group_by)  params.group_by  = filters.value.group_by
    if (filters.value.project_id)  params.project_id  = filters.value.project_id
    if (filters.value.customer_id) params.customer_id = filters.value.customer_id
    if (filters.value.status)      params.status      = filters.value.status

    const { data: res } = await api.get('/reports/cases', { params })
    if (res.success) {
      serverTotal.value = res.meta?.total_count ?? null
      const mapped = (res.data || []).map((item) => ({
        label: item.label || item.dimension || '未命名',
        value: Number(item.count || 0)
      }))
      const localMax = Math.max(...mapped.map(r => r.value), 1)
      rows.value = mapped.map((row) => ({
        ...row,
        percentage: (row.value / localMax) * 100,
        percentageDisplay: localMax ? ((row.value / (serverTotal.value || localMax)) * 100).toFixed(1) : '0.0'
      }))
    }
  } catch (err) {
    requestError.value = err?.response?.data?.error?.message || '讀取案件統計失敗，請稍後再試'
    rows.value = []
  } finally {
    loading.value = false
  }
}

function exportCsv() {
  const header = `${groupLabel.value},案件數\n`
  const body = rows.value.map(row => `"${row.label}",${row.value}`).join('\n')
  const blob = new Blob(['\uFEFF' + header + body], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = `cases_${filters.value.group_by}_${filters.value.date_from}_${filters.value.date_to}.csv`
  link.click()
  URL.revokeObjectURL(link.href)
}

onMounted(async () => {
  if (!meta.customers.length) {
    await meta.fetchDropdowns()
  }
  await fetchReport()
})
</script>

<style scoped>
@reference "tailwindcss";

.input-base {
  @apply w-full h-10 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500;
}
</style>
