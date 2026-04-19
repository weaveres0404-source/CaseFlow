<template>
  <div class="space-y-6" v-if="caseData">
    <!-- Header -->
    <div class="bg-white rounded-xl shadow-sm overflow-hidden p-6">
      <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <div class="flex items-center gap-3">
            <h2 class="text-2xl font-bold text-gray-900">{{ caseData.case_number }}</h2>
            <span class="badge" :class="meta.statusMap[caseData.status]?.color">{{ meta.statusMap[caseData.status]?.label }}</span>
            <span class="badge" :class="meta.caseTypeMap[caseData.case_type]?.color">{{ meta.caseTypeMap[caseData.case_type]?.label }}</span>
            <span class="badge" :class="meta.priorityMap[caseData.priority]?.color">{{ meta.priorityMap[caseData.priority]?.label }}</span>
          </div>
          <p class="text-sm text-gray-500 mt-1 line-clamp-1">{{ caseData.description?.substring(0, 60) }}</p>
        </div>
        <div class="flex flex-wrap gap-2">
          <button v-for="action in availableActions" :key="action.label"
            @click="action.handler()" :class="action.class" class="px-4 py-2 rounded-lg text-sm font-medium">
            {{ action.label }}
          </button>
        </div>
      </div>
      <!-- Key personnel -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-6 mt-4 pt-4 border-t border-gray-100">
        <div>
          <span class="text-xs text-gray-400">立案 PM</span>
          <p class="text-sm font-medium">{{ caseData.created_by?.full_name }}</p>
        </div>
        <div>
          <span class="text-xs text-gray-400">轉派 PM</span>
          <p class="text-sm font-medium">{{ caseData.assigned_pm?.full_name || '—' }}</p>
        </div>
        <div>
          <span class="text-xs text-gray-400">專案</span>
          <p class="text-sm font-medium">{{ caseData.project?.code }} - {{ caseData.project?.name }}</p>
        </div>
        <div>
          <span class="text-xs text-gray-400">客戶</span>
          <p class="text-sm font-medium">{{ caseData.customer?.name }}</p>
        </div>
      </div>
      <div v-if="caseData.related_case" class="mt-2">
        <router-link :to="`/cases/${caseData.related_case.id}`" class="text-sm text-indigo-600 hover:underline">
          重開來源：{{ caseData.related_case.case_number }}
        </router-link>
      </div>
    </div>

    <!-- Tabs -->
    <div class="bg-white rounded-xl shadow-sm overflow-hidden">
      <div class="border-b border-gray-200 overflow-x-auto">
        <nav class="flex -mb-px">
          <button v-for="tab in tabs" :key="tab.key" @click="activeTab = tab.key"
            class="px-4 py-3 text-sm font-medium whitespace-nowrap border-b-2 transition-colors"
            :class="activeTab === tab.key ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
            {{ tab.label }}
            <span v-if="tab.count != null" class="ml-1 text-xs bg-gray-100 rounded-full px-2">{{ tab.count }}</span>
          </button>
        </nav>
      </div>

      <div class="p-6">
        <!-- Tab: 基本資訊 -->
        <div v-if="activeTab === 'info'" class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div><span class="text-xs text-gray-400">問題分類</span><p class="text-sm">{{ caseData.category?.name }}</p></div>
            <div><span class="text-xs text-gray-400">系統/模組</span><p class="text-sm">{{ caseData.module?.name || '—' }}</p></div>
            <div><span class="text-xs text-gray-400">報修人</span><p class="text-sm">{{ caseData.reporter_name }}</p></div>
            <div><span class="text-xs text-gray-400">聯絡電話</span><p class="text-sm">{{ caseData.reporter_phone || '—' }}</p></div>
            <div><span class="text-xs text-gray-400">聯絡 Email</span><p class="text-sm">{{ caseData.reporter_email || '—' }}</p></div>
          </div>
          <div>
            <span class="text-xs text-gray-400">問題描述</span>
            <p class="text-sm mt-1 whitespace-pre-wrap bg-gray-50 p-3 rounded-lg">{{ caseData.description }}</p>
          </div>
          <!-- Attachments -->
          <div v-if="caseAttachments.length > 0">
            <span class="text-xs text-gray-400">附件</span>
            <div class="mt-2 space-y-2">
              <div v-for="att in caseAttachments" :key="att.id" class="flex items-center justify-between p-2 bg-gray-50 rounded-lg">
                <div class="text-sm"><span class="font-medium">{{ att.file_name }}</span> <span class="text-gray-400 ml-2">{{ formatFileSize(att.file_size) }}</span></div>
                <a :href="`/api/v1/attachments/${att.id}/download`" class="text-indigo-600 text-sm hover:underline">下載</a>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab: 處理歷程 -->
        <div v-if="activeTab === 'logs'" class="space-y-4">
          <div class="flex items-center justify-between">
            <div class="text-sm text-gray-500">
              總工時：<strong>{{ caseData.summary?.total_hours }}</strong> hr ｜ 總人次：<strong>{{ caseData.summary?.total_headcount }}</strong> ｜ {{ caseData.logs?.length }} 筆紀錄
            </div>
            <button @click="showLogForm = !showLogForm" class="text-sm text-indigo-600 font-medium">+ 新增紀錄</button>
          </div>
          <!-- Add log form -->
          <div v-if="showLogForm" class="bg-gray-50 p-4 rounded-lg space-y-3">
            <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div><label class="label">處理日期 *</label><input v-model="logForm.log_date" type="date" class="input-base" /></div>
              <div><label class="label">工時(hr) *</label><input v-model.number="logForm.hours_spent" type="number" step="0.5" min="0" class="input-base" /></div>
              <div><label class="label">人次</label><input v-model.number="logForm.headcount" type="number" min="1" class="input-base" /></div>
            </div>
            <div><label class="label">處理方式 *</label><textarea v-model="logForm.handling_method" rows="2" class="input-base"></textarea></div>
            <div><label class="label">處理結果</label><textarea v-model="logForm.handling_result" rows="2" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showLogForm = false" class="px-3 py-1 text-sm border rounded-lg">取消</button>
              <button @click="submitLog" class="px-3 py-1 text-sm bg-indigo-600 text-white rounded-lg">送出</button>
            </div>
          </div>
          <!-- Log timeline -->
          <div class="space-y-3">
            <div v-for="log in (caseData.logs || [])" :key="log.id" class="border-l-2 border-indigo-200 pl-4 pb-4">
              <div class="flex items-center gap-3">
                <span class="text-sm font-bold">{{ log.log_date }}</span>
                <span class="text-sm text-gray-500">{{ log.handler?.full_name }}</span>
                <span class="badge bg-blue-100 text-blue-800">{{ log.hours_spent }}hr / {{ log.headcount }}人</span>
              </div>
              <p class="text-sm mt-1"><strong>處理方式：</strong>{{ log.handling_method }}</p>
              <p v-if="log.handling_result" class="text-sm text-gray-600"><strong>處理結果：</strong>{{ log.handling_result }}</p>
            </div>
          </div>
        </div>

        <!-- Tab: 派工 -->
        <div v-if="activeTab === 'assign'" class="space-y-4">
          <button @click="showAssignForm = !showAssignForm" class="text-sm text-indigo-600 font-medium">派工 / 改派</button>
          <div v-if="showAssignForm" class="bg-gray-50 p-4 rounded-lg space-y-3">
            <div>
              <label class="label">選擇 SE（可多選，☆ 為主要負責人）</label>
              <div class="space-y-2 mt-2">
                <div v-for="se in availableSEs" :key="se.id" class="flex items-center gap-3">
                  <input type="checkbox" :value="se.id" v-model="assignForm.se_user_ids" class="rounded" />
                  <span class="text-sm">{{ se.full_name }}</span>
                  <button v-if="assignForm.se_user_ids.includes(se.id)" @click="assignForm.primary_se_user_id = se.id"
                    class="text-xs" :class="assignForm.primary_se_user_id === se.id ? 'text-yellow-500' : 'text-gray-300'">☆</button>
                </div>
              </div>
            </div>
            <div><label class="label">處理指示</label><textarea v-model="assignForm.instructions" rows="2" class="input-base"></textarea></div>
            <div><label class="label">預計完成日</label><input v-model="assignForm.expected_completion_date" type="date" class="input-base" /></div>
            <div class="flex justify-end gap-2">
              <button @click="showAssignForm = false" class="px-3 py-1 text-sm border rounded-lg">取消</button>
              <button @click="submitAssign" class="px-3 py-1 text-sm bg-indigo-600 text-white rounded-lg">確認派工</button>
            </div>
          </div>
          <!-- Current assignments -->
          <div>
            <h4 class="text-sm font-medium text-gray-700 mb-2">目前派工</h4>
            <div class="space-y-2">
              <div v-for="a in activeAssignments" :key="a.id" class="flex items-center gap-3 p-2 bg-gray-50 rounded-lg">
                <span v-if="a.is_primary" class="text-yellow-500">☆</span>
                <span class="text-sm font-medium">{{ a.se?.full_name }}</span>
                <span class="text-xs text-gray-400">派工人：{{ a.assigned_by?.full_name }}</span>
                <span v-if="a.expected_completion_date" class="text-xs text-gray-400">預計完成：{{ a.expected_completion_date }}</span>
              </div>
              <div v-if="activeAssignments.length === 0" class="text-sm text-gray-400">尚未派工</div>
            </div>
          </div>
        </div>

        <!-- Tab: 工時評估 -->
        <div v-if="activeTab === 'estimation'" class="space-y-4">
          <button @click="showEstForm = !showEstForm" class="text-sm text-indigo-600 font-medium">+ 新增評估</button>
          <div v-if="showEstForm" class="bg-gray-50 p-4 rounded-lg space-y-3">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div><label class="label">提出日期 *</label><input v-model="estForm.request_date" type="date" class="input-base" /></div>
              <div><label class="label">評估工時(hr) *</label><input v-model.number="estForm.estimated_hours" type="number" step="0.5" class="input-base" /></div>
              <div>
                <label class="label">評估人員 *</label>
                <select v-model="estForm.estimator_user_id" class="input-base">
                  <option v-for="u in meta.users" :key="u.id" :value="u.id">{{ u.full_name }} ({{ u.role }})</option>
                </select>
              </div>
            </div>
            <div><label class="label">概要 *</label><textarea v-model="estForm.summary" rows="2" class="input-base"></textarea></div>
            <div><label class="label">備註</label><textarea v-model="estForm.remarks" rows="2" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showEstForm = false" class="px-3 py-1 text-sm border rounded-lg">取消</button>
              <button @click="submitEstimation" class="px-3 py-1 text-sm bg-indigo-600 text-white rounded-lg">送出</button>
            </div>
          </div>
          <table class="min-w-full divide-y divide-gray-200" v-if="(caseData.estimations || []).length > 0">
            <thead class="bg-gray-50"><tr>
              <th class="th-cell">項次</th><th class="th-cell">提出日</th><th class="th-cell">概要</th>
              <th class="th-cell">評估工時</th><th class="th-cell">狀態</th><th class="th-cell">評估人員</th>
            </tr></thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="e in caseData.estimations" :key="e.id">
                <td class="td-cell">{{ e.seq_no }}</td>
                <td class="td-cell">{{ e.request_date }}</td>
                <td class="td-cell">{{ e.summary }}</td>
                <td class="td-cell">{{ e.estimated_hours }} hr</td>
                <td class="td-cell"><span class="badge" :class="estStatusColor(e.estimation_status)">{{ estStatusLabel(e.estimation_status) }}</span></td>
                <td class="td-cell">{{ e.estimator?.full_name }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Tab: 客戶回覆 -->
        <div v-if="activeTab === 'replies'" class="space-y-4">
          <button @click="showReplyForm = !showReplyForm" class="text-sm text-indigo-600 font-medium">+ 新增回覆</button>
          <div v-if="showReplyForm" class="bg-gray-50 p-4 rounded-lg space-y-3">
            <div><label class="label">回覆日期</label><input v-model="replyForm.reply_date" type="date" class="input-base" /></div>
            <div><label class="label">回覆內容 *</label><textarea v-model="replyForm.reply_content" rows="4" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showReplyForm = false" class="px-3 py-1 text-sm border rounded-lg">取消</button>
              <button @click="submitReply" class="px-3 py-1 text-sm bg-indigo-600 text-white rounded-lg">送出</button>
            </div>
          </div>
          <div class="space-y-3">
            <div v-for="r in (caseData.replies || [])" :key="r.id" class="border-l-2 border-green-200 pl-4 pb-4">
              <div class="flex items-center gap-3">
                <span class="text-sm font-bold">{{ r.reply_date }}</span>
                <span class="text-sm text-gray-500">{{ r.replier?.full_name }}</span>
              </div>
              <p class="text-sm mt-1 whitespace-pre-wrap">{{ r.reply_content }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- Confirm Dialog -->
  <div v-if="confirmDialog.show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
    <div class="bg-white rounded-xl p-6 max-w-md w-full mx-4">
      <h3 class="text-lg font-semibold mb-2">{{ confirmDialog.title }}</h3>
      <p class="text-sm text-gray-600 mb-4">{{ confirmDialog.message }}</p>
      <div class="flex justify-end gap-3">
        <button @click="confirmDialog.show = false" class="px-4 py-2 border rounded-lg text-sm">取消</button>
        <button @click="confirmDialog.onConfirm(); confirmDialog.show = false" :class="confirmDialog.btnClass" class="px-4 py-2 rounded-lg text-sm text-white">確認</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useMetaStore } from '../stores/meta'
import { useAuthStore } from '../stores/auth'
import api from '../utils/api'

const route = useRoute()
const router = useRouter()
const meta = useMetaStore()
const auth = useAuthStore()

const caseData = ref(null)
const activeTab = ref('info')

const showLogForm = ref(false)
const showAssignForm = ref(false)
const showEstForm = ref(false)
const showReplyForm = ref(false)

const logForm = ref({ log_date: new Date().toISOString().slice(0, 10), handling_method: '', handling_result: '', hours_spent: 0, headcount: 1 })
const assignForm = ref({ se_user_ids: [], primary_se_user_id: null, instructions: '', expected_completion_date: '' })
const estForm = ref({ request_date: new Date().toISOString().slice(0, 10), summary: '', estimated_hours: 0, estimator_user_id: null, remarks: '' })
const replyForm = ref({ reply_date: new Date().toISOString().slice(0, 10), reply_content: '' })
const confirmDialog = ref({ show: false, title: '', message: '', onConfirm: () => {}, btnClass: 'bg-indigo-600' })

const caseId = computed(() => route.params.id)

const tabs = computed(() => [
  { key: 'info', label: '基本資訊' },
  { key: 'logs', label: '處理歷程', count: caseData.value?.logs?.length || 0 },
  { key: 'assign', label: '派工', count: activeAssignments.value.length },
  { key: 'estimation', label: '工時評估', count: caseData.value?.estimations?.length || 0 },
  { key: 'replies', label: '客戶回覆', count: caseData.value?.replies?.length || 0 }
])

const activeAssignments = computed(() => (caseData.value?.assignments || []).filter(a => a.is_active))

const caseAttachments = computed(() => (caseData.value?.attachments || []).filter(a => a.entity_type === 'case'))

const availableSEs = computed(() => {
  if (!caseData.value) return []
  return meta.getProjectSEs(caseData.value.project?.id) || meta.users.filter(u => u.role === 'SE')
})

const availableActions = computed(() => {
  if (!caseData.value) return []
  const s = caseData.value.status
  const r = auth.role
  const actions = []

  if ([10, 35].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '派工', handler: () => { activeTab.value = 'assign'; showAssignForm.value = true }, class: 'bg-blue-600 text-white hover:bg-blue-700' })
  if ([20, 35].includes(s) && ['SE', 'SysAdmin'].includes(r))
    actions.push({ label: '開始處理', handler: () => doAction('start', '確認開始處理此案件？'), class: 'bg-yellow-500 text-white hover:bg-yellow-600' })
  if (s === 30 && ['SE', 'SysAdmin'].includes(r))
    actions.push({ label: '回報完工', handler: () => doAction('complete', '確認此案件已完工？'), class: 'bg-green-600 text-white hover:bg-green-700' })
  if (s === 40 && ['PM', 'SysAdmin'].includes(r)) {
    actions.push({ label: '結案', handler: () => doAction('close', '確認結案？'), class: 'bg-emerald-600 text-white hover:bg-emerald-700' })
    actions.push({ label: '退回', handler: () => doAction('return', '確認退回此案件？'), class: 'bg-red-500 text-white hover:bg-red-600' })
  }
  if ([10, 20, 30, 35, 40].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '取消', handler: () => doAction('cancel', '確認取消此案件？此操作不可逆'), class: 'bg-gray-600 text-white hover:bg-gray-700' })
  if ([50, 60].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '重開', handler: () => doAction('reopen', '將以舊案為範本建立新案件'), class: 'bg-indigo-600 text-white hover:bg-indigo-700' })

  return actions
})

