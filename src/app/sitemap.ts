import type { MetadataRoute } from 'next'
import { fetchAllPublicBlogs } from '@/lib/api/blogs'
import { fetchAllPublicWorks } from '@/lib/api/works'
import { getMetadataBaseUrl } from '@/lib/seo'

const staticPublicRoutes = [
  '/',
  '/blog',
  '/works',
  '/contact',
  '/introduction',
  '/resume',
]

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const baseUrl = getMetadataBaseUrl()
  const [blogs, works] = await Promise.all([
    fetchAllPublicBlogs().catch(() => []),
    fetchAllPublicWorks().catch(() => []),
  ])

  const now = new Date()

  return [
    ...staticPublicRoutes.map((path) => ({
      url: `${baseUrl}${path}`,
      lastModified: now,
      changeFrequency: path === '/' ? 'weekly' as const : 'monthly' as const,
      priority: path === '/' ? 1 : 0.7,
    })),
    ...blogs.map((blog) => ({
      url: `${baseUrl}/blog/${encodeURIComponent(blog.slug)}`,
      lastModified: blog.publishedAt ? new Date(blog.publishedAt) : now,
      changeFrequency: 'monthly' as const,
      priority: 0.6,
    })),
    ...works.map((work) => ({
      url: `${baseUrl}/works/${encodeURIComponent(work.slug)}`,
      lastModified: work.publishedAt ? new Date(work.publishedAt) : now,
      changeFrequency: 'monthly' as const,
      priority: 0.6,
    })),
  ]
}
