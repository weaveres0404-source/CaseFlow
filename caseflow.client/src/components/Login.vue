<template>
  <div class="min-h-screen grid lg:grid-cols-[1.1fr_1fr]">

    <!-- ===== Left brand panel (lg+) ===== -->
    <aside class="relative hidden lg:flex flex-col justify-between overflow-hidden bg-slate-900 text-slate-100 p-10 xl:p-14">
      <!-- Grid texture -->
      <div class="absolute inset-0 opacity-60"
        style="background-image:linear-gradient(rgba(255,255,255,.06) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.06) 1px,transparent 1px);background-size:44px 44px"></div>
      <!-- Glow blobs -->
      <div class="absolute -top-24 -left-24 w-[520px] h-[520px] pointer-events-none"
        style="background:radial-gradient(60% 60% at 30% 30%,rgba(129,140,248,.35) 0%,rgba(67,56,202,0) 70%)"></div>
      <div class="absolute bottom-0 right-0 w-[480px] h-[480px] pointer-events-none opacity-70"
        style="background:radial-gradient(60% 60% at 70% 70%,rgba(129,140,248,.35) 0%,rgba(67,56,202,0) 70%)"></div>

      <!-- Brand lockup -->
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

      <!-- Middle copy -->
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

        <!-- Mini stats preview -->
        <div class="relative mt-10 max-w-md">
          <div class="rounded-xl bg-white/5 ring-1 ring-white/10 backdrop-blur-sm p-4">
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

          <div class="absolute -bottom-5 -right-4 hidden xl:block rounded-xl bg-white text-slate-800 shadow-2xl p-3 w-64 rotate-1">
            <div class="flex items-center justify-between">
              <div class="tabular-nums text-xs text-brand-700 font-semibold">C2026-0086</div>
              <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-800 ring-1 ring-amber-200">處理中</span>
            </div>
            <div class="mt-1 text-sm font-medium">CRM 匯入模組重複送單</div>
            <div class="mt-1 text-[11px] text-slate-500">遠傳電信 · 李大陳</div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="relative text-[11px] text-slate-500 flex items-center gap-4">
        <span>© 2026 CaseFlow</span>
        <span>·</span>
        <a href="#" class="hover:text-slate-300">隱私權政策</a>
        <a href="#" class="hover:text-slate-300">服務條款</a>
      </div>
    </aside>

    <!-- ===== Right form panel ===== -->
    <main class="flex items-center justify-center p-6 sm:p-10 bg-white">
      <div class="w-full max-w-sm grid gap-3">

        <!-- Mobile brand -->
        <div class="lg:hidden flex items-center gap-2 mb-8">
          <div class="w-9 h-9 rounded-lg bg-brand-700 text-white grid place-items-center">
            <svg viewBox="0 0 24 24" class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 6h16M4 12h10M4 18h7"/><circle cx="18" cy="18" r="3"/>
            </svg>
          </div>
          <div>
            <div class="text-sm font-semibold text-slate-900">CaseFlow</div>
            <div class="text-[11px] text-slate-500">任務追蹤系統</div>
          </div>
        </div>

        <h2 class="text-2xl font-bold text-slate-900 tracking-tight">登入帳號</h2>
        <p class="text-sm text-slate-500 mt-1">請使用組織配發的帳號登入。若為首次登入，系統會引導您設定新密碼。</p>

        <!-- Error banner -->
        <div v-if="error"
          class="mt-6 flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-rose-800 login-shake">
          <svg class="w-4 h-4 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M12 3a9 9 0 100 18A9 9 0 0012 3z"/>
          </svg>
          <div class="text-sm">
            <div class="font-medium">帳號或密碼錯誤</div>
            <div class="text-xs text-rose-700/80 mt-0.5">連續錯誤 5 次將暫時鎖定帳號 15 分鐘。</div>
          </div>
        </div>

        <form @submit.prevent="submit" class="mt-6 space-y-4 grid gap-y-2">
          <!-- 帳號 -->
          <div class="grid gap-2">
            <label class="block text-xs font-medium text-slate-600 mb-1.5">帳號</label>
            <div class="input-wrap flex items-center h-11 px-3 rounded-lg border border-slate-300 bg-white transition"
              :class="{ 'border-brand-500 ring-4 ring-brand-700/10': usernameFocused }">
              <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
              </svg>
              <input v-model="username" type="text" autocomplete="username"
                class="flex-1 outline-none text-sm bg-transparent placeholder:text-slate-400"
                placeholder="請輸入帳號"
                @focus="usernameFocused = true" @blur="usernameFocused = false" />
            </div>
          </div>

          <!-- Password -->
          <div class="grid gap-2">
            <div class="flex items-center justify-between mb-1.5">
              <label class="block text-xs font-medium text-slate-600">密碼</label>
              <button type="button" class="text-xs text-slate-500 hover:text-brand-700" @click="handleForgotPassword">忘記密碼？</button>
            </div>
            <div class="input-wrap flex items-center h-11 px-3 rounded-lg border border-slate-300 bg-white transition"
              :class="{ 'border-brand-500 ring-4 ring-brand-700/10': pwFocused }">
              <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
              <input v-model="password" :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                class="flex-1 outline-none text-sm bg-transparent placeholder:text-slate-400"
                placeholder="請輸入密碼"
                @focus="pwFocused = true" @blur="pwFocused = false" />
              <button type="button" class="p-1 -mr-1 text-slate-400 hover:text-slate-700" @click="showPassword = !showPassword" aria-label="顯示密碼">
                <svg v-if="!showPassword" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                </svg>
                <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                </svg>
              </button>
            </div>
          </div>

          <!-- Remember me -->
          <div class="flex items-center justify-between pt-1">
            <label class="inline-flex items-center gap-2 text-sm text-slate-600 select-none cursor-pointer">
              <input v-model="rememberMe" type="checkbox" class="w-4 h-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500" />
              記住此裝置 30 天
            </label>
            <div class="text-[11px] text-slate-400 tabular-nums">v3.1.4</div>
          </div>

          <!-- Submit -->
          <button type="submit" :disabled="loading"
            class="w-full h-11 rounded-lg bg-brand-700 hover:bg-brand-800 active:bg-brand-900 text-sm font-medium shadow-sm transition disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2">
            <svg v-if="loading" class="animate-spin w-4 h-4 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="9" opacity=".3"/><path d="M21 12a9 9 0 0 1-9 9"/>
            </svg>
            {{ loading ? '驗證中…' : '登入' }}
          </button>

          <!-- Divider -->
          <div class="relative pt-1">
            <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-200"></div></div>
            <div class="relative flex justify-center"><span class="bg-white px-2 text-[11px] text-slate-400">或</span></div>
          </div>

          <button type="button" disabled class="w-full h-11 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700 inline-flex items-center justify-center gap-2 disabled:opacity-50">
            <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
            </svg>
            使用組織 SSO 登入
            <span class="text-[10px] text-slate-400 ml-1">（PH2）</span>
          </button>
        </form>

        <div class="mt-10 text-[11px] text-slate-400 leading-relaxed">
          本系統僅供授權員工使用。所有操作均會記錄於稽核日誌；如有帳號問題，請洽
          <a href="#" class="underline text-slate-500">系統管理員</a>。
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

defineOptions({
  name: 'LoginView'
})

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const error = ref('')
const loading = ref(false)
const showPassword = ref(false)
const usernameFocused = ref(false)
const pwFocused = ref(false)

function handleForgotPassword() {
  window.alert('密碼重設請聯絡系統管理員 / DBA，重設後下次登入會走首次改密碼流程。')
}

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
      error.value = '登入失敗，請稍後再試'
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
@keyframes shake {
  0%, 100% { transform: translateX(0) }
  25% { transform: translateX(-4px) }
  75% { transform: translateX(4px) }
}
.login-shake { animation: shake .35s ease; }
</style>
