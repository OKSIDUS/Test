import { useState, useEffect, useCallback } from 'react'
import './App.css'

interface TopEntry {
  name: string
  totalAmount: number
}

interface DashboardData {
  totalSavings: number
  topBuyers: TopEntry[]
  topSuppliers: TopEntry[]
}

function formatAmount(amount: number): string {
  return new Intl.NumberFormat('uk-UA', {
    style: 'currency',
    currency: 'UAH',
    maximumFractionDigits: 0,
  }).format(amount)
}

function App() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [importing, setImporting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [importMessage, setImportMessage] = useState<string | null>(null)

  const fetchAnalytics = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await fetch('/api/prozorro/analytics')
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setData(await res.json())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { fetchAnalytics() }, [fetchAnalytics])

  const handleImport = async () => {
    setImporting(true)
    setImportMessage(null)
    try {
      const res = await fetch('/api/prozorro/import-tenders', { method: 'POST' })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setImportMessage('Імпорт розпочато. Це може зайняти декілька хвилин')
      await fetchAnalytics()
    } catch (e) {
      setImportMessage(e instanceof Error ? `Помилка імпорту: ${e.message}` : 'Помилка імпорту')
    } finally {
      setImporting(false)
    }
  }

  return (
    <div className="app">
      <header className="header">
        <div className="header-title">
          <span className="header-icon">📊</span>
          <h1>Prozorro Analytics</h1>
        </div>
        <div className="header-actions">
          {importMessage && (
            <span className="import-message">{importMessage}</span>
          )}
          <button
            className="btn-refresh"
            onClick={handleImport}
            disabled={importing || loading}
          >
            {importing ? 'Імпортую...' : '↻ Оновити дані'}
          </button>
        </div>
      </header>

      <main className="main">
        {error && <div className="alert-error">Помилка завантаження: {error}</div>}

        {loading ? (
          <div className="loading">
            <div className="spinner" />
            <span>Завантаження...</span>
          </div>
        ) : data && (
          <>
            <div className="card savings-card">
              <div className="card-label">Загальна економія бюджету</div>
              <div className="savings-amount">{formatAmount(data.totalSavings)}</div>
              <div className="savings-sub">по всіх тендерах CPV 09310000-5</div>
            </div>

            <div className="tables-row">
              <div className="card">
                <h2 className="card-title">Топ-5 замовників</h2>
                <TopTable entries={data.topBuyers} />
              </div>
              <div className="card">
                <h2 className="card-title">Топ-5 постачальників</h2>
                <TopTable entries={data.topSuppliers} />
              </div>
            </div>
          </>
        )}
      </main>
    </div>
  )
}

function TopTable({ entries }: { entries: TopEntry[] }) {
  if (entries.length === 0) return <p className="empty">Немає даних</p>

  const max = entries[0].totalAmount

  return (
    <table className="data-table">
      <thead>
        <tr>
          <th className="col-rank">#</th>
          <th>Назва</th>
          <th className="col-amount">Сума контрактів</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e, i) => (
          <tr key={e.name}>
            <td className="col-rank rank">{i + 1}</td>
            <td>
              <div className="name-cell">
                <span className="name-text">{e.name}</span>
                <div
                  className="bar"
                  style={{ width: `${(e.totalAmount / max) * 100}%` }}
                />
              </div>
            </td>
            <td className="col-amount amount">{formatAmount(e.totalAmount)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default App
