<template>
  <div class="grid grid-cols-1 lg:grid-cols-2 min-h-screen w-full">
    
    <aside class="relative hidden lg:flex flex-col justify-between overflow-hidden bg-slate-900 text-slate-100 p-10 xl:p-14">
    <div class="absolute inset-0 opacity-60"
      style="background-image:linear-gradient(rgba(255,255,255,.06) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.06) 1px,transparent 1px);background-size:44px 44px"></div>
    
    <div class="absolute -top-24 -left-24 w-[520px] h-[520px] pointer-events-none"
      style="background:radial-gradient(60% 60% at 30% 30%,rgba(129,140,248,.35) 0%,rgba(67,56,202,0) 70%)"></div>
    <div class="absolute bottom-0 right-0 w-[480px] h-[480px] pointer-events-none opacity-70"
      style="background:radial-gradient(60% 60% at 70% 70%,rgba(129,140,248,.35) 0%,rgba(67,56,202,0) 70%)"></div>

    <div class="relative flex items-center gap-3">
      <div class="w-10 h-10 rounded-xl bg-white/10 backdrop-blur grid place-items-center ring-1 ring-white/10">
        <svg viewBox="0 0 24 24" class="w-5 h-5 text-indigo-200" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M4 6h16M4 12h10M4 18h7"/><circle cx="18" cy="18" r="3"/>
        </svg>
      </div>
      <div class="leading-tight">
        <div class="text-sm font-semibold tracking-wide">CaseFlow</div>
        <div class="text-[11px] text-slate-400">任務追蹤系統 · v3.1</div>
      </div>
    </div>

    <div class="relative">
      <div class="max-w-md">
        <div class="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-white/5 ring-1 ring-white/10 text-xs text-slate-300 mb-5">
          <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
          服務運作正常 · 系統版本 3.1.4
        </div>
        <h1 class="text-3xl xl:text-4xl font-bold leading-tight tracking-tight">
          把每一個叫修案件<br/>
          <span class="text-indigo-200">管得明明白白。</span>
        </h1>
        <p class="mt-4 text-slate-400 leading-relaxed">
          統一接收客戶回報、指派工程師、追蹤工時與結案——從立案到結案，一條路徑走完。
        </p>
      </div>

      <div class="relative mt-10 max-w-md">
        <div class="rounded-xl bg-white/5 ring-1 ring-white/10 backdrop-blur-sm p-4 ">
          <div class="flex items-center justify-between mb-3">
            <div class="text-xs text-slate-400">今日待辦</div>
            <div class="text-[10px] text-slate-500">更新於 14:32</div>
          </div>
          <div class="grid grid-cols-4 gap-3">
            <div><div class="font-num text-xl font-semibold">12</div><div class="text-[11px] text-slate-400">待處理</div></div>
            <div><div class="font-num text-xl font-semibold text-sky-300">5</div><div class="text-[11px] text-slate-400">已派工</div></div>
            <div><div class="font-num text-xl font-semibold text-amber-300">8</div><div class="text-[11px] text-slate-400">處理中</div></div>
            <div><div class="font-num text-xl font-semibold text-emerald-300">3</div><div class="text-[11px] text-slate-400">待結案</div></div>
          </div>
        </div>

        <div class="absolute -bottom-5 -right-4 hidden xl:block rounded-xl bg-white text-slate-800 shadow-2xl p-3 w-64 rotate-1 ring-1 ring-black/5">
          <div class="flex items-center justify-between">
            <div class="tabular-nums text-xs text-indigo-600 font-semibold">C2026-0086</div>
            <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-800 ring-1 ring-amber-200">處理中</span>
          </div>
          <div class="mt-1 text-sm font-medium">CRM 匯入模組重複送單</div>
          <div class="mt-1 text-[11px] text-slate-500">遠傳電信 · 李大陳</div>
        </div>
      </div>
    </div>

    <div class="relative text-[11px] text-slate-500 flex items-center gap-4">
      <span>© 2026 CaseFlow</span>
      <span>·</span>
      <a href="#" class="hover:text-slate-300">隱私權政策</a>
      <a href="#" class="hover:text-slate-300">服務條款</a>
    </div>
  </aside>


  <main class="flex items-center justify-center bg-white p-6 lg:p-12 min-h-screen">
    <div class="w-full max-w-md grid grid-cols-1 gap-6">
      <!-- Logo / Title -->
      <div class="text-center mb-8">
        <div class="inline-grid place-items-center w-14 h-14 rounded-2xl bg-slate-800 via-amber-500 to-indigo-600 shadow-lg mb-4">
          <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-slate-800">CaseFlow</h1>
        <p class="text-slate-500 text-sm mt-1">任務追蹤系統 — 請登入您的帳戶</p>
      </div>

      <!-- Card -->
      <div class="rounded-3xl border border-orange-100 bg-white/90 p-10 shadow-[0_24px_60px_rgba(15,23,42,0.12)] backdrop-blur">
        <form @submit.prevent="submit" class="space-y-5 grid gap-2">

          <!-- Username -->
          <div class="grid gap-2">
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">帳號</label>
            <input
              v-model="username"
              type="text"
              placeholder="請輸入帳號"
              autocomplete="username"
              required
              class="w-full rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-slate-50 ring-1 ring-slate-200 focus:ring-2 focus:ring-indigo-500 outline-none transition-all placeholder:text-slate-400"
            />
          </div>

          <!-- Password -->
          <div class="grid gap-2">
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
          <div class="grid grid-cols-[auto_1fr] items-center gap-3 pt-1">
            <label class="inline-grid grid-flow-col auto-cols-max items-center gap-2 cursor-pointer select-none">
              <input
                v-model="rememberMe"
                type="checkbox"
                class="w-4 h-4 rounded accent-indigo-600"
              />
              <span class="text-sm text-slate-600">記住我</span>
            </label>
            <a href="#" class="justify-self-end text-sm text-indigo-600 hover:text-indigo-800 hover:underline transition-colors">
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
            class="w-full py-3 rounded-xl text-white text-sm font-semibold bg-slate-800 hover:shadow-lg hover:shadow-orange-200 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
          >
            <span v-if="loading" class="inline-grid grid-flow-col auto-cols-max items-center justify-center gap-2">
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
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const result = await auth.login(username.value, password.value)
    if (result?.mustChangePassword) {
      sessionStorage.setItem('setup_token', result.setupToken)
      router.push('/setup-password')
      return
    }
    const redirect = route.query.redirect || '/dashboard'
    router.push(redirect)
  } catch (e) {
    const msg = e?.response?.data?.error?.message || e?.message || ''
    if (msg.toLowerCase().includes('invalid') || msg.toLowerCase().includes('unauthorized')) {
      error.value = '帳號或密碼錯誤，請重新輸入'
    } else if (e?.response?.status === 400) {
      error.value = '請輸入有效的帳號與密碼'
    } else {
      error.value = `登入失敗，請稍後再試`
    }
  } finally {
    loading.value = false
  }
}
</script>
