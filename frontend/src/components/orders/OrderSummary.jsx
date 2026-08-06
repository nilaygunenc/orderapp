import { useMemo } from 'react'
import { formatCurrency } from '../../utils/format'

/**
 * Seçili ürünlerin özet tablosunu gösterir.
 * NewOrderPage'den ayrı dosyaya çıkarıldı — tek sorumluluk ilkesi.
 *
 * @param {Array}  products      - Tüm ürün listesi
 * @param {Object} selectedItems - { [productId]: quantity }
 */
export default function OrderSummary({ products, selectedItems }) {
  // Her özet satırında Array.find çalıştırmak yerine ürünleri bir kez indeksle.
  const productMap = useMemo(
    () => new Map(products.map(product => [product.id, product])),
    [products],
  )

  // products veya selectedItems değişmediği sürece yeniden hesaplama yapma
  const { lines, grandTotal } = useMemo(() => {
    const lines = Object.entries(selectedItems)
      .map(([id, qty]) => {
        const p = productMap.get(Number(id))
        if (!p) return null
        return { name: p.name, qty, total: p.unitPrice * qty }
      })
      .filter(Boolean)

    return { lines, grandTotal: lines.reduce((sum, l) => sum + l.total, 0) }
  }, [productMap, selectedItems])

  if (lines.length === 0) return null

  return (
    <div className="order-summary">
      <div className="order-summary-title">Sipariş Özeti</div>
      {lines.map((l, i) => (
        <div key={i} className="summary-line">
          <span>
            {l.name}{' '}
            <span style={{ color: 'var(--text-secondary)' }}>× {l.qty}</span>
          </span>
          <span>{formatCurrency(l.total)}</span>
        </div>
      ))}
      <div className="summary-total">
        <span>Toplam</span>
        <span>{formatCurrency(grandTotal)}</span>
      </div>
    </div>
  )
}
