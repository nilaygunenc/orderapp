import { useProductSearch } from '../hooks/useProductSearch'
import ProductCard from '../components/products/ProductCard'
import Spinner from '../components/shared/Spinner'
import Alert from '../components/shared/Alert'

export default function ProductsPage() {
  const { products, search, setSearch, loading, error } = useProductSearch()

  return (
    <div className="page">
      <div className="page-header">
        <span style={{ fontSize: '1.5rem' }}>📦</span>
        <h1 style={{ color: '#ffffff' }}>Ürün Kataloğu</h1>
        {!loading && (
          <span className="badge badge-purple" style={{ marginLeft: 'auto' }}>
            {products.length} ürün
          </span>
        )}
      </div>

      <div className="search-wrap">
        <span className="search-icon">🔍</span>
        <input
          className="input search-input"
          type="text"
          placeholder="İsim veya stok koduyla ara…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      <Alert type="error" message={error} />

      {loading ? (
        <Spinner />
      ) : products.length === 0 ? (
        <div className="empty">
          <div className="empty-icon">🔎</div>
          <p>Ürün bulunamadı.</p>
        </div>
      ) : (
        <div className="product-grid">
          {products.map(p => <ProductCard key={p.id} product={p} />)}
        </div>
      )}
    </div>
  )
}
