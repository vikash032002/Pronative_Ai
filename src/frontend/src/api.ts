export type ConversionRequest = {
  amount: number
  sourceCurrency: string
  targetCurrency: string
}

export type ConversionResult = {
  id: string
  requestedAmount: number
  convertedAmount: number
  exchangeRate: number
  sourceCurrency: string
  targetCurrency: string
  providerDateMarker: string
  providerSequenceMarker?: string | null
  executedAtUtc: string
}

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

function getApiBaseUrlFromMeta(): string {
  const meta = document.querySelector<HTMLMetaElement>('meta[name="vite-api-url"]')
  return meta?.content ?? ''
}

function buildUrl(path: string): string {
  const base = getApiBaseUrlFromMeta()
  return `${base}${path}`
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.ok) return (await res.json()) as T
  const body = (await res.json().catch(() => null)) as ProblemDetails | null
  const title = body?.title ?? `Request failed with status ${res.status}`
  throw new Error(title)
}

export async function createConversion(req: ConversionRequest): Promise<ConversionResult> {
  const res = await fetch(buildUrl('/api/conversions'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req)
  })
  return handleResponse<ConversionResult>(res)
}

export async function getConversionById(id: string): Promise<ConversionResult | null> {
  const res = await fetch(buildUrl(`/api/conversions/${encodeURIComponent(id)}`))
  if (res.status === 404) return null
  return handleResponse<ConversionResult>(res)
}

export async function searchConversions(params: {
  sourceCurrency?: string
  targetCurrency?: string
  startUtc?: string
  endUtc?: string
  limit?: number
}): Promise<ConversionResult[]> {
  const query = new URLSearchParams()
  if (params.sourceCurrency) query.set('sourceCurrency', params.sourceCurrency)
  if (params.targetCurrency) query.set('targetCurrency', params.targetCurrency)
  if (params.startUtc) query.set('startUtc', params.startUtc)
  if (params.endUtc) query.set('endUtc', params.endUtc)
  if (params.limit) query.set('limit', String(params.limit))

  const res = await fetch(buildUrl(`/api/conversions?${query.toString()}`))
  return handleResponse<ConversionResult[]>(res)
}
