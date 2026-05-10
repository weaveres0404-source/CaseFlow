<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 antialiased">
    <header class="h-14 px-6 flex items-center justify-between border-b border-slate-200 bg-white">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-brand-700 text-white grid place-items-center">
          <svg viewBox="0 0 24 24" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 6h16M4 12h10M4 18h7"/><circle cx="18" cy="18" r="3"/>
          </svg>
        </div>
        <div class="text-sm font-semibold text-slate-900">CaseFlow</div>
      </div>
      <div class="flex items-center gap-3 text-xs text-slate-500">
        <span class="num">Setup Token 有效時間：<span class="text-slate-700">{{ countdownText }}</span></span>
        <button type="button" class="text-slate-500 hover:text-slate-800 inline-flex items-center gap-1" @click="goBackToLogin">
          <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
          取消
        </button>
      </div>
    </header>

    <main class="max-w-xl mx-auto px-6 py-12">
      <ol class="flex items-center text-xs mb-8">
        <li class="flex items-center gap-2">
          <span class="w-6 h-6 grid place-items-center rounded-full bg-emerald-500 text-white num text-[11px]">✓</span>
          <span class="text-slate-500">身份驗證</span>
        </li>
        <li class="flex-1 h-px bg-brand-200 mx-2"></li>
        <li class="flex items-center gap-2">
          <span class="w-6 h-6 grid place-items-center rounded-full bg-brand-700 text-white num text-[11px]">2</span>
          <span class="font-medium text-slate-900">設定新密碼</span>
        </li>
        <li class="flex-1 h-px bg-slate-200 mx-2"></li>
        <li class="flex items-center gap-2 text-slate-400">
          <span class="w-6 h-6 grid place-items-center rounded-full bg-slate-200 num text-[11px]">3</span>
          <span>進入系統</span>
        </li>
      </ol>

      <div class="bg-white border border-slate-200 rounded-2xl shadow-sm p-8">
        <div class="flex items-start gap-3 mb-2">
          <div class="w-10 h-10 rounded-xl bg-brand-50 text-brand-700 grid place-items-center shrink-0">
            <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 16v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-2"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          </div>
          <div>
            <h1 class="text-xl font-bold text-slate-900 tracking-tight">首次登入 · 請設定新密碼</h1>
            <p class="text-sm text-slate-500 mt-1">這是您第一次登入 CaseFlow，為了帳號安全，請設定一組僅您知道的新密碼。</p>
          </div>
        </div>

        <div class="mt-6 p-3 rounded-lg bg-slate-50 border border-slate-200 flex items-center gap-3">
          <div class="w-8 h-8 rounded-full bg-brand-100 text-brand-700 grid place-items-center font-semibold text-sm">{{ avatarInitial }}</div>
          <div>
            <div class="text-sm text-slate-900 font-medium">{{ displayName }} <span class="text-xs text-slate-500 font-normal num">{{ displayUserName }}</span></div>
            <div class="text-xs text-slate-500">角色：{{ displayRole }}</div>
          </div>
        </div>

        <form class="mt-6 space-y-5" @submit.prevent="submit">
          <div>
            <label class="block text-xs font-medium text-slate-600 mb-1.5">新密碼</label>
            <div class="input-wrap flex items-center h-11 px-3 rounded-lg border border-slate-300 bg-white transition">
              <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></svg>
              <input id="pw1" v-model="newPassword" :type="showPw1 ? 'text' : 'password'" class="flex-1 outline-none text-sm bg-transparent" placeholder="輸入新密碼" autocomplete="new-password" />
              <button type="button" class="p-1 text-slate-400 hover:text-slate-700" @click="showPw1 = !showPw1">
                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></svg>
              </button>
            </div>

            <div class="mt-3">
              <div class="h-1.5 rounded-full bg-slate-100 overflow-hidden flex gap-1 p-0.5">
                <div v-for="i in 4" :key="i" class="meter-bar h-full flex-1 rounded-full" :class="i <= strengthLevel ? strengthColorClass : 'bg-slate-200'"></div>
              </div>
              <div class="mt-1.5 flex items-center justify-between text-xs">
                <span class="text-slate-500">密碼強度：{{ strengthLabel }}</span>
                <span class="text-[11px] text-slate-400">建議使用 12 碼以上並混用大小寫、數字與符號</span>
              </div>
            </div>

            <ul class="mt-3 grid grid-cols-1 sm:grid-cols-2 gap-y-1.5 gap-x-4 text-xs">
              <li v-for="item in ruleItems" :key="item.key" class="flex items-center gap-1.5" :class="item.ok ? 'check-ok' : 'check-no'">
                <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path v-if="item.ok" d="M20 6 9 17l-5-5"/>
                  <circle v-else cx="12" cy="12" r="9"/>
                </svg>
                {{ item.label }}
              </li>
            </ul>
          </div>

          <div>
            <label class="block text-xs font-medium text-slate-600 mb-1.5">再次輸入新密碼</label>
            <div class="input-wrap flex items-center h-11 px-3 rounded-lg border border-slate-300 bg-white transition">
              <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></svg>
              <input id="pw2" v-model="confirmPassword" :type="showPw2 ? 'text' : 'password'" class="flex-1 outline-none text-sm bg-transparent" placeholder="再次輸入以確認" autocomplete="new-password" />
              <button type="button" class="p-1 text-slate-400 hover:text-slate-700" @click="showPw2 = !showPw2">
                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></svg>
              </button>
            </div>
            <div class="mt-1.5 text-xs" :class="matchHintClass">{{ matchHintText }}</div>
          </div>

          <p v-if="error" class="text-red-600 text-sm bg-red-50 rounded-lg px-3 py-2 border border-red-100">{{ error }}</p>

          <div class="pt-2 flex items-center justify-between gap-3">
            <button type="button" class="text-sm text-slate-500 hover:text-slate-800" @click="goBackToLogin">返回登入</button>
            <button id="saveBtn" type="submit" :disabled="loading || !canSubmit"
              class="h-11 px-5 rounded-lg bg-brand-700 hover:bg-brand-800 disabled:bg-slate-300 disabled:cursor-not-allowed text-white text-sm font-medium">
              <span v-if="loading" class="inline-flex items-center gap-2"><svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9" opacity=".3"/><path d="M21 12a9 9 0 0 1-9 9"/></svg>設定中…</span>
              <span v-else>設定密碼並繼續</span>
            </button>
          </div>
        </form>
      </div>

      <div class="mt-6 flex items-start gap-2 text-xs text-slate-500">
        <svg class="w-4 h-4 shrink-0 mt-0.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/></svg>
        <p>本系統不提供自助修改密碼。日後若需重設密碼，請聯絡系統管理員。</p>
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const newPassword = ref('')
const confirmPassword = ref('')
const error = ref('')
const loading = ref(false)
const showPw1 = ref(false)
const showPw2 = ref(false)
const secondsLeft = ref(14 * 60 + 58)
let countdownTimer = null

