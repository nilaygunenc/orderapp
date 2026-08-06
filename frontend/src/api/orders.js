import client from './client'

export const getOrders = () =>
  client.get('/orders').then(r => r.data)

export const getOrderById = (id) =>
  client.get(`/orders/${id}`).then(r => r.data)

export const createOrder = (payload) =>
  client.post('/orders', payload).then(r => r.data)
