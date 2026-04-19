<template>
  <div>
    <h2 class="text-2xl font-bold text-gray-900 mb-6">案件立案</h2>

    <form @submit.prevent="submit" class="bg-white rounded-xl shadow-sm overflow-hidden p-6 space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- 專案 -->
        <div>
          <label class="label">專案 <span class="text-red-500">*</span></label>
          <select v-model="form.project_id" required class="input-base" @change="onProjectChange">
            <option :value="null" disabled>請選擇</option>
            <option v-for="p in meta.projects" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
          </select>
        </div>
        <!-- 客戶：可選或自填 -->
        <div>
          <label class="label">客戶 <span class="text-red-500">*</span></label>
          <input list="customers" v-model="customerInput" @change="onCustomerInputChange" required class="input-base" placeholder="從下拉選或直接輸入新客戶名稱" />
          <datalist id="customers">
            <option v-for="c in meta.customers" :key="c.id" :value="c.name"></option>
          </datalist>
          <p class="text-xs text-gray-400 mt-1">可從選單選現有客戶，或輸入新客戶名稱（系統會建立或以名稱送出）。</p>
        </div>
        <!-- 報修人 -->
        <div>
          <label class="label">報修人 <span class="text-red-500">*</span></label>
          <input v-model="form.reporter_name" required class="input-base" />
        </div>
        <!-- 聯絡電話 -->
        <div>
          <label class="label">聯絡電話</label>
          <input v-model="form.reporter_phone" class="input-base" />
        </div>
        <!-- 聯絡 Email -->
        <div>
          <label class="label">聯絡 Email</label>
          <input v-model="form.reporter_email" type="email" class="input-base" />
        </div>
        <!-- 案件類型 -->
        <div>
          <label class="label">案件類型 <span class="text-red-500">*</span></label>
          <select v-model="form.case_type" required class="input-base">
            <option v-for="t in caseTypes" :key="t.value" :value="t.value">{{ t.label }}</option>
          </select>
        </div>
        <!-- 問題分類 -->
        <div>
          <label class="label">問題分類 <span class="text-red-500">*</span></label>
          <select v-model="form.category_id" required class="input-base">
            <option :value="null" disabled>請選擇</option>
            <option v-for="c in meta.categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <!-- 系統/模組 -->
        <div>
          <label class="label">系統/模組</label>
          <select v-model="form.module_id" class="input-base">
            <option :value="null">不指定</option>
            <option v-for="m in filteredModules" :key="m.id" :value="m.id">{{ m.name }}</option>
          </select>
        </div>
        <!-- 優先度 -->
        <div>
          <label class="label">優先度</label>
          <div class="flex gap-2 mt-1">
            <button v-for="p in ['HIGH','MEDIUM','LOW']" :key="p" type="button"
              @click="form.priority = p"
              :class="form.priority === p ? priorityActive[p] : 'bg-gray-100 text-gray-600'"
              class="px-3 py-1 rounded-full text-sm font-medium transition-colors">
              {{ { HIGH: '高', MEDIUM: '中', LOW: '低' }[p] }}
            </button>
          </div>
        </div>
      </div>

      <!-- 問題描述 -->
      <div>
        <label class="label">問題描述 <span class="text-red-500">*</span></label>
        <textarea v-model="form.description" required rows="5" class="input-base" placeholder="請詳細描述問題..."></textarea>
      </div>

      <!-- 類似案件引用 -->
      <div>
        <button type="button" @click="showSimilarModal = true" class="text-sm text-indigo-600 hover:text-indigo-800 font-medium">
          📎 引用類似案件
        </button>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-3 pt-4 border-t">
        <router-link to="/cases" class="px-4 py-2 border border-gray-300 rounded-lg text-sm text-gray-700 hover:bg-gray-50">取消</router-link>
        <button type="submit" :disabled="submitting" class="px-6 py-2 bg-indigo-600 text-white rounded-lg text-sm hover:bg-indigo-700 disabled:opacity-50">
          {{ submitting ? '建立中...' : '建立案件' }}
        </button>
      </div>
    </form>

    <!-- Similar Case Modal -->
    <div v-if="showSimilarModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="showSimilarModal = false">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] overflow-hidden">
        <div class="p-4 border-b flex justify-between items-center">
          <h3 class="text-lg font-semibold">搜尋類似案件</h3>
          <button @click="showSimilarModal = false" class="text-gray-400 hover:text-gray-600">&times;</button>
        </div>
        <div class="p-4">
          <div class="flex gap-2 mb-4">
            <input v-model="similarQuery" class="input-base flex-1" placeholder="輸入關鍵字搜尋..." @keyup.enter="searchSimilar" />
            <button @click="searchSimilar" class="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm">搜尋</button>
          </div>
          <div class="max-h-60 overflow-y-auto divide-y">
            <div v-for="c in similarCases" :key="c.id" class="p-3 hover:bg-gray-50 cursor-pointer" @click="applySimilar(c)">
              <div class="flex justify-between">
                <span class="font-medium text-sm text-indigo-600">{{ c.case_number }}</span>
                <span class="text-xs text-gray-400">{{ c.customer?.name }}</span>
              </div>
              <p class="text-sm text-gray-600 mt-1 line-clamp-2">{{ c.description }}</p>
            </div>
            <div v-if="similarCases.length === 0" class="p-4 text-center text-gray-400 text-sm">無搜尋結果</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useMetaStore } from '../stores/meta'
