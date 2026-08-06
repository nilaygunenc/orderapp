import axios from 'axios'

const client = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,   // 10 saniye — yanıtsız istekte kullanıcı sonsuz spinner görmez
})

// API hata mesajını normalize et — ProblemDetails veya düz string olabilir
export function extractErrorMessage(error) {
  // AbortController tarafından iptal edilen istek — sessizce geç
  if (axios.isCancel(error)) return ''

  const data = error?.response?.data
  if (!data) return 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.'

  // BusinessException → errors dizisi
  if (Array.isArray(data.errors)) return data.errors.join('\n')

  // ProblemDetails → detail > title sırası
  if (data.detail) return data.detail
  if (data.title)  return data.title

  return 'Beklenmeyen bir hata oluştu.'
}

export default client
