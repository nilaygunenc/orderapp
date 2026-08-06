import { useState, useEffect, useCallback, useRef } from 'react'
import { getProducts } from '../api/products'
import { extractErrorMessage } from '../api/client'

/**
 * Ürün listesi + debounced arama hook'u.
 * ProductsPage ve NewOrderPage paylaşır — kod tekrarı yok.
 *
 * Optimizasyonlar:
 * - AbortController: eski istek sonucu yeni sonucun üzerine yazamaz
 * - Tek useEffect: ilk yükleme + debounce aynı effect içinde yönetilir,
 *   StrictMode'da çift istek sorunu giderildi
 */
export function useProductSearch(debounceMs = 400) {
  const [products, setProducts] = useState([])
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState('')
  const [refreshVersion, setRefreshVersion] = useState(0)

  // Her istek için controller ref'i — cleanup'ta abort edilir
  const abortRef = useRef(null)

  const fetchProducts = useCallback(async (term, signal) => {
    setLoading(true)
    setError('')
    try {
      const data = await getProducts(term, signal)
      setProducts(data)
    } catch (err) {
      // AbortError beklenen bir durum — kullanıcıya gösterme
      if (err?.code === 'ERR_CANCELED' || err?.name === 'AbortError') return
      setError(extractErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // Önceki isteği iptal et
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller

    // İlk yüklemede debounce yok; sonrasında bekle
    const delay = search === '' ? 0 : debounceMs
    const timer = setTimeout(() => {
      fetchProducts(search, controller.signal)
    }, delay)

    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [search, fetchProducts, debounceMs, refreshVersion])

  const refresh = useCallback(() => {
    setRefreshVersion(version => version + 1)
  }, [])

  return { products, search, setSearch, loading, error, refresh }
}
