import { type FormEvent, useState } from 'react'
import { createConversion, searchConversions, type ConversionResult } from './api'
import './index.css'

function toUtcIso(value: string): string | undefined {
  if (!value) return undefined
  const dt = new Date(value)
  if (Number.isNaN(dt.getTime())) return undefined
  return dt.toISOString()
}

export default function App() {
  const [amount, setAmount] = useState<string>('100.00')
  const [sourceCurrency, setSourceCurrency] = useState<string>('USD')
  const [targetCurrency, setTargetCurrency] = useState<string>('EUR')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<ConversionResult | null>(null)

  const [historyLoading, setHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const [history, setHistory] = useState<ConversionResult[]>([])

  const [historyStart, setHistoryStart] = useState('')
  const [historyEnd, setHistoryEnd] = useState('')
  const [historyLimit, setHistoryLimit] = useState('10')

  async function onConvert(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setResult(null)
    setSubmitting(true)

    try {
      const parsed = Number(amount)
      if (!Number.isFinite(parsed) || parsed <= 0) {
        throw new Error('Amount must be a positive number.')
      }
      const res = await createConversion({
        amount: parsed,
        sourceCurrency,
        targetCurrency
      })
      setResult(res)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Request failed')
    } finally {
      setSubmitting(false)
    }
  }

  async function loadHistory() {
    setHistoryError(null)
    setHistoryLoading(true)
    try {
      const items = await searchConversions({
        sourceCurrency: sourceCurrency.trim().toUpperCase() || undefined,
        targetCurrency: targetCurrency.trim().toUpperCase() || undefined,
        startUtc: toUtcIso(historyStart),
        endUtc: toUtcIso(historyEnd),
        limit: Number(historyLimit) || 10
      })
      setHistory(items)
    } catch (err) {
      setHistoryError(err instanceof Error ? err.message : 'History request failed')
    } finally {
      setHistoryLoading(false)
    }
  }

  return (
    <div className="wrap">
      <div className="row" style={{ alignItems: 'flex-start' }}>
        <div className="card" style={{ flex: 1, minWidth: 320 }}>
          <h2 style={{ marginTop: 0 }}>Real-Time Conversion</h2>
          <div className="muted">Submit a live conversion and immediately get an audit trail record.</div>

          <form onSubmit={onConvert} style={{ marginTop: 14 }}>
            <div className="row">
              <div className="field">
                <label>Amount</label>
                <input inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} />
              </div>
              <div className="field">
                <label>From</label>
                <input value={sourceCurrency} onChange={(e) => setSourceCurrency(e.target.value)} />
              </div>
              <div className="field">
                <label>To</label>
                <input value={targetCurrency} onChange={(e) => setTargetCurrency(e.target.value)} />
              </div>
            </div>

            <div style={{ marginTop: 12, display: 'flex', gap: 12, alignItems: 'center' }}>
              <button type="submit" disabled={submitting}>
                {submitting ? 'Converting…' : 'Convert'}
              </button>
              {error ? <div style={{ color: '#fca5a5' }}>{error}</div> : null}
            </div>
          </form>

          {result ? (
            <div style={{ marginTop: 14 }}>
              <div className="muted">Stored audit record:</div>
              <div className="kvs">
                <div className="kv">
                  <div className="k">Conversion Id</div>
                  <div className="v">{result.id}</div>
                </div>
                <div className="kv">
                  <div className="k">Executed At (UTC)</div>
                  <div className="v">{new Date(result.executedAtUtc).toISOString()}</div>
                </div>
                <div className="kv">
                  <div className="k">Requested Amount</div>
                  <div className="v">{result.requestedAmount}</div>
                </div>
                <div className="kv">
                  <div className="k">Converted Amount</div>
                  <div className="v">{result.convertedAmount}</div>
                </div>
                <div className="kv">
                  <div className="k">Exchange Rate</div>
                  <div className="v">{result.exchangeRate}</div>
                </div>
                <div className="kv">
                  <div className="k">Provider Date Marker</div>
                  <div className="v">{result.providerDateMarker}</div>
                </div>
              </div>
              <div className="muted" style={{ marginTop: 10 }}>
                {result.sourceCurrency} → {result.targetCurrency}
              </div>
            </div>
          ) : null}
        </div>

        <div className="card" style={{ flex: 1, minWidth: 320 }}>
          <h2 style={{ marginTop: 0 }}>Audit History</h2>
          <div className="muted">Search stored conversions for the selected currency pair.</div>

          <div className="row" style={{ marginTop: 14 }}>
            <div className="field">
              <label>Start (UTC)</label>
              <input type="datetime-local" value={historyStart} onChange={(e) => setHistoryStart(e.target.value)} />
            </div>
            <div className="field">
              <label>End (UTC)</label>
              <input type="datetime-local" value={historyEnd} onChange={(e) => setHistoryEnd(e.target.value)} />
            </div>
            <div className="field" style={{ minWidth: 120, flex: '0 0 120px' }}>
              <label>Limit</label>
              <input value={historyLimit} onChange={(e) => setHistoryLimit(e.target.value)} inputMode="numeric" />
            </div>
          </div>

          <div style={{ marginTop: 12, display: 'flex', gap: 12, alignItems: 'center' }}>
            <button type="button" onClick={loadHistory} disabled={historyLoading}>
              {historyLoading ? 'Loading…' : 'Load History'}
            </button>
            {historyError ? <div style={{ color: '#fca5a5' }}>{historyError}</div> : null}
          </div>

          <div style={{ marginTop: 14 }}>
            {history.length === 0 ? (
              <div className="muted">No records loaded yet.</div>
            ) : (
              <div style={{ display: 'grid', gap: 10 }}>
                {history.map((h) => (
                  <div key={h.id} className="kv">
                    <div className="k">{h.id}</div>
                    <div className="v" style={{ fontSize: 14, fontWeight: 600 }}>
                      {h.sourceCurrency} → {h.targetCurrency}: {h.convertedAmount} (rate {h.exchangeRate})
                    </div>
                    <div className="muted" style={{ marginTop: 8 }}>
                      ExecutedAtUtc: {h.executedAtUtc} | ProviderDateMarker: {h.providerDateMarker}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
