<template>
  <div class="mx-auto flex w-full max-w-[1400px] flex-col gap-4">
    <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div>
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-1.5">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">問題分類管理</span>
        </div>
        <h1 class="text-2xl md:text-[28px] font-bold text-slate-900 tracking-tight">問題分類管理</h1>
        <p class="text-sm text-slate-500 mt-1">維護案件立案與篩選所使用的問題分類。</p>
      </div>
      <button @click="openCreate" class="h-9 px-3.5 inline-flex items-center gap-1.5 rounded-lg bg-brand-700 hover:bg-brand-800 text-white text-sm font-medium shadow-sm">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        新增分類
      </button>
    </div>

    <section class="grid gap-3 md:grid-cols-[1.3fr_.7fr_.7fr]">
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">搜尋結果</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ total }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">目前頁數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ page }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">每頁筆數</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ pageSize }}</div>
      </article>
    </section>

    <section class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div class="px-5 py-4 border-b border-slate-100 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div class="flex-1 flex flex-col gap-3 sm:flex-row sm:items-center">
          <div class="relative flex-1 max-w-xl">
            <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z"/></svg>
            <input v-model.trim="query" type="text" placeholder="搜尋分類名稱或描述"
              class="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500"
              @keyup.enter="searchCategories" />
          </div>
          <button @click="searchCategories" class="h-10 px-4 rounded-lg bg-brand-700 hover:bg-brand-800 text-white text-sm font-medium">搜尋</button>
        </div>
        <div class="flex items-center gap-2 text-sm text-slate-500">
          <span>每頁</span>
          <select v-model="pageSize" class="h-9 px-2 rounded-lg border border-slate-300 bg-white text-sm text-slate-700" @change="handlePageSizeChange">
            <option :value="10">10</option>
            <option :value="20">20</option>
            <option :value="50">50</option>
          </select>
        </div>
      </div>

      <div v-if="requestError" class="mx-5 mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ requestError }}</div>

      <div class="hidden md:block overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead class="bg-slate-50 text-slate-500 text-left">
            <tr class="[&_th]:px-5 [&_th]:py-3 [&_th]:font-medium [&_th]:whitespace-nowrap">
              <th>名稱</th>
              <th>描述</th>
              <th>排序</th>
              <th>狀態</th>
              <th>更新時間</th>
              <th class="text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="cat in categories" :key="cat.id" class="hover:bg-slate-50">
              <td class="px-5 py-4">
                <div class="font-medium text-slate-900 break-words [overflow-wrap:anywhere]">{{ cat.name }}</div>
                <div class="text-xs text-slate-400 tabular-nums mt-1">ID {{ cat.id }}</div>
              </td>
              <td class="px-5 py-4 text-slate-600 max-w-md break-words [overflow-wrap:anywhere] whitespace-pre-wrap">{{ cat.description || '—' }}</td>
              <td class="px-5 py-4 tabular-nums text-slate-700">{{ cat.sort_order }}</td>
              <td class="px-5 py-4">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="cat.is_active ? 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200' : 'bg-slate-100 text-slate-500 ring-1 ring-slate-200'">
                  {{ cat.is_active ? '啟用' : '停用' }}
                </span>
              </td>
              <td class="px-5 py-4 text-slate-500 tabular-nums">{{ formatTime(cat.updated_at) }}</td>
              <td class="px-5 py-4">
                <div class="flex items-center justify-end gap-2">
                  <button @click="openEdit(cat)" class="h-8 px-3 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700">編輯</button>
                  <button @click="confirmDelete(cat)" class="h-8 px-3 rounded-lg border border-rose-200 bg-rose-50 hover:bg-rose-100 text-sm text-rose-700">刪除</button>
                </div>
              </td>
            </tr>
            <tr v-if="!loading && categories.length === 0">
              <td colspan="6" class="px-5 py-16 text-center text-slate-400">沒有符合條件的分類</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="md:hidden divide-y divide-slate-100">
        <div v-for="cat in categories" :key="cat.id" class="px-4 py-4">
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="font-medium text-slate-900 break-words [overflow-wrap:anywhere]">{{ cat.name }}</div>
              <div class="text-xs text-slate-500 mt-1 break-words [overflow-wrap:anywhere] whitespace-pre-wrap">{{ cat.description || '未填寫描述' }}</div>
            </div>
            <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium whitespace-nowrap" :class="cat.is_active ? 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200' : 'bg-slate-100 text-slate-500 ring-1 ring-slate-200'">{{ cat.is_active ? '啟用' : '停用' }}</span>
          </div>
          <div class="mt-3 flex items-center justify-between text-xs text-slate-400">
            <span class="tabular-nums">排序 {{ cat.sort_order }}</span>
            <span class="tabular-nums">{{ formatTime(cat.updated_at) }}</span>
          </div>
          <div class="mt-3 flex gap-2">
            <button @click="openEdit(cat)" class="flex-1 h-9 rounded-lg border border-slate-300 bg-white text-sm text-slate-700">編輯</button>
            <button @click="confirmDelete(cat)" class="flex-1 h-9 rounded-lg border border-rose-200 bg-rose-50 text-sm text-rose-700">刪除</button>
          </div>
        </div>
      </div>

      <div v-if="loading" class="px-5 py-10 text-center text-sm text-slate-400">載入中…</div>

      <div v-if="totalPages > 1" class="px-5 py-4 border-t border-slate-100 flex items-center justify-between gap-3">
        <div class="text-xs text-slate-500 tabular-nums">第 {{ page }} / {{ totalPages }} 頁</div>
        <div class="inline-flex items-center gap-2">
          <button @click="page = Math.max(1, page - 1)" :disabled="page <= 1" class="h-8 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-600 hover:bg-slate-50 disabled:opacity-40">上一頁</button>
          <button @click="page = Math.min(totalPages, page + 1)" :disabled="page >= totalPages" class="h-8 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-600 hover:bg-slate-50 disabled:opacity-40">下一頁</button>
        </div>
      </div>
    </section>

    <div v-if="showModal" class="fixed inset-0 z-50 bg-slate-950/45 flex items-center justify-center p-4" @click.self="closeModal">
      <div class="w-full max-w-xl rounded-2xl bg-white border border-slate-200 shadow-xl overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-100 flex items-center justify-between gap-3">
          <div>
            <h2 class="text-lg font-semibold text-slate-900">{{ isEdit ? '編輯分類' : '新增分類' }}</h2>
            <p class="text-xs text-slate-500 mt-0.5">分類名稱會用於案件立案、查詢與統計。</p>
          </div>
          <button @click="closeModal" class="p-2 rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-700">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <div class="px-5 py-5 space-y-4">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1.5">名稱</label>
            <input v-model.trim="form.categoryName" type="text" class="w-full h-10 px-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500" placeholder="例如：帳號權限 / 系統 Bug" />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1.5">描述</label>
            <textarea v-model.trim="form.description" rows="4" class="w-full px-3 py-2.5 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500" placeholder="補充這個分類的使用情境"></textarea>
          </div>
          <div class="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-end">
            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1.5">排序</label>
              <input v-model.number="form.sortOrder" type="number" min="0" class="w-full h-10 px-3 rounded-lg border border-slate-300 text-sm tabular-nums focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500" />
            </div>
            <label class="inline-flex items-center gap-2 h-10 text-sm text-slate-600">
              <input v-model="form.isActive" type="checkbox" class="w-4 h-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500" />
              啟用此分類
            </label>
          </div>

          <div v-if="formError" class="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ formError }}</div>
        </div>

        <div class="px-5 py-4 border-t border-slate-100 flex items-center justify-end gap-2">
          <button @click="closeModal" class="h-9 px-4 rounded-lg border border-slate-300 bg-white text-sm text-slate-700 hover:bg-slate-50">取消</button>
          <button @click="submitForm" :disabled="saving" class="h-9 px-4 rounded-lg bg-brand-700 hover:bg-brand-800 disabled:bg-slate-300 text-sm font-medium text-white">
            {{ saving ? '儲存中…' : isEdit ? '更新分類' : '建立分類' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '../utils/api'

defineOptions({
  name: 'ProblemCategoriesAdminView'
})

const categories = ref([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const query = ref('')
const loading = ref(false)
const saving = ref(false)
const requestError = ref('')
const formError = ref('')
const showModal = ref(false)
const isEdit = ref(false)
const form = ref(createEmptyForm())

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))

function createEmptyForm() {
  return {
    id: null,
    categoryName: '',
    description: '',
    sortOrder: 10,
    isActive: true
  }
}

async function load() {
  loading.value = true
  requestError.value = ''
  try {
    const { data: res } = await api.get('/problem-categories', {
      params: {
        page: page.value,
        page_size: pageSize.value,
        q: query.value || undefined
      }
    })
    if (res.success) {
      categories.value = res.data || []
      total.value = res.meta?.total || 0
    }
  } catch (error) {
    requestError.value = error?.response?.data?.error?.message || '讀取問題分類失敗，請稍後再試'
  } finally {
    loading.value = false
  }
}

function searchCategories() {
  page.value = 1
  load()
}

function handlePageSizeChange() {
  page.value = 1
  load()
}

function openCreate() {
  isEdit.value = false
  form.value = createEmptyForm()
  formError.value = ''
  showModal.value = true
}

function openEdit(category) {
  isEdit.value = true
  form.value = {
    id: category.id,
    categoryName: category.name,
    description: category.description || '',
    sortOrder: category.sort_order ?? 10,
    isActive: Boolean(category.is_active)
  }
  formError.value = ''
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  saving.value = false
}

async function submitForm() {
  formError.value = ''
  if (!form.value.categoryName) {
    formError.value = '名稱為必填'
    return
  }

  saving.value = true
  try {
    const payload = {
      CategoryName: form.value.categoryName,
      Description: form.value.description || null,
      SortOrder: Number(form.value.sortOrder) || 0,
      IsActive: Boolean(form.value.isActive)
    }

    if (isEdit.value && form.value.id) {
      await api.put(`/problem-categories/${form.value.id}`, payload)
    } else {
      await api.post('/problem-categories', payload)
    }

    closeModal()
    await load()
  } catch (error) {
    formError.value = error?.response?.data?.error?.message || '儲存失敗，請稍後再試'
  } finally {
    saving.value = false
  }
}

async function confirmDelete(category) {
  if (!window.confirm(`確定刪除「${category.name}」？`)) return
  requestError.value = ''
  try {
    await api.delete(`/problem-categories/${category.id}`)
    if (categories.value.length === 1 && page.value > 1) {
      page.value -= 1
    }
    await load()
  } catch (error) {
    requestError.value = error?.response?.data?.error?.message || '刪除失敗，請稍後再試'
  }
}

function formatTime(iso) {
  if (!iso) return '—'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('zh-TW', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  })
}

watch(page, () => {
  load()
})

onMounted(load)
</script>
