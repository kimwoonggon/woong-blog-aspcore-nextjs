import { getServerApiBaseUrl, getServerCookieHeader } from '@/lib/api/server'

export interface BlogListItem {
  id: string
  slug: string
  title: string
  excerpt: string
  tags: string[]
  publishedAt?: string | null
  coverUrl?: string
}

export interface PagedBlogsPayload {
  items: BlogListItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface BlogAdminItem extends BlogListItem {
  published: boolean
  createdAt?: string
  updatedAt?: string
}

export interface BlogDetail extends BlogListItem {
  contentJson: string
}

export interface PublicBlogSearchOptions {
  query?: string | null
  searchMode?: 'title' | 'content' | string | null
}

export interface AdminBlogDetail {
  id: string
  title: string
  slug: string
  excerpt: string
  tags: string[]
  published: boolean
  publishedAt?: string | null
  updatedAt?: string
  content: { html: string }
}

async function buildAdminHeaders(): Promise<Record<string, string>> {
  const cookieHeader = await getServerCookieHeader()
  if (!cookieHeader) {
    return {}
  }

  return { cookie: cookieHeader }
}

export async function fetchPublicBlogs(page = 1, pageSize = 10, searchOptions: PublicBlogSearchOptions = {}) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  })
  const query = searchOptions.query?.trim()
  if (query) {
    params.set('query', query)
    params.set('searchMode', searchOptions.searchMode === 'content' ? 'content' : 'title')
  }

  const response = await fetch(`${apiBaseUrl}/public/blogs?${params.toString()}`, { cache: 'no-store' })
  if (!response.ok) {
    return {
      items: [],
      page,
      pageSize,
      totalItems: 0,
      totalPages: 1,
    } satisfies PagedBlogsPayload
  }
  return response.json() as Promise<PagedBlogsPayload>
}

export async function fetchAllPublicBlogs(pageSize = 100) {
  const firstPage = await fetchPublicBlogs(1, pageSize)
  const items = [...firstPage.items]

  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const nextPage = await fetchPublicBlogs(page, pageSize)
    items.push(...nextPage.items)
  }

  return items
}

export async function fetchPublicBlogBySlug(slug: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/blogs/${encodeURIComponent(slug)}`, { cache: 'no-store' })
  if (!response.ok) return null
  return response.json() as Promise<BlogDetail>
}

export async function fetchAdminBlogs() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/admin/blogs`, {
    cache: 'no-store',
    headers: await buildAdminHeaders(),
  })
  if (!response.ok) throw new Error('Failed to load admin blog posts.')
  return response.json() as Promise<BlogAdminItem[]>
}

export async function fetchAdminBlogById(id: string) {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/admin/blogs/${encodeURIComponent(id)}`, {
    cache: 'no-store',
    headers: await buildAdminHeaders(),
  })
  if (response.status === 404) return null
  if (!response.ok) throw new Error('Failed to load the requested blog post.')
  return response.json() as Promise<AdminBlogDetail>
}
