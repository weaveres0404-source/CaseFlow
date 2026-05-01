<template>
  <div class="mx-auto flex w-full max-w-[1400px] flex-col gap-4">
    <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div class="min-w-0">
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-1.5">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">通知中心</span>
        </div>
        <h1 class="text-2xl md:text-[28px] font-bold text-slate-900 tracking-tight">通知中心</h1>
        <p class="text-sm text-slate-500 mt-1">集中查看派工、完工、退回與估工相關通知。</p>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <div class="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-500 shadow-sm">
          <span class="w-2 h-2 rounded-full bg-rose-500"></span>
          未讀
          <span class="font-medium text-slate-800 tabular-nums">{{ unreadCount }}</span>
        </div>
        <button v-if="notifications.length > 0" @click="markAllRead" class="h-9 px-3.5 inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700 disabled:opacity-50" :disabled="unreadCount === 0">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5-2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
          全部標為已讀
        </button>
      </div>
    </div>

    <section class="grid gap-3 md:grid-cols-3">
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">目前頁面</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ page }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">未讀通知</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ unreadCount }}</div>
      </article>
      <article class="rounded-2xl border border-slate-200 bg-white px-4 py-3.5 shadow-sm">
        <div class="text-xs text-slate-500">已讀通知</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">{{ readCount }}</div>
      </article>
    </section>

    <section class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div class="px-5 py-4 border-b border-slate-100 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 class="text-sm font-semibold text-slate-900">通知篩選</h2>
          <p class="text-xs text-slate-500 mt-0.5">依已讀狀態切換列表。</p>
        </div>
        <div class="flex flex-wrap gap-2">
          <button v-for="f in filterOptions" :key="f.value" @click="filter = f.value"
            class="h-9 px-3 rounded-full text-sm font-medium border transition"
            :class="filter === f.value ? 'bg-brand-700 border-brand-700 text-white shadow-sm' : 'bg-white border-slate-300 text-slate-600 hover:bg-slate-50'">
            {{ f.label }}
          </button>
        </div>
      </div>

      <div class="divide-y divide-slate-100">
        <button v-for="n in notifications" :key="n.id" type="button"
          @click="goToCase(n)"
          class="w-full px-5 py-4 text-left flex items-start gap-4 hover:bg-slate-50 transition"
          :class="n.is_read ? 'bg-white' : 'bg-brand-50/50'">
          <div class="mt-1 shrink-0">
            <span class="flex w-10 h-10 rounded-xl items-center justify-center"
              :class="n.is_read ? 'bg-slate-100 text-slate-500' : 'bg-brand-100 text-brand-700'">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="notificationIcon(n.notification_type) === 'assigned'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                <path v-else-if="notificationIcon(n.notification_type) === 'completed'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                <path v-else-if="notificationIcon(n.notification_type) === 'returned'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10m0 0l-3-3m3 3l-3 3m8 5a8 8 0 10-8-8"/>
                <path v-else-if="notificationIcon(n.notification_type) === 'estimation'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m-6 4h3m-7 8h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
              </svg>
            </span>
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex flex-wrap items-center gap-2 mb-1.5">
              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="typeColor(n.notification_type)">{{ typeLabel(n.notification_type) }}</span>
              <span class="text-xs text-slate-400 tabular-nums">{{ formatTime(n.created_at) }}</span>
              <span v-if="!n.is_read" class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-rose-50 text-rose-700 ring-1 ring-rose-200">未讀</span>
            </div>
            <div class="text-sm font-medium text-slate-900 break-words [overflow-wrap:anywhere]">{{ notificationTitle(n) }}</div>
            <p class="text-sm mt-1 text-slate-600 leading-relaxed break-words [overflow-wrap:anywhere] whitespace-pre-wrap">{{ n.message }}</p>
            <div class="mt-2 flex flex-wrap items-center gap-3 text-xs text-slate-400">
              <span v-if="n.case_number" class="tabular-nums break-words [overflow-wrap:anywhere]">案件：{{ n.case_number }}</span>
              <span v-if="n.case_id" class="inline-flex items-center gap-1 text-brand-700">查看案件
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
              </span>
            </div>
          </div>
        </button>

        <div v-if="notifications.length === 0" class="px-5 py-16 text-center">
          <div class="mx-auto w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 grid place-items-center mb-3">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/></svg>
          </div>
          <h3 class="text-sm font-semibold text-slate-900">暫無通知</h3>
          <p class="text-sm text-slate-500 mt-1">目前篩選條件下沒有可顯示的通知。</p>
        </div>
      </div>

      <div v-if="totalPages > 1" class="px-5 py-4 border-t border-slate-100 flex items-center justify-between gap-3">
        <div class="text-xs text-slate-500 tabular-nums">第 {{ page }} / {{ totalPages }} 頁</div>
        <div class="inline-flex items-center gap-2">
          <button @click="page--" :disabled="page <= 1" class="h-8 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-600 hover:bg-slate-50 disabled:opacity-40">上一頁</button>
          <button @click="page++" :disabled="page >= totalPages" class="h-8 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-600 hover:bg-slate-50 disabled:opacity-40">下一頁</button>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../utils/api'

defineOptions({
  name: 'NotificationsCenterView'
})

const router = useRouter()
const notifications = ref([])
const page = ref(1)
const totalPages = ref(1)
const filter = ref('')

const unreadCount = computed(() => notifications.value.filter((item) => !item.is_read).length)
const readCount = computed(() => notifications.value.filter((item) => item.is_read).length)

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

async function goToCase(n) {
  if (!n.is_read) {
    await api.patch('/notifications/read', { notification_ids: [n.id] })
    n.is_read = true
  }
  if (n.case_id) router.push(`/cases/${n.case_id}`)
}

function notificationTitle(notification) {
  return notification.title || `${typeLabel(notification.notification_type)}通知`
}

function notificationIcon(type) {
  return {
    CASE_CREATED: 'created',
    ASSIGNED: 'assigned',
    COMPLETED: 'completed',
    ESTIMATION_DONE: 'estimation',
    CASE_CANCELLED: 'created',
    CASE_RETURNED: 'returned'
  }[type] || 'default'
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
