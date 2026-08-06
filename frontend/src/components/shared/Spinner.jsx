export default function Spinner({ text = 'Yükleniyor...' }) {
  return (
    <div className="spinner-wrap">
      <div className="spinner" />
      <span>{text}</span>
    </div>
  )
}
