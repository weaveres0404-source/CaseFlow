export function useFetch(token) {
  async function api(path, opts = {}) {
    const headers = opts.headers || {}
    if (token) headers['Authorization'] = `Bearer ${token}`
    const res = await fetch(path, { ...opts, headers })
    return res
  }

  return { api }
}
