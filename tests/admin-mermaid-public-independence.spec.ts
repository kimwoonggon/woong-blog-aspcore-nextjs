import { expect, test, type APIRequestContext } from './helpers/performance-test'

import { expectMermaidRendered } from './helpers/mermaid'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

async function getCsrf(request: APIRequestContext) {
  const csrfResponse = await request.get('/api/auth/csrf')
  expect(csrfResponse.ok()).toBeTruthy()
  return await csrfResponse.json() as { requestToken: string; headerName: string }
}

function mermaidBlockHtml(code: string) {
  return `<mermaid-block data-code="${code.replace(/\n/g, '&#10;')}"></mermaid-block>`
}

async function createBlog(request: APIRequestContext, title: string, html: string, tags: string[]) {
  const csrf = await getCsrf(request)
  const response = await request.post('/api/admin/blogs', {
    headers: {
      'Content-Type': 'application/json',
      [csrf.headerName]: csrf.requestToken,
    },
    data: {
      title,
      excerpt: `excerpt for ${title}`,
      tags,
      published: true,
      contentJson: JSON.stringify({ html }),
    },
  })
  expect(response.ok()).toBeTruthy()
  return await response.json() as { id: string; slug: string }
}

async function createWork(request: APIRequestContext, title: string, html: string, tags: string[]) {
  const csrf = await getCsrf(request)
  const response = await request.post('/api/admin/works', {
    headers: {
      'Content-Type': 'application/json',
      [csrf.headerName]: csrf.requestToken,
    },
    data: {
      title,
      category: 'mermaid-independent',
      period: '2026.04',
      tags,
      published: true,
      contentJson: JSON.stringify({ html }),
      allPropertiesJson: JSON.stringify({}),
      thumbnailAssetId: null,
      iconAssetId: null,
    },
  })
  expect(response.ok()).toBeTruthy()
  return await response.json() as { id: string; slug: string }
}

test('public blog and work pages stay stable when mermaid content exists', async ({ page, request }) => {
  const suffix = Date.now()
  const tag = `mermaid-independence-${suffix}`
  const mermaidCode = `sequenceDiagram
    participant User
    participant Frontend
    User->>Frontend: 로그인 클릭
    Frontend-->>User: 데이터 표시`
  const mermaidBlogTitle = `Mermaid Independent Blog ${suffix}`
  const plainBlogTitle = `Plain Independent Blog ${suffix}`
  const mermaidWorkTitle = `Mermaid Independent Work ${suffix}`
  const plainWorkTitle = `Plain Independent Work ${suffix}`

  const mermaidBlog = await createBlog(
    request,
    mermaidBlogTitle,
    `<p>${tag} Blog before diagram</p>${mermaidBlockHtml(mermaidCode)}<p>Blog after diagram</p>`,
    [tag, 'mermaid'],
  )
  const plainBlog = await createBlog(request, plainBlogTitle, `<p>${tag} Plain blog body</p>`, [tag, 'plain'])
  const mermaidWork = await createWork(
    request,
    mermaidWorkTitle,
    `<p>${tag} Work before diagram</p>${mermaidBlockHtml(mermaidCode)}<p>Work after diagram</p>`,
    [tag, 'mermaid'],
  )
  const plainWork = await createWork(request, plainWorkTitle, `<p>${tag} Plain work body</p>`, [tag, 'plain'])

  await page.goto(`/blog?query=${encodeURIComponent(tag)}&searchMode=content`)
  const blogGrid = page.getByTestId('blog-grid')
  await expect(page.getByTestId('blog-card').filter({ hasText: mermaidBlogTitle }).first()).toBeVisible()
  await expect(page.getByTestId('blog-card').filter({ hasText: plainBlogTitle }).first()).toBeVisible()
  await expect(blogGrid).not.toContainText('sequenceDiagram')
  await expect(blogGrid).not.toContainText('User->>Frontend')
  await expect(blogGrid).not.toContainText('data-code')

  await page.goto(`/works?query=${encodeURIComponent(tag)}&searchMode=content`)
  await expect(page.getByTestId('work-card').filter({ hasText: mermaidWorkTitle }).first()).toBeVisible()
  await expect(page.getByTestId('work-card').filter({ hasText: plainWorkTitle }).first()).toBeVisible()
  await expect(page.locator('main')).not.toContainText('sequenceDiagram')
  await expect(page.locator('main')).not.toContainText('User->>Frontend')
  await expect(page.locator('main')).not.toContainText('data-code')

  await page.goto(`/blog/${mermaidBlog.slug}`)
  await expect(page.locator('main h1', { hasText: mermaidBlogTitle })).toBeVisible()
  await expectMermaidRendered(page)
  const blogBody = page.getByTestId('blog-detail-body')
  await expect(blogBody.getByText(`${tag} Blog before diagram`)).toBeVisible()
  await expect(blogBody.getByText('Blog after diagram')).toBeVisible()
  await page.goto(`/blog/${plainBlog.slug}`)
  await expect(page.locator('main h1', { hasText: plainBlogTitle })).toBeVisible()
  await expect(page.getByText('Plain blog body')).toBeVisible()

  await page.goto(`/works/${mermaidWork.slug}`)
  await expect(page.locator('main h1', { hasText: mermaidWorkTitle })).toBeVisible()
  await expectMermaidRendered(page)
  const workBody = page.getByTestId('work-detail-body')
  await expect(workBody.getByText(`${tag} Work before diagram`)).toBeVisible()
  await expect(workBody.getByText('Work after diagram')).toBeVisible()
  const relatedNext = page.getByRole('button', { name: 'Go to next related page' })
  if (await relatedNext.isEnabled()) {
    await relatedNext.click()
    await expect(page).toHaveURL(/relatedPage=\d+/)
  }
  const relatedCard = page.getByTestId('related-work-card').first()
  if (await relatedCard.count()) {
    await relatedCard.click()
    await expect(page).toHaveURL(/\/works\/.+/)
  }

  await page.goto(`/works/${plainWork.slug}`)
  await expect(page.locator('main h1', { hasText: plainWorkTitle })).toBeVisible()
  await expect(page.getByTestId('work-detail-body').getByText(`${tag} Plain work body`)).toBeVisible()
})
