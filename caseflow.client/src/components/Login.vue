<template>
  <div class="min-h-screen w-full grid place-items-center bg-gradient-to-br from-slate-100 to-indigo-50 px-4">
    <div class="w-full max-w-md">
      <!-- Logo / Title -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-indigo-600 to-blue-600 shadow-lg mb-4">
          <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-slate-800">CaseFlow</h1>
        <p class="text-slate-500 text-sm mt-1">叫修管理系統 — 請登入您的帳戶</p>
      </div>

      <!-- Card -->
      <div class="bg-white shadow-2xl rounded-3xl p-10 border border-slate-100">
        <form @submit.prevent="submit" class="space-y-5">

          <!-- Email -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">電子郵件</label>
            <input
              v-model="email"
              type="email"
              placeholder="you@example.com"
              autocomplete="email"
              required
              class="w-full rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-slate-50 ring-1 ring-slate-200 focus:ring-2 focus:ring-indigo-500 outline-none transition-all placeholder:text-slate-400"
            />
          </div>

          <!-- Password -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">密碼</label>
            <input
              v-model="password"
              type="password"
              placeholder="••••••••"
              autocomplete="current-password"
              required
              class="w-full rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-slate-50 ring-1 ring-slate-200 focus:ring-2 focus:ring-indigo-500 outline-none transition-all placeholder:text-slate-400"
            />
          </div>

          <!-- Remember me + Forgot -->
          <div class="flex items-center justify-between pt-1">
            <label class="flex items-center gap-2 cursor-pointer select-none">
              <input
                v-model="rememberMe"
                type="checkbox"
                class="w-4 h-4 rounded accent-indigo-600"
              />
              <span class="text-sm text-slate-600">記住我</span>
            </label>
            <a href="#" class="text-sm text-indigo-600 hover:text-indigo-800 hover:underline transition-colors">
              忘記密碼？
            </a>
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
              登入中...
            </span>
            <span v-else>登入</span>
          </button>

        </form>
      </div>

      <p class="text-center text-xs text-slate-400 mt-6">
        如需帳號請聯絡系統管理員
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const rememberMe = ref(false)
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    const redirect = route.query.redirect || '/dashboard'
    router.push(redirect)
  } catch (e) {
    const msg = e?.response?.data?.error?.message || e?.message || ''
    if (msg.toLowerCase().includes('invalid') || msg.toLowerCase().includes('unauthorized')) {
      error.value = '帳號或密碼錯誤，請重新輸入'
    } else if (e?.response?.status === 400) {
      error.value = '請輸入有效的電子郵件與密碼'
    } else {
      error.value = `登入失敗，請稍後再試`
    }
  } finally {
    loading.value = false
  }
}
</script>
