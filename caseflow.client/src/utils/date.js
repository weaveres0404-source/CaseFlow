// 後端回傳的日期時間字串是 UTC 時間，但序列化時未附加時區標記
// （例如 "2026-07-22T16:00:00.000000"，沒有結尾的 'Z' 或 +hh:mm 偏移）。
// 若直接用 `new Date(value)` 解析，瀏覽器會依 ECMAScript 規範將「無時區標記的日期時間字串」
// 視為「本地時間」而非 UTC，導致在 UTC+8 環境下時間被誤判（例如日期少算一天）。
// 這個函式會在字串缺少時區標記時補上 'Z'，確保一律以 UTC 正確解析，
// 之後再交由呼叫端用 toLocaleString(..., { timeZone: 'Asia/Taipei' }) 轉換顯示。
export function parseServerDateTime(value) {
  if (!value) return null
  const hasTimezone = /[zZ]$|[+-]\d{2}:?\d{2}$/.test(value)
  return new Date(hasTimezone ? value : `${value}Z`)
}