async function fetchCase() {
  const { data: res } = await api.get(`/cases/${caseId.value}`)
  if (res.success) caseData.value = res.data
}

function doAction(action, message) {
  confirmDialog.value = {
    show: true,
    title: `確認操作`,
    message,
    btnClass: action === 'cancel' ? 'bg-red-600' : 'bg-indigo-600',
    onConfirm: async () => {
      await api.post(`/cases/${caseId.value}/${action}`)
      if (action === 'reopen') {
        // Redirect to new case
        const { data: res } = await api.post(`/cases/${caseId.value}/${action}`)
        if (res.success) router.push(`/cases/${res.data.id}`)
      } else {
        await fetchCase()
      }
    }
  }
}

async function submitLog() {
  await api.post(`/cases/${caseId.value}/logs`, logForm.value)
  showLogForm.value = false
  logForm.value = { log_date: new Date().toISOString().slice(0, 10), handling_method: '', handling_result: '', hours_spent: 0, headcount: 1 }
  await fetchCase()
}

async function submitAssign() {
  await api.post(`/cases/${caseId.value}/assign`, assignForm.value)
  showAssignForm.value = false
  await fetchCase()
}

async function submitEstimation() {
  await api.post(`/cases/${caseId.value}/estimations`, estForm.value)
  showEstForm.value = false
  estForm.value = { request_date: new Date().toISOString().slice(0, 10), summary: '', estimated_hours: 0, estimator_user_id: null, remarks: '' }
  await fetchCase()
}

async function submitReply() {
  await api.post(`/cases/${caseId.value}/replies`, replyForm.value)
  showReplyForm.value = false
  replyForm.value = { reply_date: new Date().toISOString().slice(0, 10), reply_content: '' }
  await fetchCase()
}

function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function estStatusLabel(s) { return { 10: '待評估', 20: '評估中', 30: '已回覆' }[s] || s }
function estStatusColor(s) { return { 10: 'bg-gray-100 text-gray-800', 20: 'bg-yellow-100 text-yellow-800', 30: 'bg-green-100 text-green-800' }[s] }

onMounted(fetchCase)
</script>

<style scoped>
@reference "tailwindcss";
.label { @apply block text-sm font-medium text-gray-700 mb-1 }
.input-base { @apply w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 }
.badge { @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium }
.th-cell { @apply px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap }
.td-cell { @apply px-4 py-3 text-sm whitespace-nowrap }
</style>
