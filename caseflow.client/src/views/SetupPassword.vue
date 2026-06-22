<template>
  <div class="min-h-screen w-full grid place-items-center bg-gradient-to-br from-slate-100 to-indigo-50 px-4">
    <div class="w-full max-w-md">
      <!-- Logo / Title -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-indigo-600 to-blue-600 shadow-lg mb-4">
          <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-slate-800">設定新密碼</h1>
        <p class="text-slate-500 text-sm mt-1">首次登入請設定您的密碼</p>
      </div>

      <!-- Card -->
      <div class="bg-white shadow-2xl rounded-3xl p-10 border border-slate-100">
        <form @submit.prevent="submit" class="space-y-5">

          <!-- New Password -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">新密碼</label>
            <input
              v-model="newPassword"
              type="password"
              placeholder="請輸入新密碼（至少 8 個字元）"
              autocomplete="new-password"
              required
              minlength="8"
              class="w-full rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-slate-50 ring-1 ring-slate-200 focus:ring-2 focus:ring-indigo-500 outline-none transition-all placeholder:text-slate-400"
            />
          </div>

          <!-- Confirm Password -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">確認密碼</label>
            <input
              v-model="confirmPassword"
              type="password"
              placeholder="再次輸入新密碼"
              autocomplete="new-password"
              required
              class="w-full rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-slate-50 ring-1 ring-slate-200 focus:ring-2 focus:ring-indigo-500 outline-none transition-all placeholder:text-slate-400"
            />
          </div>

          <!-- Error -->
          <p v-if="error" class="text-red-500 text-sm bg-red-50 rounded-xl px-4 py-2.5 border border-red-100">
            {{ error }}
          </p>

          <!-- Submit -->
          <button
            type="submit"
            :disabled="loading"
            class="w-full py-3 rounded-xl text-white text-sm font-semibold bg-gradient-to-r from-indigo-600 to-blue-600 hover:shadow-lg hover:shadow-indigo-200 active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
          >
            <span v-if="loading" class="flex items-center justify-center gap-2">
              <svg class="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              設定中...
            </span>
            <span v-else>確認設定</span>
          </button>

        </form>
      </div>

      <p class="text-center text-xs text-slate-400 mt-6">
        設定完成後將自動登入系統
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const newPassword = ref('')
const confirmPassword = ref('')
const error = ref('')
const loading = ref(false)

onMounted(() => {
  const token = sessionStorage.getItem('setup_token')
  if (!token) {
    router.replace('/login')
  }
})

async function submit() {
  error.value = ''
  if (newPassword.value.length < 8) {
    error.value = '密碼長度至少需要 8 個字元'
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = '兩次輸入的密碼不一致'
    return
  }

  const setupToken = sessionStorage.getItem('setup_token')
  if (!setupToken) {
    router.replace('/login')
    return
  }

  loading.value = true
  try {
    await auth.setupPassword(setupToken, newPassword.value)
    sessionStorage.removeItem('setup_token')
    router.push('/dashboard')
  } catch (e) {
    const msg = e?.response?.data?.error?.message || e?.message || ''
    if (msg.toLowerCase().includes('expired') || msg.toLowerCase().includes('invalid')) {
      error.value = '設定連結已過期，請重新登入'
      sessionStorage.removeItem('setup_token')
      setTimeout(() => router.replace('/login'), 2000)
    } else {
      error.value = msg || '設定失敗，請稍後再試'
    }
  } finally {
    loading.value = false
  }
}
</script>
