<template>
  <div class="mx-auto flex w-full max-w-[1400px] flex-col gap-4">
    <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div>
        <div class="mb-1 flex items-center gap-1.5 text-xs text-slate-500">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">工時統計</span>
        </div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 md:text-[28px]">工時統計</h1>
        <p class="mt-1 text-sm text-slate-500">沿用案件列表的查詢節奏，依工程師、專案或客戶查看工時與案件數。</p>
      </div>
      <button @click="exportExcel" :disabled="exporting || rows.length === 0" class="h-9 rounded-lg bg-indigo-700 px-3.5 text-sm font-medium text-white hover:bg-indigo-800 disabled:cursor-not-allowed disabled:bg-slate-300">
        {{ exporting ? '匯出中…' : '匯出 Excel' }}
      </button>
    </div>

    <section class="grid gap-3 sm:grid-cols-3">
      <article class="rounded-2xl border border-slate-200 bg-white px-5 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">分群筆數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ rows.length }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-5 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">總計</div>
        <div class="mt-1 text-2xl font-semibold text-indigo-700 tabular-nums">{{ total }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-5 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">統計區間</div>
        <div class="mt-1 text-sm font-medium text-slate-900 tabular-nums">{{ filters.date_from }} 至 {{ filters.date_to }}</div>
      </article>
    </section>

    <div class="grid gap-4 xl:grid-cols-[320px_minmax(0,1fr)]">
      <section class="self-start rounded-2xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
        <div>
          <label class="label">起始日期</label>
          <input v-model="filters.date_from" type="date" class="input-base" />
        </div>
        <div>
          <label class="label">結束日期</label>
          <input v-model="filters.date_to" type="date" class="input-base" />
        </div>
        <div>
          <label class="label">分群方式</label>
          <select v-model="filters.group_by" class="input-base">
            <option value="se">工程師</option>
            <option value="project">專案</option>
            <option value="customer">客戶</option>
            <option value="category">問題分類</option>
            <option value="created_by">立案者</option>
            <option value="assigned_pm">轉派 PM</option>
          </select>
        </div>
        <div>
          <label class="label">指標</label>
          <select v-model="filters.metric" class="input-base">
            <option value="hours">工時</option>
            <option value="count">案件數</option>
          </select>
        </div>
        <div>
          <label class="label">專案</label>
          <select v-model="filters.project_id" class="input-base">
            <option value="">全部</option>
            <option v-for="p in meta.projects" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
          </select>
        </div>
        <div>
          <label class="label">客戶</label>
          <select v-model="filters.customer_id" class="input-base">
            <option value="">全部</option>
            <option v-for="c in meta.customers" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <button @click="fetchReport" class="w-full rounded-lg bg-indigo-700 py-2 text-sm font-medium text-white hover:bg-indigo-800">查詢</button>
      </section>

      <section class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div class="flex items-center justify-between border-b border-slate-100 px-5 py-4">
          <div>
            <span class="text-sm font-medium text-slate-900">{{ groupLabel }}統計</span>
            <span class="ml-2 text-xs text-slate-500">共 {{ rows.length }} 筆結果</span>
          </div>
          <div class="flex items-center gap-3">
            <select v-model="exportType" class="rounded-lg border border-slate-300 px-2 py-1 text-sm focus:ring-1 focus:ring-indigo-500">
              <option value="hours">完整匯出 (Excel)</option>
              <option value="hours_gsheets">Google Sheets 格式</option>
            </select>
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200">
            <thead class="bg-slate-50">
              <tr>
                <th class="th-cell">{{ groupLabel }}</th>
                <th class="th-cell text-right">{{ filters.metric === 'hours' ? '工時 (hr)' : '案件數' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr v-for="(row, idx) in rows" :key="idx" class="hover:bg-slate-50">
                <td class="td-cell">{{ row.label }}</td>
                <td class="td-cell text-right font-mono">{{ row.value }}</td>
              </tr>
              <tr v-if="rows.length > 0" class="bg-slate-50 font-bold">
                <td class="td-cell">合計</td>
                <td class="td-cell text-right font-mono">{{ total }}</td>
              </tr>
            </tbody>
          </table>
          <div v-if="rows.length === 0" class="py-16 text-center text-slate-400">按「查詢」取得報表</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const meta = useMetaStore()
const rows = ref([])
const exporting = ref(false)
const exportType = ref('hours')

const now = new Date()
const firstDay = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10)
const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10)

const filters = ref({
  date_from: firstDay,
  date_to: lastDay,
  group_by: 'se',
  metric: 'hours',
  project_id: '',
  customer_id: ''
})

const groupLabel = computed(() => ({
  se: '工程師', project: '專案', customer: '客戶', category: '問題分類', created_by: '立案者', assigned_pm: '轉派 PM'
}[filters.value.group_by]))

const total = computed(() => rows.value.reduce((s, r) => s + Number(r.value), 0))

async function fetchReport() {
  const params = { ...filters.value }
  if (!params.project_id) delete params.project_id
  if (!params.customer_id) delete params.customer_id
  const { data: res } = await api.get('/reports/hours', { params })
  if (res.success) {
    const isHours = filters.value.metric === 'hours'
    rows.value = (res.data || []).map(r => ({
      label: r.dimension,
      value: isHours ? (r.total_hours ?? 0) : (r.count ?? r.case_count ?? 0)
    }))
  }
}

async function exportExcel() {
  exporting.value = true
  try {
    const params = { ...filters.value, report_type: exportType.value }
    if (!params.project_id) delete params.project_id
    if (!params.customer_id) delete params.customer_id
    const res = await api.post('/reports/export', params, { responseType: 'blob' })
    const blob = new Blob([res.data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `report_${filters.value.group_by}_${filters.value.date_from}_${filters.value.date_to}.xlsx`
    link.click()
    URL.revokeObjectURL(link.href)
  } catch (e) {
    alert('匯出失敗，請稍後再試')
  } finally {
    exporting.value = false
  }
}

function exportCsv() {
  const header = `${groupLabel.value},${filters.value.metric === 'hours' ? '工時(hr)' : '案件數'}\n`
  const body = rows.value.map(r => `${r.label},${r.value}`).join('\n')
  const blob = new Blob(['\uFEFF' + header + body], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = `report_${filters.value.group_by}_${filters.value.date_from}_${filters.value.date_to}.csv`
  link.click()
}

onMounted(() => { if (!meta.customers.length) meta.fetchDropdowns() })
</script>

<style scoped>
@reference "tailwindcss";
.label { @apply mb-1 block text-[11px] font-medium text-slate-500 }
.input-base { @apply w-full h-10 rounded-lg border border-slate-300 px-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 }
.th-cell { @apply whitespace-nowrap px-5 py-3 text-left text-xs font-medium uppercase text-slate-500 }
.td-cell { @apply px-5 py-3.5 text-sm text-slate-700 }
</style>
