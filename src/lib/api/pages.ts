import { getServerApiBaseUrl } from '@/lib/api/server'

export async function fetchPublicPageBySlug(slug: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/pages/${encodeURIComponent(slug)}`, {
    cache: 'no-store',
  })

  if (!response.ok) {
    return null
  }

  return response.json() as Promise<{
    id: string
    slug: string
    title: string
    contentJson: string
  }>
}
