import { memo } from 'react'
import { formatCurrency } from '../../utils/format'

/**
 * memo: product prop değişmediği sürece yeniden render edilmez.
 * Arama sırasında diğer kartlar etkilenmez.
 */
const ProductCard = memo(function ProductCard({ product }) {
  const outOfStock = product.stockQuantity === 0
  const lowStock   = product.stockQuantity > 0 && product.stockQuantity <= 5

  return (
    <div className={`product-card${outOfStock ? ' out-of-stock' : ''}`}>
      <div className="product-sku">{product.sku}</div>
      <div className="product-name">{product.name}</div>

      <div className="product-footer">
        <span className="product-price">
          {formatCurrency(product.unitPrice)}
        </span>

        {outOfStock
          ? <span className="badge badge-red">Stok Yok</span>
          : lowStock
            ? <span className="badge badge-yellow">Az: {product.stockQuantity}</span>
            : <span className="product-stock">{product.stockQuantity} adet</span>
        }
      </div>
    </div>
  )
})

export default ProductCard
