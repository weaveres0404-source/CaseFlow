import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../utils/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('access_token') || '')
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

  const isLoggedIn = computed(() => !!token.value)
  const role = computed(() => user.value?.role || '')

  async function login(username, password) {
    const { data: res } = await api.post('/auth/login', { username, password })
    if (res.success) {
      if (res.data.must_change_password) {
        return { mustChangePassword: true, setupToken: res.data.setup_token }
      }
      token.value = res.data.access_token
      user.value = res.data.user
      localStorage.setItem('access_token', res.data.access_token)
      localStorage.setItem('user', JSON.stringify(res.data.user))
      return res.data
    }
    throw new Error(res.error?.message || '登入失敗')
  }

  async function setupPassword(setupToken, newPassword) {
    const { data: res } = await api.post('/auth/setup-password', {
      setup_token: setupToken,
      new_password: newPassword
    })
    if (res.success && res.data.access_token) {
      token.value = res.data.access_token
      user.value = res.data.user
      localStorage.setItem('access_token', res.data.access_token)
      localStorage.setItem('user', JSON.stringify(res.data.user))
      return res.data
    }
    throw new Error(res.error?.message || '設定密碼失敗')
  }

  async function fetchMe() {
    const { data: res } = await api.get('/auth/me')
    if (res.success) {
      user.value = res.data
      localStorage.setItem('user', JSON.stringify(res.data))
    }
    return res.data
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('user')
  }

  return { token, user, isLoggedIn, role, login, setupPassword, fetchMe, logout }
})
