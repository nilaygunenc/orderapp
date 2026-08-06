import { NavLink } from 'react-router-dom'

export default function Navbar() {
  return (
    <nav style={{
      background: 'var(--surface)',
      borderBottom: '1px solid var(--border)',
      padding: '0 1.25rem',
      display: 'flex',
      alignItems: 'center',
      height: '56px',
      position: 'sticky',
      top: 0,
      zIndex: 100,
      backdropFilter: 'blur(12px)',
    }}>
      {/* Logo */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginRight: 'auto' }}>
        <span style={{ fontSize: '1.3rem' }}>🛒</span>
        <span style={{ fontWeight: '700', fontSize: '1rem', color: '#ffffff', letterSpacing: '-0.01em' }}>
          OrderApp
        </span>
      </div>

      {/* Links */}
      <div style={{ display: 'flex', gap: '0.25rem' }}>
        <NavItem to="/" label="Ürünler" icon="📦" end />
        <NavItem to="/order/new" label="Sipariş Oluştur" icon="➕" />
        <NavItem to="/orders" label="Siparişlerim" icon="📋" />
      </div>
    </nav>
  )
}

function NavItem({ to, label, icon, end }) {
  return (
    <NavLink
      to={to}
      end={end}
      style={({ isActive }) => ({
        display: 'flex',
        alignItems: 'center',
        gap: '0.4rem',
        padding: '0.4rem 0.85rem',
        borderRadius: 'var(--radius-md)',
        textDecoration: 'none',
        fontSize: '0.88rem',
        fontWeight: isActive ? '600' : '500',
        color: isActive ? 'var(--text-primary)' : '#b0b4cc',  /* eskiden text-secondary, daha okunaklı */
        background: isActive ? 'var(--surface-2)' : 'transparent',
        border: isActive ? '1px solid var(--border)' : '1px solid transparent',
        transition: 'all 0.15s',
      })}
    >
      <span style={{ fontSize: '0.9rem' }}>{icon}</span>
      <span>{label}</span>
    </NavLink>
  )
}
