<template>
  <div class="login-shell">
    <section class="brand-panel" aria-label="Case Flow">
      <div class="brand-grid" aria-hidden="true"></div>
      <div class="brand-mark" aria-hidden="true">SL</div>
      <div class="dragon-mark" aria-hidden="true"></div>

      <div class="brand-content">
        <h1>Case Flow</h1>
        <div class="title-rule" aria-hidden="true"></div>
        <p class="system-name">任務追蹤系統</p>

        <div class="brand-divider" aria-hidden="true"></div>

        <div class="brand-logo">SQUARE LIGHT</div>
        <p class="brand-note">Built for internal operations</p>
      </div>
    </section>

    <main class="login-panel">
      <form class="login-form" @submit.prevent="submit">
        <div class="login-company-logo" aria-label="矩明數位有限公司">
          <span class="login-logo-zh">矩明數位有限公司</span>
        </div>

        <label class="field-group">
          <span class="field-label">帳號</span>
          <span class="input-wrap">
            <svg class="field-icon" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            <input
              v-model="username"
              type="text"
              name="account"
              placeholder="請輸入帳號"
              autocomplete="username"
              required />
          </span>
        </label>

        <label class="field-group">
          <span class="field-label">密碼</span>
          <span class="input-wrap">
            <svg class="field-icon" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true">
              <rect x="4" y="11" width="16" height="10" rx="2" />
              <path d="M8 11V7a4 4 0 0 1 8 0v4" />
              <path d="M12 15v2" />
            </svg>
            <input
              id="password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              name="password"
              placeholder="請輸入密碼"
              autocomplete="current-password"
              required />
            <button
              class="password-toggle"
              type="button"
              :aria-label="showPassword ? '隱藏密碼' : '顯示密碼'"
              aria-controls="password"
              @click="showPassword = !showPassword">
              <svg class="eye-icon" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
                <circle cx="12" cy="12" r="3" />
              </svg>
            </button>
          </span>
        </label>

        <div class="form-row">
          <label class="remember">
            <input v-model="rememberMe" type="checkbox" name="remember" />
            <span>記住我</span>
          </label>
          <a href="#" @click.prevent="showForgotHint">忘記密碼？</a>
        </div>

        <p v-if="error" class="error-message">
          {{ error }}
        </p>

        <button class="login-button" type="submit" :disabled="loading">
          <span v-if="loading">登入中...</span>
          <span v-else>登入</span>
        </button>
      </form>
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

defineOptions({ name: 'LoginView' })

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const showPassword = ref(false)
const error = ref('')
const loading = ref(false)

