const icons = { success: '✓', error: '✕', info: 'ℹ' }

export default function Alert({ type = 'info', message }) {
  if (!message) return null
  return (
    <div className={`alert alert-${type}`}>
      <span style={{ fontWeight: 700, flexShrink: 0 }}>{icons[type]}</span>
      <span>{message}</span>
    </div>
  )
}
