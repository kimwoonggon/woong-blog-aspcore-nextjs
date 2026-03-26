import { getServerApiBaseUrl, getServerCookieHeader } from '@/lib/api/server'

export interface WorkListItem {
  id: string
  slug: string
  title: string
  excerpt: string
  category: string
  period?: string | null
  tags: string[]
  thumbnailUrl?: string
  iconUrl?: string
  publishedAt?: string | null
}

export interface PagedWorksPayload {
  items: WorkListItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface WorkAdminItem extends WorkListItem {
  published: boolean
  createdAt?: string
  updatedAt?: string
}

export interface WorkDetail extends WorkListItem {
  contentJson: string
}

async function buildAdminHeaders(): Promise<Record<string, string>> {
  const cookieHeader = await getServerCookieHeader()
  if (!cookieHeader) {
    return {}
  }

  return { cookie: cookieHeader }
}

export async function fetchPublicWorks(page = 1, pageSize = 6) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/works?page=${page}&pageSize=${pageSize}`, { cache: 'no-store' })
  if (!response.ok) {
    return {
      items: [],
      page,
      pageSize,
      totalItems: 0,
      totalPages: 1,
    } satisfies PagedWorksPayload
  }
  return response.json() as Promise<PagedWorksPayload>
}

export async function fetchAllPublicWorks(pageSize = 100) {
  const firstPage = await fetchPublicWorks(1, pageSize)
  const items = [...firstPage.items]

  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const nextPage = await fetchPublicWorks(page, pageSize)
    items.push(...nextPage.items)
  }

  return items
}

export async function fetchPublicWorkBySlug(slug: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/works/${encodeURIComponent(slug)}`, { cache: 'no-store' })
  if (!response.ok) return null
  return response.json() as Promise<WorkDetail>
}

export async function fetchAdminWorks() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/admin/works`, {
    cache: 'no-store',
    headers: await buildAdminHeaders(),
  })
  if (!response.ok) throw new Error('Failed to load admin works.')
  return response.json() as Promise<WorkAdminItem[]>
}

export async function fetchAdminWorkById(id: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/admin/works/${encodeURIComponent(id)}`, {
    cache: 'no-store',
    headers: await buildAdminHeaders(),
  })
  if (response.status === 404) return null
  if (!response.ok) throw new Error('Failed to load the requested work item.')
  return response.json() as Promise<{
    id: string
    title: string
    slug: string
    excerpt: string
    category: string
    period?: string | null
    tags: string[]
    published: boolean
    publishedAt?: string | null
    updatedAt?: string
    content: { html: string }
    all_properties: Record<string, unknown>
    thumbnail_asset_id?: string | null
    icon_asset_id?: string | null
    thumbnail_url?: string
    icon_url?: string
  }>
}