const displayName = computed(() => auth.user?.full_name || '使用者')
const displayUserName = computed(() => auth.user?.username || '')
const displayRole = computed(() => auth.user?.role || '未指定')
const avatarInitial = computed(() => (displayName.value || 'U').slice(0, 1))

const rules = computed(() => {
  const p = newPassword.value || ''
  const hintSource = `${displayUserName.value}${displayName.value}`.toLowerCase()
  return {
    r1: p.length >= 8,
    r2: /[A-Z]/.test(p),
    r3: /[a-z]/.test(p),
    r4: /[0-9]/.test(p),
    r5: /[^A-Za-z0-9]/.test(p),
    r6: p.length > 0 && !(hintSource && p.toLowerCase().includes(hintSource))
  }
})

const ruleItems = computed(() => [
  { key: 'r1', label: '至少 8 個字元', ok: rules.value.r1 },
  { key: 'r2', label: '包含英文大寫字母', ok: rules.value.r2 },
  { key: 'r3', label: '包含英文小寫字母', ok: rules.value.r3 },
  { key: 'r4', label: '包含數字', ok: rules.value.r4 },
  { key: 'r5', label: '包含特殊符號（!@#$%…）', ok: rules.value.r5 },
  { key: 'r6', label: '不包含您的帳號 / 姓名', ok: rules.value.r6 }
])

const passedRulesCount = computed(() => Object.values(rules.value).filter(Boolean).length)
const strengthLevel = computed(() => Math.min(4, Math.max(0, Math.ceil((passedRulesCount.value * 4) / 6))))
const strengthLabel = computed(() => ['—', '弱', '普通', '良好', '非常強'][strengthLevel.value])
const strengthColorClass = computed(() => ['bg-slate-200', 'bg-rose-400', 'bg-amber-400', 'bg-lime-500', 'bg-emerald-500'][strengthLevel.value])

const passwordMatch = computed(() => newPassword.value.length > 0 && newPassword.value === confirmPassword.value)
const canSubmit = computed(() => passedRulesCount.value === 6 && passwordMatch.value)

const matchHintText = computed(() => {
  if (!confirmPassword.value) return '兩次輸入需一致'
  return passwordMatch.value ? '✓ 密碼一致' : '兩次密碼不一致'
})

const matchHintClass = computed(() => {
  if (!confirmPassword.value) return 'text-slate-400'
  return passwordMatch.value ? 'text-emerald-600' : 'text-rose-600'
})

const countdownText = computed(() => {
  const m = String(Math.floor(secondsLeft.value / 60)).padStart(2, '0')
  const s = String(secondsLeft.value % 60).padStart(2, '0')
  return `${m}:${s}`
})

function goBackToLogin() {
  sessionStorage.removeItem('setup_token')
  router.replace('/login')
}

onMounted(() => {
  const token = sessionStorage.getItem('setup_token')
  if (!token) {
    router.replace('/login')
    return
  }

  countdownTimer = window.setInterval(() => {
    if (secondsLeft.value > 0) secondsLeft.value -= 1
  }, 1000)
})

onUnmounted(() => {
  if (countdownTimer) window.clearInterval(countdownTimer)
})

async function submit() {
  error.value = ''
  if (!canSubmit.value) {
    error.value = '請先完成所有密碼規則，並確認兩次密碼一致'
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

<style scoped>
.num {
  font-feature-settings: 'tnum';
}

.input-wrap:focus-within {
  border-color: #4f46e5;
  box-shadow: 0 0 0 4px rgba(79, 70, 229, 0.12);
}

.check-ok {
  color: #059669;
}

.check-no {
  color: #94a3b8;
}

.meter-bar {
  transition: background-color .3s ease;
}
</style>
