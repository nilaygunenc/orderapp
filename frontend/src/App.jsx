import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Navbar from './components/shared/Navbar'
import ErrorBoundary from './components/shared/ErrorBoundary'
import Spinner from './components/shared/Spinner'

// Lazy load: her sayfa ayrı JS chunk'ına bölünür — ilk yükleme daha hızlı
const ProductsPage = lazy(() => import('./pages/ProductsPage'))
const NewOrderPage = lazy(() => import('./pages/NewOrderPage'))
const OrdersPage   = lazy(() => import('./pages/OrdersPage'))

export default function App() {
  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg)' }}>
      <Navbar />
      <main>
        {/*
          Suspense: lazy component yüklenirken fallback gösterir.
          ErrorBoundary key'i route'a bağlı — sayfa değişince hata state sıfırlanır.
        */}
        <Routes>
          <Route path="/" element={
            <ErrorBoundary key="products">
              <Suspense fallback={<Spinner />}>
                <ProductsPage />
              </Suspense>
            </ErrorBoundary>
          } />
          <Route path="/order/new" element={
            <ErrorBoundary key="new-order">
              <Suspense fallback={<Spinner />}>
                <NewOrderPage />
              </Suspense>
            </ErrorBoundary>
          } />
          <Route path="/orders" element={
            <ErrorBoundary key="orders">
              <Suspense fallback={<Spinner />}>
                <OrdersPage />
              </Suspense>
            </ErrorBoundary>
          } />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  )
}
