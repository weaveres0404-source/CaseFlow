<template>
  <div class="space-y-5">
    <div class="flex items-center justify-between">
      <h2 class="text-2xl font-bold text-gray-900">案件管理</h2>
      <router-link to="/cases/new" class="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm font-medium shadow-sm">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        新增案件
      </router-link>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl shadow-sm overflow-hidden p-5 lg:p-6">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-sm font-semibold text-gray-700">篩選條件</h3>
        <button @click="showFilters = !showFilters" class="text-xs text-gray-400 hover:text-gray-600 lg:hidden">{{ showFilters ? '收合 ▲' : '展開 ▼' }}</button>
      </div>
      <div :class="showFilters ? 'grid' : 'hidden lg:grid'" class="grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <input v-model="filters.q" placeholder="搜尋案件編號/報修人/描述" class="input-base" @keyup.enter="fetchCases" />
        <select v-model="filters.project_id" class="input-base">
          <option :value="null">全部專案</option>
          <option v-for="p in meta.projects" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
        </select>
        <select v-model="filters.status" class="input-base">
          <option :value="null">全部狀態</option>
          <option v-for="s in meta.enums.statuses" :key="s.value" :value="s.value">{{ s.label }}</option>
        </select>
        <select v-model="filters.priority" class="input-base">
          <option :value="null">全部優先度</option>
          <option value="HIGH">高</option>
          <option value="MEDIUM">中</option>
          <option value="LOW">低</option>
        </select>
        <select v-model="filters.case_type" class="input-base">
          <option :value="null">全部類型</option>
          <option v-for="t in meta.enums.case_types" :key="t.value" :value="t.value">{{ t.label }}</option>
        </select>
        <input v-model="filters.date_from" type="date" class="input-base" placeholder="開始日期" />
        <input v-model="filters.date_to" type="date" class="input-base" placeholder="結束日期" />
        <button @click="fetchCases" class="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm hover:bg-indigo-700">搜尋</button>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="th-cell sticky left-0 bg-gray-50 z-10">案件編號</th>
            <th class="th-cell">專案</th>
            <th class="th-cell">客戶</th>
            <th class="th-cell">類型</th>
            <th class="th-cell">優先度</th>
            <th class="th-cell">狀態</th>
            <th class="th-cell">立案 PM</th>
            <th class="th-cell">轉派 PM</th>
            <th class="th-cell">主要 SE</th>
            <th class="th-cell">最後更新</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="c in cases" :key="c.id" class="hover:bg-gray-50 cursor-pointer" @click="$router.push(`/cases/${c.id}`)">
            <td class="td-cell sticky left-0 bg-white font-medium text-indigo-600">{{ c.case_number }}</td>
            <td class="td-cell">
              <div class="text-sm">{{ c.project?.code }}</div>
              <div class="text-xs text-gray-400">{{ c.project?.name }}</div>
            </td>
            <td class="td-cell">{{ c.customer?.name }}</td>
            <td class="td-cell"><span class="badge" :class="meta.caseTypeMap[c.case_type]?.color">{{ meta.caseTypeMap[c.case_type]?.label }}</span></td>
            <td class="td-cell"><span class="badge" :class="meta.priorityMap[c.priority]?.color">{{ meta.priorityMap[c.priority]?.label }}</span></td>
            <td class="td-cell"><span class="badge" :class="meta.statusMap[c.status]?.color">{{ meta.statusMap[c.status]?.label }}</span></td>
            <td class="td-cell">{{ c.created_by?.full_name }}</td>
            <td class="td-cell">{{ c.assigned_pm?.full_name || '—' }}</td>
            <td class="td-cell">{{ c.primary_se?.full_name || '—' }}</td>
            <td class="td-cell text-sm text-gray-500">{{ formatDate(c.updated_at) }}</td>
          </tr>
          <tr v-if="cases.length === 0">
            <td colspan="10" class="p-8 text-center text-gray-400">暫無案件</td>
          </tr>
        </tbody>
      </table>
      </div>
    </div>

    <!-- Pagination -->
    <div class="flex items-center justify-between">
      <span class="text-sm text-gray-500">共 {{ totalCount }} 筆</span>
      <div class="flex gap-2">
        <button :disabled="page <= 1" @click="page--; fetchCases()" class="px-3 py-1 border rounded text-sm disabled:opacity-40">上一頁</button>
        <span class="px-3 py-1 text-sm">{{ page }} / {{ totalPages }}</span>
        <button :disabled="page >= totalPages" @click="page++; fetchCases()" class="px-3 py-1 border rounded text-sm disabled:opacity-40">下一頁</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const meta = useMetaStore()
const cases = ref([])
const page = ref(1)
const pageSize = 20
const totalCount = ref(0)
const totalPages = ref(1)
const showFilters = ref(true)

const filters = ref({
  q: null,
  project_id: null,
  status: null,
  priority: null,
  case_type: null,
  date_from: null,
  date_to: null
})

async function fetchCases() {
  const params = { page: page.value, page_size: pageSize }
  Object.entries(filters.value).forEach(([k, v]) => { if (v) params[k] = v })
  const { data: res } = await api.get('/cases', { params })
  if (res.success) {
    cases.value = res.data
    totalCount.value = res.meta.total
    totalPages.value = Math.max(1, Math.ceil(res.meta.total / pageSize))
  }
}

function formatDate(dt) {
  if (!dt) return ''
  return new Date(dt).toLocaleDateString('zh-TW', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

onMounted(fetchCases)
</script>

<style scoped>
@reference "tailwindcss";
.input-base { @apply w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 }
.th-cell { @apply px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap }
.td-cell { @apply px-4 py-3 text-sm whitespace-nowrap }
.badge { @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium }
</style>
