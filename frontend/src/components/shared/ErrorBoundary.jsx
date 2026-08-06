import { Component } from 'react'

/**
 * Render sırasında oluşan beklenmedik hataları yakalar.
 * Tüm uygulamanın beyaz ekrana dönmesini önler.
 */
export default class ErrorBoundary extends Component {
  constructor(props) {
    super(props)
    this.state = { hasError: false, message: '' }
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, message: error?.message ?? 'Bilinmeyen hata' }
  }

  componentDidCatch(error, info) {
    console.error('[ErrorBoundary]', error, info)
  }

  render() {
    if (!this.state.hasError) return this.props.children

    return (
      <div style={{
        maxWidth: 480, margin: '4rem auto', padding: '2rem',
        background: 'var(--surface)', border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)', textAlign: 'center',
      }}>
        <div style={{ fontSize: '2.5rem', marginBottom: '1rem' }}>⚠️</div>
        <h2 style={{ color: '#fff', marginBottom: '0.5rem' }}>Bir şeyler ters gitti</h2>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem', fontSize: '0.9rem' }}>
          {this.state.message}
        </p>
        <button
          className="btn btn-ghost"
          onClick={() => this.setState({ hasError: false, message: '' })}
        >
          Tekrar Dene
        </button>
      </div>
    )
  }
}
