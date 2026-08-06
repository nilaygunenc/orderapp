import client from './client'

// signal: AbortController.signal — hook'tan geçirilir, stale response önlenir
export const getProducts = (search = '', signal) =>
  client.get('/products', {
    params: search ? { search } : {},
    signal,
  }).then(r => r.data)

export const getProductById = (id) =>
  client.get(`/products/${id}`).then(r => r.data)
