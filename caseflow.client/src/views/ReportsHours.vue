<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-bold text-gray-900">工時報表</h2>

    <div class="flex flex-col lg:flex-row gap-6 lg:gap-8">
      <!-- Filters -->
      <div class="w-full lg:w-80 bg-white rounded-xl shadow-sm overflow-hidden p-5 space-y-4 self-start">
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
        <button @click="fetchReport" class="w-full py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700">查詢</button>
      </div>

      <!-- Results -->
      <div class="flex-1 bg-white rounded-xl shadow-sm overflow-hidden">
        <div class="p-4 border-b border-gray-200 flex items-center justify-between">
          <span class="text-sm text-gray-500">共 {{ rows.length }} 筆</span>
          <button @click="exportCsv" class="text-sm text-indigo-600 font-medium hover:underline">匯出 CSV</button>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="th-cell">{{ groupLabel }}</th>
                <th class="th-cell text-right">{{ filters.metric === 'hours' ? '工時 (hr)' : '案件數' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(row, idx) in rows" :key="idx" class="hover:bg-gray-50">
                <td class="td-cell">{{ row.label }}</td>
                <td class="td-cell text-right font-mono">{{ row.value }}</td>
              </tr>
              <tr v-if="rows.length > 0" class="bg-gray-50 font-bold">
                <td class="td-cell">合計</td>
                <td class="td-cell text-right font-mono">{{ total }}</td>
              </tr>
            </tbody>
          </table>
          <div v-if="rows.length === 0" class="text-center py-16 text-gray-400">按「查詢」取得報表</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const meta = useMetaStore()
const rows = ref([])

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
    rows.value = (res.data || []).map(r => ({ label: r.label, value: r.value }))
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
.label { @apply block text-sm font-medium text-gray-700 mb-1 }
.input-base { @apply w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 }
.th-cell { @apply px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap }
.td-cell { @apply px-4 py-3 text-sm whitespace-nowrap }
</style>
