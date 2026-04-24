import { expect, type APIRequestContext, type TestInfo } from '@playwright/test'

interface BlogFixtureOptions {
  titlePrefix?: string
  html?: string
  excerpt?: string
  tags?: string[]
  published?: boolean
}

interface WorkFixtureOptions {
  titlePrefix?: string
  html?: string
  excerpt?: string
  category?: string
  tags?: string[]
  published?: boolean
  allPropertiesJson?: string
}

export interface BlogFixture {
  id: string
  slug: string
  title: string
  tag: string
}

export interface WorkFixture {
  id: string
  slug: string
  title: string
  tag: string
}

async function getCsrf(request: APIRequestContext) {
  const csrfResponse = await request.get('/api/auth/csrf')
  expect(csrfResponse.ok()).toBeTruthy()
  return await csrfResponse.json() as { requestToken: string; headerName: string }
}

function buildFixtureTag(testInfo: TestInfo) {
  const random = Math.random().toString(36).slice(2, 8)
  return `pw-w${testInfo.parallelIndex}-r${testInfo.retry}-${Date.now().toString(36)}-${random}`
}

export async function createBlogFixture(
  request: APIRequestContext,
  testInfo: TestInfo,
  options: BlogFixtureOptions = {},
): Promise<BlogFixture> {
  const tag = buildFixtureTag(testInfo)
  const title = `${options.titlePrefix ?? 'Playwright Blog Fixture'} ${tag}`
  const html = options.html ?? `<p>${title} body</p>`
  const csrf = await getCsrf(request)
  const response = await request.post('/api/admin/blogs', {
    headers: {
      'Content-Type': 'application/json',
      [csrf.headerName]: csrf.requestToken,
    },
    data: {
      title,
      excerpt: options.excerpt ?? `Excerpt for ${title}`,
      tags: options.tags ?? ['playwright-fixture', tag],
      published: options.published ?? true,
      contentJson: JSON.stringify({ html }),
    },
  })

  if (!response.ok()) {
    throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`)
  }

  const payload = await response.json() as { id: string; slug: string }
  return { ...payload, title, tag }
}

export async function createWorkFixture(
  request: APIRequestContext,
  testInfo: TestInfo,
  options: WorkFixtureOptions = {},
): Promise<WorkFixture> {
  const tag = buildFixtureTag(testInfo)
  const title = `${options.titlePrefix ?? 'Playwright Work Fixture'} ${tag}`
  const html = options.html ?? `<p>${title} body</p>`
  const excerpt = options.excerpt ?? `Excerpt for ${title}`
  const csrf = await getCsrf(request)
  const response = await request.post('/api/admin/works', {
    headers: {
      'Content-Type': 'application/json',
      [csrf.headerName]: csrf.requestToken,
    },
    data: {
      title,
      category: options.category ?? 'playwright-fixture',
      period: '',
      tags: options.tags ?? ['playwright-fixture', tag],
      published: options.published ?? true,
      contentJson: JSON.stringify({ html }),
      allPropertiesJson: options.allPropertiesJson ?? '{}',
      thumbnailAssetId: null,
      iconAssetId: null,
      excerpt,
    },
  })

  if (!response.ok()) {
    throw new Error(`Failed to create work fixture: ${response.status()} ${await response.text()}`)
  }

  const payload = await response.json() as { id: string; slug: string }
  return { ...payload, title, tag }
}
