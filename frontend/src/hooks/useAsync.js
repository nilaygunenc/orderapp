import { useState, useCallback } from 'react'
import { extractErrorMessage } from '../api/client'

/**
 * Tekrar eden loading / error / data state yönetimini soyutlar.
 *
 * Kullanım:
 *   const { data, loading, error, run } = useAsync()
 *   run(() => getOrders())
 */
export function useAsync(initialData = null) {
  const [data, setData]       = useState(initialData)
  const [loading, setLoading] = useState(false)
  const [error, setError]     = useState('')

  const run = useCallback(async (asyncFn) => {
    setLoading(true)
    setError('')
    try {
      const result = await asyncFn()
      setData(result)
      return result
    } catch (err) {
      setError(extractErrorMessage(err))
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, error, run, setData, setError }
}
