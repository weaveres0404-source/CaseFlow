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
        <button @click="fetchReport" :disabled="loading" class="w-full py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-60">
          {{ loading ? '查詢中...' : '查詢' }}
        </button>
      </div>

      <!-- Results -->
      <div class="flex-1 bg-white rounded-xl shadow-sm overflow-hidden">
        <div class="p-4 border-b border-gray-200 flex items-center justify-between">
          <span class="text-sm text-gray-500">共 {{ rows.length }} 筆</span>
          <div class="flex items-center gap-3">
            <button
              @click="exportExcel"
              :disabled="exporting"
              class="inline-flex items-center gap-2 rounded-lg border border-indigo-200 bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all hover:bg-indigo-700 hover:border-indigo-300 hover:shadow disabled:cursor-not-allowed disabled:opacity-60"
            >
              <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 3.75v10.5m0 0l-4.5-4.5m4.5 4.5l4.5-4.5M4.5 15.75v1.5A2.25 2.25 0 006.75 19.5h10.5a2.25 2.25 0 002.25-2.25v-1.5" />
              </svg>
              {{ exporting ? '匯出中...' : '匯出 Excel' }}
            </button>
          </div>
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
          <div v-if="errorMessage" class="text-center py-16 text-rose-500">{{ errorMessage }}</div>
          <div v-else-if="rows.length === 0" class="text-center py-16 text-gray-400">
            {{ hasSearched ? '查無符合條件的報表資料' : '按「查詢」取得報表' }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import ExcelJS from 'exceljs'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const meta = useMetaStore()
const rows = ref([])
const exporting = ref(false)
const loading = ref(false)
const hasSearched = ref(false)
const errorMessage = ref('')

const now = new Date()
const formatLocalDate = date => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
const firstDay = formatLocalDate(new Date(now.getFullYear(), now.getMonth(), 1))
const lastDay = formatLocalDate(new Date(now.getFullYear(), now.getMonth() + 1, 0))

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
const selectedProject = computed(() => meta.projects.find(p => Number(p.id) === Number(filters.value.project_id)) || null)
const selectedProjectExport = computed(() => {
  const project = selectedProject.value
  if (!project) return null
  return PROJECT_CUSTOM_EXPORTS.find(item => item.matches(project)) || null
})

const PROJECT_CUSTOM_EXPORTS = [
  {
    key: 'suda-hours',
    label: '速達客服格式',
    filenamePrefix: '速達客服_工時轉出Excel',
    matches: project => project.code === 'SUDA-CC' || project.name?.includes('統一速達客服系統維護')
  }
]

async function fetchReport() {
  loading.value = true
  hasSearched.value = true
  errorMessage.value = ''
  try {
    const params = { ...filters.value }
    if (!params.project_id) delete params.project_id
    if (!params.customer_id) delete params.customer_id
    const { data: res } = await api.get('/reports/hours', { params })
    if (!res.success) throw new Error(res.error?.message || 'report query failed')
    rows.value = (res.data || []).map(r => ({
      label: r.label ?? r.dimension ?? '未分類',
      value: filters.value.metric === 'count'
        ? Number(r.value ?? r.count ?? r.case_count ?? 0)
        : Number(r.value ?? r.total_hours ?? 0)
    }))
  } catch (e) {
    rows.value = []
    errorMessage.value = e?.response?.data?.error?.message || '查詢失敗，請稍後再試'
  } finally {
    loading.value = false
  }
}

async function exportExcel() {
  const customExport = selectedProjectExport.value
  if (customExport) {
    await exportCustomProjectExcel(customExport)
    return
  }

  exporting.value = true
  try {
    const params = { ...filters.value, report_type: 'hours' }
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

async function exportCustomProjectExcel(customExport) {
  if (!filters.value.project_id) {
    alert('請先選擇專案')
    return
  }

  exporting.value = true
  try {
    const params = {
      project_id: filters.value.project_id,
      date_from: filters.value.date_from,
      date_to: filters.value.date_to
    }
    const { data: res } = await api.get('/reports/custom/suda-hours', { params })
    if (!res.success) throw new Error('custom export failed')

    const workbook = buildSudaWorkbook(res.data?.rows || [])
    const buffer = await workbook.xlsx.writeBuffer()
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `${customExport.filenamePrefix}_${filters.value.date_from}_${filters.value.date_to}.xlsx`
    link.click()
    URL.revokeObjectURL(link.href)
  } catch (e) {
    alert('專案格式匯出失敗，請稍後再試')
  } finally {
    exporting.value = false
  }
}

function buildSudaWorkbook(sourceRows) {
  const workbook = new ExcelJS.Workbook()
  const sheet = workbook.addWorksheet('List')
  const headers = ['提出\n日期', '系統別', '反應內容', '負責單位', '狀態', '調查結果', '改善措施', '投入工時', '結案\n日期', '編號', '需求單', '分類', '處理人員']
  const headerFills = [
    'FFDEEAF6', 'FFFFFF00', 'FFDEEAF6', 'FFFFFF00', 'FFDEEAF6', 'FFDEEAF6', 'FFFFFF00',
    'FFDEEAF6', 'FFA8D08D', 'FFA8D08D', 'FFA8D08D', 'FFA8D08D', 'FFA8D08D'
  ]
  const baseFont = { name: 'PMingLiu', family: 1, charset: 136, size: 12, color: { theme: 1 } }

  sheet.getCell('A1').value = 'ㄋ'
  sheet.addRow(headers)

  sourceRows.forEach(row => {
    sheet.addRow([
      toExcelDate(row.submitted_date),
      row.system || '客服',
      row.response_content || '',
      row.owner_unit || '矩明',
      row.status || '',
      row.investigation_result || '',
      row.improvement_action || '',
      Number(row.hours_spent || 0),
      toExcelDate(row.closed_date),
      row.case_number || '',
      row.request_no || '',
      row.category || '',
      row.handlers || ''
    ])
  })

  sheet.columns = [
    { width: 7.125 },
    { width: 6.875 },
    { width: 24.75 },
    { width: 4.5625 },
    { width: 12.25 },
    { width: 22 },
    { width: 4.875 },
    { width: 6 },
    { width: 5.5625 },
    { width: 13.6875 },
    { width: 6.875 },
    { width: 14.3125 },
    { width: 12.3125 }
  ]

  sheet.getRow(1).height = 8.25
  sheet.getRow(2).height = 37.5

  for (let columnNumber = 1; columnNumber <= 13; columnNumber += 1) {
    const topCell = sheet.getCell(1, columnNumber)
    topCell.font = baseFont
    topCell.alignment = columnNumber <= 5 || columnNumber === 9 || columnNumber === 12
      ? { horizontal: 'center', vertical: 'middle' }
      : { vertical: 'middle', wrapText: columnNumber === 6 || columnNumber === 7 }

    const headerCell = sheet.getCell(2, columnNumber)
    headerCell.font = baseFont
    headerCell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: headerFills[columnNumber - 1] },
      bgColor: { argb: headerFills[columnNumber - 1] }
    }
    headerCell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: headers[columnNumber - 1].includes('\n') || [2, 3, 4, 6, 7, 9].includes(columnNumber) }
    headerCell.border = tableBorder()
    if (columnNumber === 8) headerCell.numFmt = '0.0_);[Red](0.0)'
  }

  for (let rowNumber = 3; rowNumber <= sheet.rowCount; rowNumber += 1) {
    const row = sheet.getRow(rowNumber)
    row.height = sudaRowHeight(row)

    for (let columnNumber = 1; columnNumber <= 13; columnNumber += 1) {
      const cell = row.getCell(columnNumber)
      cell.font = columnNumber === 11
        ? { name: 'Calibri', family: 2, size: 12, color: { theme: 1 } }
        : baseFont
      cell.fill = { type: 'pattern', pattern: 'none' }
      cell.border = tableBorder()
      cell.alignment = sudaCellAlignment(columnNumber)
    }

    row.getCell(1).numFmt = 'mm/dd'
    row.getCell(8).numFmt = '0.0_);[Red](0.0)'
    row.getCell(9).numFmt = 'm/d'
  }

  sheet.autoFilter = 'A2:M2'
  sheet.views = [{
    state: 'frozen',
    xSplit: 1,
    ySplit: 2,
    topLeftCell: 'B5',
    activeCell: 'A2',
    showGridLines: true
  }]
  return workbook
}

function tableBorder() {
  return {
    top: { style: 'thin', color: { argb: 'FFB7B7B7' } },
    left: { style: 'thin', color: { argb: 'FFB7B7B7' } },
    bottom: { style: 'thin', color: { argb: 'FFB7B7B7' } },
    right: { style: 'thin', color: { argb: 'FFB7B7B7' } }
  }
}

function sudaCellAlignment(columnNumber) {
  if ([1, 2, 4, 5, 9, 12].includes(columnNumber)) {
    return { horizontal: 'center', vertical: 'middle' }
  }
  if ([3, 6, 10].includes(columnNumber)) {
    return { horizontal: 'left', vertical: 'middle', wrapText: columnNumber === 6 }
  }
  if ([7, 13].includes(columnNumber)) {
    return { vertical: 'middle', wrapText: true }
  }
  return { vertical: 'middle' }
}

function sudaRowHeight(row) {
  const lineCount = [3, 6, 7, 13].reduce((maxLines, columnNumber) => {
    const value = row.getCell(columnNumber).value
    const text = value == null ? '' : String(value)
    return Math.max(maxLines, text.split('\n').length)
  }, 1)
  return Math.max(29.25, Math.min(90, 18 + lineCount * 7.5))
}

function toExcelDate(value) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
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
