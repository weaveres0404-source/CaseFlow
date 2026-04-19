<template>
  <div class="space-y-5">
    <div class="flex items-center justify-between">
      <h2 class="text-2xl font-bold text-gray-900">通知中心</h2>
      <button v-if="notifications.length > 0" @click="markAllRead" class="text-sm text-indigo-600 font-medium hover:underline">全部標為已讀</button>
    </div>

    <!-- Filters -->
    <div class="flex gap-2">
      <button v-for="f in filterOptions" :key="f.value" @click="filter = f.value"
        class="px-3 py-1.5 text-sm rounded-full border transition-colors"
        :class="filter === f.value ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'">
        {{ f.label }}
      </button>
    </div>

    <!-- List -->
    <div class="space-y-2">
      <div v-for="n in notifications" :key="n.id"
        @click="goToCase(n)"
        class="bg-white rounded-xl shadow-sm p-4 flex items-start gap-4 cursor-pointer hover:shadow-md transition-shadow"
        :class="n.is_read ? 'border border-gray-100' : 'border border-indigo-200 bg-indigo-50/30'">
        <div class="shrink-0 w-2 h-2 rounded-full mt-2" :class="n.is_read ? 'bg-gray-300' : 'bg-indigo-600'"></div>
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2">
            <span class="badge" :class="typeColor(n.notification_type)">{{ typeLabel(n.notification_type) }}</span>
            <span class="text-xs text-gray-400">{{ formatTime(n.created_at) }}</span>
          </div>
          <p class="text-sm mt-1 text-gray-800 line-clamp-2">{{ n.message }}</p>
          <p v-if="n.case_number" class="text-xs text-gray-400 mt-1">案件：{{ n.case_number }}</p>
        </div>
      </div>
      <div v-if="notifications.length === 0" class="text-center py-16 text-gray-400">暫無通知</div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex justify-center items-center gap-2 pt-4">
      <button @click="page--" :disabled="page <= 1" class="px-3 py-1 border rounded-lg text-sm disabled:opacity-40">上一頁</button>
      <span class="text-sm text-gray-500">{{ page }} / {{ totalPages }}</span>
      <button @click="page++" :disabled="page >= totalPages" class="px-3 py-1 border rounded-lg text-sm disabled:opacity-40">下一頁</button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../utils/api'

const router = useRouter()
const notifications = ref([])
const page = ref(1)
const totalPages = ref(1)
const filter = ref('')

const filterOptions = [
  { label: '全部', value: '' },
  { label: '未讀', value: 'false' },
  { label: '已讀', value: 'true' }
]

async function fetchNotifications() {
  const params = { page: page.value, page_size: 20 }
  if (filter.value !== '') params.is_read = filter.value
  const { data: res } = await api.get('/notifications', { params })
  if (res.success) {
    notifications.value = res.data
    totalPages.value = res.meta?.total_pages || 1
  }
}

async function markAllRead() {
  await api.patch('/notifications/read', { all: true })
  await fetchNotifications()
}

function goToCase(n) {
  if (!n.is_read) api.patch('/notifications/read', { notification_ids: [n.id] })
  if (n.case_id) router.push(`/cases/${n.case_id}`)
}

function typeLabel(t) {
  return { CASE_CREATED: '新案', ASSIGNED: '派工', COMPLETED: '完工', ESTIMATION_DONE: '評估', CASE_CANCELLED: '取消', CASE_RETURNED: '退回' }[t] || t
}
function typeColor(t) {
  return {
    CASE_CREATED: 'bg-blue-100 text-blue-800', ASSIGNED: 'bg-indigo-100 text-indigo-800',
    COMPLETED: 'bg-green-100 text-green-800', ESTIMATION_DONE: 'bg-yellow-100 text-yellow-800',
    CASE_CANCELLED: 'bg-red-100 text-red-800', CASE_RETURNED: 'bg-orange-100 text-orange-800'
  }[t] || 'bg-gray-100 text-gray-800'
}
function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

watch([page, filter], () => { if (filter.value !== undefined) fetchNotifications() })
onMounted(fetchNotifications)
</script>

<style scoped>
@reference "tailwindcss";
.badge { @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium }
</style>
