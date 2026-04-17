import { getServerApiBaseUrl, getServerForwardingHeaders } from '@/lib/api/server'
import { throwPublicApiError } from '@/lib/api/public-errors'

export async function fetchPublicPageBySlug(slug: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/pages/${encodeURIComponent(slug)}`, {
    cache: 'no-store',
    headers: await getServerForwardingHeaders(),
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    await throwPublicApiError(response, `Failed to load public page '${slug}'.`)
  }

  return response.json() as Promise<{
    id: string
    slug: string
    title: string
    contentJson: string
  }>
}
