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
      <button @click="exportCsv" :disabled="rows.length === 0" class="h-9 px-3.5 rounded-lg bg-brand-700 hover:bg-brand-800 disabled:bg-slate-300 text-sm font-medium text-white">匯出 CSV</button>
    </div>

    <section class="grid gap-3 md:grid-cols-3">
      <article class="bg-white border border-slate-200 rounded-2xl px-4 py-4 shadow-sm">
        <div class="text-xs text-slate-500">分群筆數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ rows.length }}</div>
      </article>
      <article class="bg-white border border-slate-200 rounded-2xl px-4 py-4 shadow-sm">
        <div class="text-xs text-slate-500">案件總數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ total }}</div>
      </article>
      <article class="bg-white border border-slate-200 rounded-2xl px-4 py-4 shadow-sm">
        <div class="text-xs text-slate-500">統計區間</div>
        <div class="mt-1 text-sm font-medium text-slate-900 tabular-nums">{{ periodLabel }}</div>
      </article>
    </section>

    <section class="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden">
      <div class="px-5 py-4 border-b border-slate-100 grid gap-3 lg:grid-cols-[repeat(5,minmax(0,1fr))_auto] lg:items-end">
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
            <option value="status">狀態</option>
            <option value="project">專案</option>
            <option value="customer">客戶</option>
            <option value="category">問題分類</option>
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
        <button @click="fetchReport" class="h-10 px-4 rounded-lg bg-brand-700 hover:bg-brand-800 text-sm font-medium text-white">查詢</button>
      </div>

      <div v-if="requestError" class="mx-5 mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ requestError }}</div>

      <div class="px-5 py-4 border-b border-slate-100 flex items-center justify-between gap-3 text-sm">
        <div>
          <div class="font-medium text-slate-900">{{ groupLabel }}分布</div>
          <div class="text-xs text-slate-500 mt-0.5">顯示各群組的案件總量與相對占比。</div>
        </div>
        <div class="text-xs text-slate-500 tabular-nums">{{ rows.length }} 筆結果</div>
      </div>

      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead class="bg-slate-50 text-slate-500 text-left">
            <tr class="[&_th]:px-5 [&_th]:py-3 [&_th]:font-medium [&_th]:whitespace-nowrap">
              <th class="w-16">#</th>
              <th>{{ groupLabel }}</th>
              <th class="text-right">案件數</th>
              <th class="w-56">占比</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="(row, idx) in rows" :key="row.label + idx" class="hover:bg-slate-50">
              <td class="px-5 py-4 text-slate-400 tabular-nums">{{ idx + 1 }}</td>
              <td class="px-5 py-4 font-medium text-slate-900">{{ row.label }}</td>
              <td class="px-5 py-4 text-right font-medium text-slate-900 tabular-nums">{{ row.value }}</td>
              <td class="px-5 py-4">
                <div class="h-2 rounded-full bg-slate-100 overflow-hidden">
                  <div class="h-full bg-brand-700 rounded-full" :style="{ width: `${row.percentage}%` }"></div>
                </div>
              </td>
            </tr>
            <tr v-if="!loading && rows.length === 0">
              <td colspan="4" class="px-5 py-16 text-center text-slate-400">沒有符合條件的報表資料</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="loading" class="px-5 py-10 text-center text-sm text-slate-400">載入中…</div>
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

const now = new Date()
const firstDay = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10)
const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10)

const filters = ref({
  date_from: firstDay,
  date_to: lastDay,
  group_by: 'project',
  project_id: '',
  customer_id: ''
})

const groupLabel = computed(() => ({
  status: '狀態',
  project: '專案',
  customer: '客戶',
  category: '問題分類',
  created_by: '立案者',
  assigned_pm: '轉派 PM'
}[filters.value.group_by]))

const total = computed(() => rows.value.reduce((sum, row) => sum + Number(row.value || 0), 0))
const periodLabel = computed(() => `${filters.value.date_from || '—'} 至 ${filters.value.date_to || '—'}`)

async function fetchReport() {
  loading.value = true
  requestError.value = ''
  try {
    const params = { ...filters.value, metric: 'count' }
    if (!params.project_id) delete params.project_id
    if (!params.customer_id) delete params.customer_id
    const { data: res } = await api.get('/reports/hours', { params })
    if (res.success) {
      const mapped = (res.data || []).map((item) => ({
        label: item.dimension || '未命名',
        value: Number(item.count || 0)
      }))
      const localMax = Math.max(...mapped.map(row => row.value), 0)
      rows.value = mapped.map((row) => ({
        ...row,
        percentage: localMax ? Number(((row.value / localMax) * 100).toFixed(1)) : 0
      }))
    }
  } catch (error) {
    requestError.value = error?.response?.data?.error?.message || '讀取案件統計失敗，請稍後再試'
    rows.value = []
  } finally {
    loading.value = false
  }
}

function exportCsv() {
  const header = `${groupLabel.value},案件數\n`
  const body = rows.value.map(row => `${row.label},${row.value}`).join('\n')
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