import api from '../utils/api'

const router = useRouter()
const meta = useMetaStore()

// 固定的案件類型選項（作為下拉選單的預設值）
const caseTypes = [
  { value: 'REPAIR', label: '叫修' },
  { value: 'INQUIRY', label: '提問' },
  { value: 'EVALUATION', label: '評估需求' }
]

const form = ref({
  project_id: null,
  customer_id: null,
  reporter_name: '',
  reporter_phone: '',
  reporter_email: '',
  case_type: caseTypes[0].value, // default to 'REPAIR'
  category_id: null,
  module_id: null,
  priority: 'MEDIUM',
  description: ''
})

// 可選或自填的客戶名稱欄位（datalist）
const customerInput = ref('')

const submitting = ref(false)
const creatingCustomer = ref(false)
const showSimilarModal = ref(false)
const similarQuery = ref('')
const similarCases = ref([])

const filteredModules = computed(() => form.value.project_id ? meta.getModulesByProject(form.value.project_id) : [])

const priorityActive = {
  HIGH: 'bg-red-100 text-red-800',
  MEDIUM: 'bg-yellow-100 text-yellow-800',
  LOW: 'bg-green-100 text-green-800'
}

function onProjectChange() {
  form.value.module_id = null
  // Auto-fill customer if project has one
  const project = meta.projects.find(p => p.id === form.value.project_id)
  if (project?.customer_id) {
    form.value.customer_id = project.customer_id
    const cust = meta.customers.find(c => c.id === project.customer_id)
    customerInput.value = cust?.name || ''
  } else {
    form.value.customer_id = null
    customerInput.value = ''
  }
}

function onCustomerInputChange() {
  const name = (customerInput.value || '').trim()
  if (!name) {
    form.value.customer_id = null
    return
  }
  const matched = meta.customers.find(c => c.name === name)
  if (matched) form.value.customer_id = matched.id
  else form.value.customer_id = null
}

async function searchSimilar() {
  if (!similarQuery.value.trim()) return
  const { data: res } = await api.get('/cases', { params: { q: similarQuery.value, page_size: 10 } })
  if (res.success) similarCases.value = res.data
}

function applySimilar(c) {
  // 整案引用
  form.value.project_id = c.project?.id || form.value.project_id
  form.value.customer_id = c.customer?.id || form.value.customer_id
  customerInput.value = c.customer?.name || customerInput.value
  form.value.case_type = c.case_type || form.value.case_type
  form.value.priority = c.priority || form.value.priority
  form.value.description = c.description || ''
  showSimilarModal.value = false
}

async function submit() {
  submitting.value = true
  try {
    // 組裝 payload：若選到既有客戶傳送 customer_id，否則嘗試先建立客戶再傳送 customer_id
    const payload = { ...form.value }
    const inputName = (customerInput.value || '').trim()
    if (!payload.customer_id && inputName) {
      // 先嘗試建立客戶
      try {
        creatingCustomer.value = true
        const { data: cRes } = await api.post('/customers', { name: inputName })
        if (cRes && cRes.success && cRes.data?.id) {
          payload.customer_id = cRes.data.id
        } else if (cRes && cRes.data?.id) {
          payload.customer_id = cRes.data.id
        } else {
          // 若 API 未回傳 id，退回以名稱傳送（讓後端決定）
          payload.customer_name = inputName
          delete payload.customer_id
        }
      } catch (err) {
        // 若建立客戶失敗，顯示錯誤並中止提交
        alert(err.response?.data?.error?.message || '建立客戶失敗，請稍後再試')
        return
      } finally {
        creatingCustomer.value = false
      }
    }

    const { data: res } = await api.post('/cases', payload)
    if (res.success) {
      router.push(`/cases/${res.data.id}`)
    }
  } catch (err) {
    alert(err.response?.data?.error?.message || '建立失敗')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
@reference "tailwindcss";
.label { @apply block text-sm font-medium text-gray-700 mb-1 }
.input-base { @apply w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 }
</style>
