import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

export default {
  name: 'Login',
  setup() {
    const router = useRouter()
    const email = ref('')
    const password = ref('')
    const error = ref('')
    const loading = ref(false)

    async function submit(e) {
      e && e.preventDefault()
      error.value = ''
      loading.value = true
      try {
        const res = await axios.post('/api/v1/auth/login', {
          email: email.value,
          password: password.value,
        })

        // Response may follow different envelopes. Try common shapes.
        const payload = res?.data
        let token = null
        if (payload) {
          if (payload.access_token) token = payload.access_token
          else if (payload.data?.access_token) token = payload.data.access_token
          else if (payload.data?.accessToken) token = payload.data.accessToken
        }

        if (!token) {
          error.value = '登入失敗：伺服器回傳格式錯誤'
          loading.value = false
          return
        }

        localStorage.setItem('access_token', token)
        router.push('/dashboard')
      } catch (err) {
        if (err.response && err.response.data) {
          const d = err.response.data
          if (d.error?.message) error.value = d.error.message
          else if (d.message) error.value = d.message
          else error.value = '登入失敗'
        } else {
          error.value = '無法連線至伺服器'
        }
      } finally {
        loading.value = false
      }
    }

    return () => (
      <div class="min-h-screen flex items-center justify-center bg-gray-50">
        <div class="max-w-md w-full bg-white shadow-lg rounded-lg p-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-6">登入 CaseFlow</h2>

          <form onSubmit={submit}>
            <label class="block text-sm font-medium text-gray-700">Email</label>
            <input
              type="email"
              value={email.value}
              onInput={e => (email.value = e.target.value)}
              class="mt-1 mb-4 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="user@example.com"
              required
            />

            <label class="block text-sm font-medium text-gray-700">Password</label>
            <input
              type="password"
              value={password.value}
              onInput={e => (password.value = e.target.value)}
              class="mt-1 mb-4 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Your password"
              required
            />

            {error.value ? (
              <div class="text-red-600 text-sm mb-4">{error.value}</div>
            ) : null}

            <div class="flex items-center justify-between">
              <button
                type="submit"
                class="inline-flex items-center justify-center px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none disabled:opacity-50"
                disabled={loading.value}
              >
                {loading.value ? '登入中...' : '登入'}
              </button>

              <a href="#" class="text-sm text-indigo-600 hover:underline">忘記密碼？</a>
            </div>
          </form>

          <div class="mt-6 text-sm text-gray-500">
            使用測試帳號請聯絡系統管理員。
          </div>
        </div>
      </div>
    )
  }
}
