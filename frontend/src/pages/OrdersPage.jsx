import { useState, useEffect, useCallback } from 'react'
import { getOrders, getOrderById } from '../api/orders'
import { extractErrorMessage } from '../api/client'
import { formatCurrency, formatDateTime } from '../utils/format'
import Spinner from '../components/shared/Spinner'
import Alert from '../components/shared/Alert'

export default function OrdersPage() {
  const [orders, setOrders]   = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError]     = useState('')
  const [expandedId, setExpandedId]     = useState(null)
  const [detailMap, setDetailMap]       = useState({})
  const [detailLoading, setDetailLoading] = useState(false)

  useEffect(() => {
    setLoading(true)
    getOrders()
      .then(setOrders)
      .catch(err => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false))
  }, [])

  // useCallback — her render'da yeni fonksiyon oluşturma
  const handleToggle = useCallback(async (id) => {
    if (expandedId === id) { setExpandedId(null); return }
    setExpandedId(id)
    if (detailMap[id]) return   // zaten yüklü — tekrar istek yapma
    setDetailLoading(true)
    try {
      const detail = await getOrderById(id)
      setDetailMap(prev => ({ ...prev, [id]: detail }))
    } catch (err) {
      setError(extractErrorMessage(err))
    } finally {
      setDetailLoading(false)
    }
  }, [expandedId, detailMap])

  return (
    <div className="page">
      <div className="page-header">
        <span style={{ fontSize: '1.5rem' }}>📋</span>
        <h1 style={{ color: '#ffffff' }}>Siparişler</h1>
        {!loading && orders.length > 0 && (
          <span className="badge badge-purple" style={{ marginLeft: 'auto' }}>
            {orders.length} sipariş
          </span>
        )}
      </div>

      <Alert type="error" message={error} />

      {loading ? <Spinner /> : orders.length === 0 ? (
        <div className="empty">
          <div className="empty-icon">📭</div>
          <p>Henüz sipariş bulunmuyor.</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.65rem' }}>
          {orders.map(order => (
            <div
              key={order.id}
              className={`order-row${expandedId === order.id ? ' expanded' : ''}`}
            >
              {/* Başlık satırı */}
              <div className="order-row-header" onClick={() => handleToggle(order.id)}>
                <span className="order-id">#{order.id}</span>
                <span className="order-customer">{order.customerName}</span>
                <span className="order-date">
                  {formatDateTime(order.orderDate)}
                </span>
                <span className="order-total">
                  {formatCurrency(order.totalAmount)}
                </span>
                <span className={`order-chevron${expandedId === order.id ? ' open' : ''}`}>▼</span>
              </div>

              {/* Detay paneli */}
              {expandedId === order.id && (
                <div className="order-detail-panel">
                  {detailLoading && !detailMap[order.id]
                    ? <Spinner text="Detay yükleniyor…" />
                    : <OrderDetail order={detailMap[order.id]} />
                  }
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function OrderDetail({ order }) {
  if (!order) return null
  return (
    <>
      <table>
        <thead>
          <tr>
            <th>SKU</th>
            <th>Ürün</th>
            <th style={{ textAlign: 'right' }}>Birim Fiyat *</th>
            <th style={{ textAlign: 'right' }}>Adet</th>
            <th style={{ textAlign: 'right' }}>Tutar</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((item, i) => (
            <tr key={i}>
              <td style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', fontWeight: 600 }}>
                {item.productSku}
              </td>
              <td style={{ fontWeight: 500 }}>{item.productName}</td>
              <td style={{ textAlign: 'right' }}>
                {formatCurrency(item.unitPrice)}
              </td>
              <td style={{ textAlign: 'right', color: 'var(--text-secondary)' }}>{item.quantity}</td>
              <td style={{ textAlign: 'right', fontWeight: 700, color: 'var(--primary)' }}>
                {formatCurrency(item.lineTotal)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={4} style={{ textAlign: 'right', fontWeight: 700, color: 'var(--text-secondary)', borderTop: '1px solid var(--border)', paddingTop: '0.75rem' }}>
              Toplam
            </td>
            <td style={{ textAlign: 'right', fontWeight: 700, color: 'var(--primary)', borderTop: '1px solid var(--border)', paddingTop: '0.75rem' }}>
              {formatCurrency(order.totalAmount)}
            </td>
          </tr>
        </tfoot>
      </table>
      <p className="detail-note">* Sipariş anındaki fiyatlardır. Güncel ürün fiyatından farklı olabilir.</p>
    </>
  )
}
