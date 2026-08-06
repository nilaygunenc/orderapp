import { memo } from 'react'

/**
 * Stok durumunu görsel badge olarak gösterir.
 * ProductCard ve NewOrderPage tablosunda ortak kullanılır.
 */
const StockBadge = memo(function StockBadge({ quantity }) {
  if (quantity === 0)
    return <span className="badge badge-red">Stok Yok</span>
  if (quantity <= 5)
    return <span className="badge badge-yellow">Az: {quantity}</span>
  return <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>{quantity} adet</span>
})

export default StockBadge
