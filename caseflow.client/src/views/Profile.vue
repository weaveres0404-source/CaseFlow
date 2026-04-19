<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-bold text-gray-900">個人資料</h2>
    <div class="bg-white rounded-xl shadow-sm overflow-hidden p-6 lg:p-8">
      <div class="flex items-center gap-4 mb-6">
        <div class="w-16 h-16 bg-indigo-100 rounded-full flex items-center justify-center text-2xl font-bold text-indigo-600">
          {{ user?.full_name?.charAt(0) || '?' }}
        </div>
        <div>
          <p class="text-lg font-semibold text-gray-900">{{ user?.full_name }}</p>
          <span class="badge" :class="roleColor">{{ roleLabel }}</span>
        </div>
      </div>
      <dl class="divide-y divide-gray-100">
        <div class="py-3 flex justify-between"><dt class="text-sm text-gray-500">帳號</dt><dd class="text-sm font-medium">{{ user?.username }}</dd></div>
        <div class="py-3 flex justify-between"><dt class="text-sm text-gray-500">Email</dt><dd class="text-sm font-medium">{{ user?.email }}</dd></div>
        <div class="py-3 flex justify-between"><dt class="text-sm text-gray-500">角色</dt><dd class="text-sm font-medium">{{ roleLabel }}</dd></div>
        <div class="py-3 flex justify-between"><dt class="text-sm text-gray-500">上次登入</dt><dd class="text-sm font-medium">{{ formatTime(user?.last_login_at) }}</dd></div>
      </dl>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const user = computed(() => auth.user)

const roleLabel = computed(() => ({ SysAdmin: '系統管理員', PM: '專案經理', SE: '工程師' }[user.value?.role] || user.value?.role))
const roleColor = computed(() => ({ SysAdmin: 'bg-red-100 text-red-800', PM: 'bg-blue-100 text-blue-800', SE: 'bg-green-100 text-green-800' }[user.value?.role] || 'bg-gray-100 text-gray-800'))

function formatTime(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  return `${d.getFullYear()}/${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}
</script>

<style scoped>
@reference "tailwindcss";
.badge { @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium }
</style>