function showForgotHint() {
  window.alert('請聯絡系統管理員重設密碼')
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
      error.value = `登入失敗，請稍後再試`
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+TC:wght@300;400;500;600&display=swap');

:global(body) {
  font-family: 'Noto Sans TC', 'Microsoft JhengHei', 'PingFang TC', 'Segoe UI', Arial, sans-serif;
}

.login-shell {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(600px, 54fr) minmax(660px, 46fr);
  overflow: hidden;
  background: radial-gradient(circle at 34% 37%, rgba(49, 115, 122, 0.18), transparent 34%), linear-gradient(135deg, #071a1f 0%, #082227 52%, #04181d 100%);
}

.brand-panel {
  position: relative;
  min-height: 100vh;
  padding: clamp(42px, 7vw, 104px);
  overflow: hidden;
  color: #f4f7f7;
  isolation: isolate;
}

.brand-panel::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: -3;
  background: radial-gradient(circle at 24% 42%, rgba(22, 184, 174, 0.12), transparent 27%), radial-gradient(circle at 73% 74%, rgba(42, 91, 96, 0.36), transparent 27%), linear-gradient(90deg, rgba(7, 28, 33, 0.95), rgba(6, 28, 33, 0.86));
}

.brand-panel::after {
  content: '';
  position: absolute;
  inset: 0;
  z-index: -2;
  background-image: radial-gradient(rgba(30, 194, 188, 0.22) 1px, transparent 1px), linear-gradient(105deg, transparent 54%, rgba(54, 120, 126, 0.11) 55%, transparent 82%);
  background-size: 14px 14px, 100% 100%;
  opacity: 0.36;
}

.brand-grid {
  position: absolute;
  right: -66px;
  bottom: 318px;
  width: 468px;
  height: 226px;
  z-index: -1;
  opacity: 0.28;
  transform: perspective(620px) rotateX(62deg) rotateZ(-3deg);
  transform-origin: right bottom;
  background: repeating-linear-gradient(90deg, rgba(45, 142, 149, 0.35) 0 1px, transparent 1px 13px), repeating-linear-gradient(0deg, rgba(45, 142, 149, 0.3) 0 1px, transparent 1px 13px);
}

.brand-mark {
  position: absolute;
  top: 18px;
  left: 38px;
  font-size: min(14vw, 190px);
  font-weight: 600;
  letter-spacing: 0.08em;
  opacity: 0.06;
}

.dragon-mark {
  position: absolute;
  right: -60px;
  bottom: 70px;
  width: min(38vw, 323px);
  height: min(38vw, 323px);
  opacity: 0.68;
  z-index: -1;
  border-radius: 50%;
  background: radial-gradient(circle at 35% 35%, rgba(98, 196, 190, 0.6), rgba(5, 37, 44, 0.08) 62%, transparent 75%);
}

.brand-content {
  position: relative;
  top: 56%;
  transform: translateY(-4%);
  max-width: 470px;
}

.brand-content h1 {
  margin: 0 0 18px;
  color: #ffffff;
  font-size: clamp(40px, 3.5vw, 56px);
  font-weight: 300;
  line-height: 1.08;
  text-shadow: 0 6px 16px rgba(0, 0, 0, 0.38);
}

.title-rule {
  width: 42px;
  height: 4px;
  margin: 0 0 22px 4px;
  background: #19b8ae;
  box-shadow: 0 0 18px rgba(25, 184, 174, 0.68);
}

.system-name {
  margin: 0;
  color: #19b8ae;
  font-size: clamp(18px, 1.35vw, 26px);
  font-weight: 300;
}

.brand-divider {
  width: 266px;
  height: 1px;
  margin: 36px 0 34px;
  background: rgba(219, 230, 231, 0.58);
}

.brand-logo {
  color: #ffffff;
  letter-spacing: 0.14em;
  font-weight: 500;
}

.brand-note {
  margin: 22px 0 0;
  color: rgba(229, 236, 236, 0.58);
  font-size: 16px;
  font-weight: 300;
}

.login-panel {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 56px clamp(44px, 5.4vw, 96px);
  border-radius: 20px;
  background: radial-gradient(circle at 68% 22%, rgba(225, 226, 226, 0.28), transparent 34%), linear-gradient(145deg, #ffffff 0%, #fbfbfa 100%);
  box-shadow: -28px 0 70px rgba(0, 0, 0, 0.14);
}

.login-form {
  width: min(100%, 392px);
  transform: translateY(2px);
}

.login-company-logo {
  display: flex;
  justify-content: center;
  margin: 0 auto 40px;
}

.login-logo-zh {
  position: static;
  color: #020506;
  font-size: 30px;
  font-weight: 600;
  line-height: 1.15;
  letter-spacing: 0.02em;
}

.field-group {
  display: block;
  margin: 0 0 26px;
}

.field-label {
  display: block;
  margin: 0 0 10px;
  color: #0b1217;
  font-size: 18px;
  font-weight: 600;
  line-height: 1.1;
}

.input-wrap {
  position: relative;
  display: block;
}

.field-icon,
.eye-icon {
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.field-icon {
  position: absolute;
  left: 16px;
  top: 50%;
  color: #6e7780;
  transform: translateY(-50%);
  pointer-events: none;
}

.input-wrap input {
  width: 100%;
  height: 50px;
  padding: 0 46px;
  color: #182129;
  background: rgba(255, 255, 255, 0.68);
  border: 1px solid #d3d9de;
  border-radius: 8px;
  font-size: 16px;
  outline: none;
  transition: border-color 160ms ease, box-shadow 160ms ease, background 160ms ease;
}

.input-wrap input::placeholder {
  color: #9aa2a9;
}

.input-wrap input:focus {
  background: #ffffff;
  border-color: rgba(0, 103, 108, 0.66);
  box-shadow: 0 0 0 4px rgba(0, 103, 108, 0.08);
}

.password-toggle {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  width: 34px;
  height: 34px;
  display: grid;
  place-items: center;
  padding: 0;
  color: #68737c;
  background: transparent;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
}

.password-toggle:hover,
.password-toggle:focus-visible {
  color: #00676c;
  background: rgba(0, 103, 108, 0.06);
  outline: none;
}

.form-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  margin: 0 0 20px;
  font-size: 16px;
}

.remember {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: #101820;
  cursor: pointer;
}

.remember input {
  appearance: none;
  width: 20px;
  height: 20px;
  flex: 0 0 20px;
  margin: 0;
  border: 1.5px solid #6d7780;
  border-radius: 5px;
  background: #ffffff;
  cursor: pointer;
  display: grid;
  place-items: center;
}

.remember input::after {
  content: '';
  width: 9px;
  height: 5px;
  border-left: 2px solid #ffffff;
  border-bottom: 2px solid #ffffff;
  transform: rotate(-45deg) scale(0);
  transition: transform 120ms ease;
}

.remember input:checked {
  border-color: #00676c;
  background: #00676c;
}

.remember input:checked::after {
  transform: rotate(-45deg) scale(1);
}

.form-row a {
  color: #00676c;
  text-decoration: none;
  font-weight: 500;
}

.form-row a:hover,
.form-row a:focus-visible {
  text-decoration: underline;
  outline: none;
}

.error-message {
  margin: 0 0 16px;
  color: #b91c1c;
  font-size: 14px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
  padding: 12px 14px;
}

.login-button {
  width: 100%;
  height: 58px;
  color: #ffffff;
  background: linear-gradient(90deg, rgba(0, 86, 90, 0.22), transparent 42%), linear-gradient(180deg, #062c32 0%, #041d22 100%);
  border: 0;
  border-radius: 8px;
  box-shadow: 0 5px 12px rgba(0, 38, 43, 0.14);
  font-size: 24px;
  font-weight: 500;
  cursor: pointer;
  transition: transform 160ms ease, box-shadow 160ms ease, filter 160ms ease;
}

.login-button:hover,
.login-button:focus-visible {
  filter: brightness(1.08);
  box-shadow: 0 8px 18px rgba(0, 38, 43, 0.18);
  outline: none;
}

.login-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 900px) {
  .login-shell {
    display: block;
    min-height: 100vh;
    background: #fbfbfa;
  }

  .brand-panel {
    min-height: 360px;
    padding: 48px 28px;
  }

  .brand-content {
    top: auto;
    transform: none;
    max-width: 420px;
  }

  .brand-content h1 {
    font-size: 40px;
  }

  .login-panel {
    min-height: auto;
    display: block;
    padding: 52px 28px 64px;
    border-radius: 24px 24px 0 0;
    box-shadow: 0 -18px 44px rgba(0, 0, 0, 0.13);
  }

  .login-form {
    margin: 0 auto;
    transform: none;
  }

  .login-logo-zh {
    font-size: 26px;
  }

  .input-wrap input {
    height: 52px;
    padding-inline: 46px;
    font-size: 16px;
  }

  .login-button {
    height: 54px;
    font-size: 24px;
  }

  .field-label {
    font-size: 18px;
  }

  .form-row {
    font-size: 16px;
  }

  .error-message {
    font-size: 14px;
  }
}

@media (min-width: 1200px) and (max-width: 1680px) {
  .login-shell {
    grid-template-columns: minmax(520px, 54fr) minmax(520px, 46fr);
  }

  .brand-content {
    top: 54%;
  }

  .brand-content h1 {
    font-size: clamp(44px, 3.8vw, 52px);
  }

  .login-panel {
    padding: 44px clamp(34px, 4.8vw, 74px);
  }

  .login-form {
    width: min(100%, 430px);
  }

  .login-logo-zh {
    font-size: 26px;
  }

  .field-label {
    font-size: 20px;
  }

  .input-wrap input {
    height: 54px;
    font-size: 17px;
  }

  .form-row {
    font-size: 17px;
  }

  .login-button {
    height: 56px;
    font-size: 22px;
  }
}
</style>
