/**
 * Para birimi formatlama yardımcısı.
 * Uygulama genelinde tek bir locale/currency tanımı — değiştirilmesi gerekirse tek yerden.
 */
export function formatCurrency(amount) {
  return amount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
}

/**
 * Tarih/saat formatlama yardımcısı.
 */
export function formatDateTime(isoString) {
  return new Date(isoString).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}
