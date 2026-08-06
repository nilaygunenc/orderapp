import { memo, useState, useCallback } from 'react'
import { createOrder } from '../api/orders'
import { extractErrorMessage } from '../api/client'
import { useProductSearch } from '../hooks/useProductSearch'
import { formatCurrency } from '../utils/format'
import OrderSummary from '../components/orders/OrderSummary'
import Spinner from '../components/shared/Spinner'
import Alert from '../components/shared/Alert'

export default function NewOrderPage() {
  const {
    products,
    search,
    setSearch,
    loading: loadingProducts,
    error: productsError,
    refresh: refreshProducts,
  } = useProductSearch()

  const [selectedItems, setSelectedItems] = useState({})  // { [productId]: quantity }
  const [customerName, setCustomerName]   = useState('')
  const [submitting, setSubmitting]       = useState(false)
  const [successMsg, setSuccessMsg]       = useState('')
  const [errorMsg, setErrorMsg]           = useState('')
  const [validationMsg, setValidationMsg] = useState('')

  // useCallback — her render'da yeni fonksiyon oluşturma
  const toggleProduct = useCallback((id) => {
    setSelectedItems(prev => {
      const next = { ...prev }
      if (next[id] !== undefined) delete next[id]
      else next[id] = 1
      return next
    })
  }, [])

  const updateQuantity = useCallback((id, value) => {
    const qty = Math.max(1, parseInt(value) || 1)
    setSelectedItems(prev => ({ ...prev, [id]: qty }))
  }, [])

  function validate() {
    if (!customerName.trim()) {
      setValidationMsg('Müşteri adı zorunludur.')
      return false
    }
    if (Object.keys(selectedItems).length === 0) {
      setValidationMsg('En az bir ürün seçmelisiniz.')
      return false
    }
    setValidationMsg('')
    return true
  }

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validate()) return

    setSubmitting(true)
    setSuccessMsg('')
    setErrorMsg('')

    try {
      const order = await createOrder({
        customerName: customerName.trim(),
        items: Object.entries(selectedItems).map(([pid, qty]) => ({
          productId: parseInt(pid),
          quantity: qty,
        })),
      })
      setSuccessMsg(`Sipariş #${order.id} oluşturuldu! Toplam: ${formatCurrency(order.totalAmount)}`)
      setSelectedItems({})
      setCustomerName('')
      refreshProducts()
    } catch (err) {
      setErrorMsg(extractErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  const selectedCount = Object.keys(selectedItems).length

  return (
    <div className="page">
      <div className="page-header">
        <span style={{ fontSize: '1.5rem' }}>➕</span>
        <h1 style={{ color: '#ffffff' }}>Sipariş Oluştur</h1>
      </div>

      <Alert type="success" message={successMsg} />
      <Alert type="error"   message={errorMsg} />
      <Alert type="error"   message={validationMsg} />
      <Alert type="error"   message={productsError} />

      {/* Müşteri adı */}
      <div style={{ marginBottom: '1.5rem' }}>
        <label className="form-label">Müşteri Adı</label>
        <input
          className="input"
          type="text"
          placeholder="Adınızı girin"
          value={customerName}
          onChange={e => setCustomerName(e.target.value)}
        />
      </div>

      <hr className="divider" />

      {/* Ürün seçimi başlığı + arama */}
      <div style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
          <label className="form-label" style={{ margin: 0 }}>Ürün Seç</label>
          {selectedCount > 0 && (
            <span className="badge badge-purple">{selectedCount} ürün seçildi</span>
          )}
        </div>
        <div className="search-wrap" style={{ marginBottom: 0 }}>
          <span className="search-icon">🔍</span>
          <input
            className="input search-input"
            type="text"
            placeholder="Ürün ara…"
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Ürün tablosu */}
      {loadingProducts
        ? <Spinner text="Ürünler yükleniyor…" />
        : (
          <div className="table-wrap" style={{ marginBottom: '1.5rem' }}>
            <table>
              <thead>
                <tr>
                  <th style={{ width: 40 }} />
                  <th>SKU</th>
                  <th>Ürün Adı</th>
                  <th style={{ textAlign: 'right' }}>Fiyat</th>
                  <th style={{ textAlign: 'right' }}>Stok</th>
                  <th style={{ textAlign: 'center' }}>Miktar</th>
                </tr>
              </thead>
              <tbody>
                {products.map(p => (
                  <ProductRow
                    key={p.id}
                    product={p}
                    quantity={selectedItems[p.id]}
                    onToggle={toggleProduct}
                    onQuantityChange={updateQuantity}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )
      }

      {/* Sipariş özeti */}
      <OrderSummary products={products} selectedItems={selectedItems} />

      <button
        className="btn btn-primary btn-block"
        onClick={handleSubmit}
        disabled={submitting}
      >
        {submitting ? '⏳ Oluşturuluyor…' : '🛒 Sipariş Oluştur'}
      </button>
    </div>
  )
}

/**
 * Tablo satırı ayrı component — her satır bağımsız render edilir,
 * diğer satırlar etkilenmez (React reconciliation optimizasyonu).
 */
const ProductRow = memo(function ProductRow({ product: p, quantity, onToggle, onQuantityChange }) {
  const isSelected = quantity !== undefined
  const outOfStock = p.stockQuantity === 0

  return (
    <tr
      className={`${isSelected ? 'row-selected' : ''} ${outOfStock ? 'row-disabled' : ''}`}
      onClick={() => !outOfStock && onToggle(p.id)}
      style={{ cursor: outOfStock ? 'not-allowed' : 'pointer' }}
    >
      <td onClick={e => e.stopPropagation()} style={{ textAlign: 'center' }}>
        <input
          type="checkbox"
          checked={isSelected}
          disabled={outOfStock}
          onChange={() => onToggle(p.id)}
        />
      </td>
      <td style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', fontWeight: 600 }}>
        {p.sku}
      </td>
      <td style={{ fontWeight: 500 }}>{p.name}</td>
      <td style={{ textAlign: 'right', fontWeight: 700, color: 'var(--primary)' }}>
        {formatCurrency(p.unitPrice)}
      </td>
      <td style={{ textAlign: 'right' }}>
        <StockBadge quantity={p.stockQuantity} />
      </td>
      <td style={{ textAlign: 'center' }} onClick={e => e.stopPropagation()}>
        {isSelected && (
          <input
            type="number"
            className="qty-input"
            min={1}
            max={p.stockQuantity}
            value={quantity}
            onChange={e => onQuantityChange(p.id, e.target.value)}
          />
        )}
      </td>
    </tr>
  )
})

/** Stok durumuna göre doğru badge'i render eder */
function StockBadge({ quantity }) {
  if (quantity === 0)  return <span className="badge badge-red">Yok</span>
  if (quantity <= 5)   return <span className="badge badge-yellow">{quantity}</span>
  return <span style={{ color: 'var(--text-secondary)' }}>{quantity}</span>
}
